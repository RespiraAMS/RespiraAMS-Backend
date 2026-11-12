using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Commands.Update;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Commands.Update;

public sealed class UpdateDoctorProfileCommandHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly Mock<IMessageBus> _bus = new();
    private readonly UpdateDoctorProfileCommandHandler _handler;

    public UpdateDoctorProfileCommandHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new DoctorDbContext(_options);
        var logger = Mock.Of<ILogger<UpdateDoctorProfileCommandHandler>>();

        _handler = new(context, _bus.Object, logger);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var context = new DoctorDbContext(_options);
        await context
            .Doctors.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context
            .HeadDepartmentDoctors.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ByAdmin_UpdatesEveryProfileField()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Le", saved.FirstName);
        Assert.Equal("Van B", saved.LastName);
        Assert.Equal("002099099888", saved.CitizenIdentificationNumber);
        Assert.Equal("99 Nguyen Trai", saved.Address);
        Assert.Equal(GenderType.Female, saved.Gender);
        Assert.Equal(PositionType.SeniorDoctor, saved.Position);
        Assert.Equal(AcademicTitle.AssociateProfessor, saved.AcademicTitle);
        Assert.Equal([DegreeType.Doctor, DegreeType.PhD], saved.Degrees);
        Assert.Equal(
            new DateTimeOffset(1985, 3, 15, 0, 0, 0, TimeSpan.Zero),
            saved.DateOfBirth
        );
    }

    [Fact]
    public async Task HandleAsync_ByAdmin_PublishesDoctorProfileUpdated()
    {
        var seeded = await SeedDoctorAsync();

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        var updated = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdated>()
        );
        Assert.Equal(seeded.Id, updated.DoctorId);
        Assert.Equal(Guid.Empty, updated.OldMediaId);
        Assert.Equal(Guid.Empty, updated.MediaId);
    }

    [Fact]
    public async Task HandleAsync_ByAdmin_UpdatesTimestamp()
    {
        var seeded = await SeedDoctorAsync();
        var before = DateTimeOffset.UtcNow.AddSeconds(-5);

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.InRange(
            saved.UpdatedAt.ToUnixTimeMilliseconds(),
            before.ToUnixTimeMilliseconds(),
            DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeMilliseconds()
        );
    }

    [Fact]
    public async Task HandleAsync_ByOwnerDoctor_UpdatesProfile()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = seeded.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Le", saved.FirstName);
    }

    [Fact]
    public async Task HandleAsync_ByOwnerHeadDepartmentDoctor_UpdatesProfile()
    {
        var seeded = await SeedHeadAsync();
        var command = BuildCommand(seeded, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = seeded.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.HeadDepartmentDoctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Le", saved.FirstName);
    }

    [Fact]
    public async Task HandleAsync_ByOwningHeadDepartmentDoctor_UpdatesSubordinateDoctor()
    {
        var head = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: head.Id);
        var command = BuildCommand(doctor, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = head.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == doctor.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Le", saved.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WithNewMediaId_LinksNewMediaAndReportsPreviousOne()
    {
        var oldMediaId = Guid.CreateVersion7();
        var newMediaId = Guid.CreateVersion7();
        var seeded = await SeedDoctorAsync(mediaId: oldMediaId);
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.NewMediaId = newMediaId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(newMediaId, saved.MediaId);

        var updated = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdated>()
        );
        Assert.Equal(newMediaId, updated.MediaId);
        Assert.Equal(oldMediaId, updated.OldMediaId);
    }

    [Fact]
    public async Task HandleAsync_WithoutNewMediaId_KeepsExistingMedia()
    {
        var oldMediaId = Guid.CreateVersion7();
        var seeded = await SeedDoctorAsync(mediaId: oldMediaId);
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.NewMediaId = Guid.Empty;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(oldMediaId, saved.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithoutNewMediaId_ReportsEmptyOldMediaId()
    {
        var seeded = await SeedDoctorAsync();

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        var updated = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdated>()
        );
        Assert.Equal(Guid.Empty, updated.OldMediaId);
    }

    [Fact]
    public async Task HandleAsync_PublishesAuditEvent()
    {
        var seeded = await SeedDoctorAsync();
        var sagaId = Guid.CreateVersion7();
        var actorAccountId = Guid.CreateVersion7();
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.SagaId = sagaId;
        command.ActorAccountId = actorAccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("Doctor.ProfileUpdated", auditEvent.Action);
        Assert.Equal("Doctor", auditEvent.EntityType);
        Assert.Equal(seeded.Id, auditEvent.EntityId);
        Assert.Equal(sagaId, auditEvent.CorrelationId);
        Assert.Equal(actorAccountId, auditEvent.ActorAccountId);
        Assert.Equal("Admin", auditEvent.ActorRole);
    }

    [Fact]
    public async Task HandleAsync_WithBlankActorRole_IsDenied()
    {
        // Unlike the soft delete flow, a blank role is not treated as "system" here:
        // the actor cannot manage anything, so the update is rejected.
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.ActorRole = "  ";

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Nguyen", saved.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyActorAccount_PublishesAuditEventWithoutActor()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.ActorAccountId = Guid.Empty;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Null(auditEvent.ActorAccountId);
    }

    [Fact]
    public async Task HandleAsync_UpdatesHeadDepartmentDoctorProfile()
    {
        var seeded = await SeedHeadAsync();

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.HeadDepartmentDoctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Le", saved.FirstName);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("HeadDepartmentDoctor", auditEvent.EntityType);
    }

    #endregion

    #region Fail path

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_PublishesFailure()
    {
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("Profile not found", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_DoesNotPublishUpdated()
    {
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdated>()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenProfileBelongsToAnotherAccount_PublishesFailure()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.AccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("Profile does not belong to the account", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileBelongsToAnotherAccount_DoesNotModifyProfile()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.AccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Nguyen", saved.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WhenActorCannotManageProfile_PublishesFailure()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenActorCannotManageProfile_PublishesAuthorizationDeniedAuditEvent()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("Authorization.ProfileUpdateDenied", auditEvent.Action);
        Assert.Equal("Doctor", auditEvent.EntityType);
        Assert.Equal(seeded.Id, auditEvent.EntityId);
    }

    [Fact]
    public async Task HandleAsync_WhenActorCannotManageProfile_DoesNotModifyProfile()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Nguyen", saved.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WhenActorIsAnotherHeadDepartmentDoctor_PublishesFailure()
    {
        var head = await SeedHeadAsync();
        var otherHead = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: head.Id);
        var command = BuildCommand(doctor, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = otherHead.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenActorRoleIsUnknown_PublishesFailure()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Guest");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenActorRoleIsEmpty_PublishesFailure()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: string.Empty);

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenHeadActorTargetsHeadProfile_PublishesFailure()
    {
        // A head of department doctor manages doctors, not other head profiles.
        var head = await SeedHeadAsync();
        var otherHead = await SeedHeadAsync();
        var command = BuildCommand(otherHead, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = head.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_PublishesFailureWithExceptionMessage()
    {
        var seeded = await SeedDoctorAsync();
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new UpdateDoctorProfileCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<UpdateDoctorProfileCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdateFailure>()
        );
        Assert.Equal("save failed", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_DoesNotPublishUpdated()
    {
        var seeded = await SeedDoctorAsync();
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new UpdateDoctorProfileCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<UpdateDoctorProfileCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileUpdated>()
        );
    }

    #endregion

    private async Task<DoctorEntity> SeedDoctorAsync(Guid? headDepartmentDoctorId = null, Guid? mediaId = null)
    {
        await using var context = new DoctorDbContext(_options);
        var doctor = TestData.DoctorFactory.CreateDoctor(
            headDepartmentDoctorId: headDepartmentDoctorId,
            mediaId: mediaId
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<HeadDepartmentDoctorEntity> SeedHeadAsync()
    {
        await using var context = new DoctorDbContext(_options);
        var head = TestData.DoctorFactory.CreateHeadDepartmentDoctor();
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return head;
    }

    private static UpdateDoctorProfileCommand BuildCommand(
        BaseDoctor seeded,
        string actorRole
    ) =>
        new()
        {
            SagaId = Guid.CreateVersion7(),
            AccountId = seeded.AccountId,
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = actorRole,
            DoctorId = seeded.Id,
            FirstName = "Le",
            LastName = "Van B",
            Degrees = [DegreeType.Doctor, DegreeType.PhD],
            AcademicTitle = AcademicTitle.AssociateProfessor,
            Position = PositionType.SeniorDoctor,
            Gender = GenderType.Female,
            CitizenIdentificationNumber = "002099099888",
            DateOfBirth = new DateTimeOffset(1985, 3, 15, 0, 0, 0, TimeSpan.Zero),
            Address = "99 Nguyen Trai",
        };

    /// <summary>
    /// A command that points at a profile id that does not exist in the database.
    /// </summary>
    private static UpdateDoctorProfileCommand BuildCommandForMissingProfile(string actorRole) =>
        new()
        {
            SagaId = Guid.CreateVersion7(),
            AccountId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = actorRole,
            DoctorId = Guid.CreateVersion7(),
            FirstName = "Le",
            LastName = "Van B",
            Degrees = [DegreeType.Doctor],
            AcademicTitle = AcademicTitle.None,
            Position = PositionType.StaffDoctor,
            Gender = GenderType.Male,
            CitizenIdentificationNumber = "002099099888",
            Address = "99 Nguyen Trai",
        };

    private sealed class FailingSaveDoctorDbContext(DoctorDbContext inner)
        : Respira.Doctor.Application.Contracts.Data.IDoctorDbContext
    {
        public DbSet<DoctorEntity> Doctors => inner.Doctors;

        public DbSet<HeadDepartmentDoctorEntity> HeadDepartmentDoctors =>
            inner.HeadDepartmentDoctors;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("save failed");
    }
}
