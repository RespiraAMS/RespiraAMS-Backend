using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Commands.SoftDelete;

public sealed class SoftDeleteDoctorCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly Mock<IMessageBus> _bus = new();
    private readonly SoftDeleteDoctorCommandHandler _handler;

    public SoftDeleteDoctorCommandHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new DoctorDbContext(_options);
        var logger = Mock.Of<ILogger<SoftDeleteDoctorCommandHandler>>();

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
    public async Task HandleAsync_ByAdmin_SoftDeletesDoctorProfile()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .Doctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_ByAdmin_SoftDeletesHeadDepartmentDoctorProfile()
    {
        var seeded = await SeedHeadAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .HeadDepartmentDoctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_PublishesSoftDeleteDoctorCompleted()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var completed = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorCompleted>()
        );
        Assert.Equal(seeded.Id, completed.DoctorId);
        Assert.Equal(command.SagaId, completed.SagaId);
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
        Assert.Equal("Doctor.ProfileSoftDeleted", auditEvent.Action);
        Assert.Equal("Doctor", auditEvent.EntityType);
        Assert.Equal(seeded.Id, auditEvent.EntityId);
        Assert.Equal(sagaId, auditEvent.CorrelationId);
        Assert.Equal(actorAccountId, auditEvent.ActorAccountId);
        Assert.Equal("Admin", auditEvent.ActorRole);
    }

    [Fact]
    public async Task HandleAsync_ForHeadProfile_PublishesAuditEventWithHeadEntityType()
    {
        var seeded = await SeedHeadAsync();

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("Doctor.ProfileSoftDeleted", auditEvent.Action);
        Assert.Equal("HeadDepartmentDoctor", auditEvent.EntityType);
        Assert.Equal(seeded.Id, auditEvent.EntityId);
    }

    [Fact]
    public async Task HandleAsync_ByAdmin_AllowsActorAccountDifferentFromProfileAccount()
    {
        // The admin actor does not own the profile: the role grants the permission.
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin");
        command.ActorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .Doctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_ByOwnerDoctor_SoftDeletesOwnProfile()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = seeded.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .Doctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_ByOwnerHeadDepartmentDoctor_SoftDeletesOwnHeadProfile()
    {
        var seeded = await SeedHeadAsync();
        var command = BuildCommand(seeded, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = seeded.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .HeadDepartmentDoctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_ByOwningHeadDepartmentDoctor_SoftDeletesSubordinateDoctor()
    {
        var head = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: head.Id);
        var command = BuildCommand(doctor, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = head.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .Doctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == doctor.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_WithBlankActorRole_SkipsAuthorizationCheck()
    {
        // A blank role means "system": the authorization check is skipped entirely,
        // so even a mismatching target account does not block the soft delete.
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "   ");
        command.TargetAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .Doctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsDeleted);
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
    public async Task HandleAsync_WithBlankActorRole_PublishesAuditEventWithSystemRole()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "  ");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("System", auditEvent.ActorRole);
    }

    #endregion

    #region Fail path

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_PublishesCompleted()
    {
        // The compensation is idempotent: an already deleted profile counts as success.
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var completed = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorCompleted>()
        );
        Assert.Equal(command.DoctorId, completed.DoctorId);
        Assert.Equal(command.SagaId, completed.SagaId);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_DoesNotPublishFailure()
    {
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorFailure>()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_DoesNotPublishAuditEvent()
    {
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(_bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>());
    }

    [Fact]
    public async Task HandleAsync_WhenActorCannotManageProfile_PublishesFailure()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorFailure>()
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
        Assert.Equal("Authorization.ProfileDeleteDenied", auditEvent.Action);
        Assert.Equal("Doctor", auditEvent.EntityType);
        Assert.Equal(seeded.Id, auditEvent.EntityId);
    }

    [Fact]
    public async Task HandleAsync_WhenActorCannotManageProfile_DoesNotSoftDeleteProfile()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Doctor");
        command.ActorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification
            .Doctors.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == seeded.Id, TestContext.Current.CancellationToken);

        Assert.False(saved.IsDeleted);
        Assert.Null(saved.DeletedAt);
    }

    [Fact]
    public async Task HandleAsync_WhenHeadActorTargetsUnmanagedDoctor_PublishesFailure()
    {
        var actorHead = await SeedHeadAsync();
        var managingHead = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: managingHead.Id);
        var command = BuildCommand(doctor, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = actorHead.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorFailure>()
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
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenHeadActorTargetsAnotherHeadProfile_PublishesFailure()
    {
        // A head of department doctor manages doctors, not other head profiles.
        var actorHead = await SeedHeadAsync();
        var otherHead = await SeedHeadAsync();
        var command = BuildCommand(otherHead, actorRole: "HeadDepartmentDoctor");
        command.ActorAccountId = actorHead.AccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorFailure>()
        );
        Assert.Equal("The actor cannot manage this profile", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_PublishesFailureWithExceptionMessage()
    {
        var seeded = await SeedDoctorAsync();
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new SoftDeleteDoctorCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<SoftDeleteDoctorCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorFailure>()
        );
        Assert.Equal("save failed", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_DoesNotPublishCompleted()
    {
        var seeded = await SeedDoctorAsync();
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new SoftDeleteDoctorCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<SoftDeleteDoctorCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<SoftDeleteDoctorCompleted>()
        );
    }

    #endregion

    private async Task<DoctorEntity> SeedDoctorAsync(Guid? headDepartmentDoctorId = null)
    {
        await using var context = new DoctorDbContext(_options);
        var doctor = TestData.DoctorFactory.CreateDoctor(
            headDepartmentDoctorId: headDepartmentDoctorId,
            citizenIdentificationNumber: NextCitizenIdentificationNumber()
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<HeadDepartmentDoctorEntity> SeedHeadAsync()
    {
        await using var context = new DoctorDbContext(_options);
        var head = TestData.DoctorFactory.CreateHeadDepartmentDoctor(
            citizenIdentificationNumber: NextCitizenIdentificationNumber()
        );
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return head;
    }

    private static string NextCitizenIdentificationNumber() =>
        Guid.CreateVersion7().ToString("N")[..12];

    private static SoftDeleteDoctorCommand BuildCommand(BaseDoctor seeded, string actorRole) =>
        new()
        {
            SagaId = Guid.CreateVersion7(),
            DoctorId = seeded.Id,
            TargetAccountId = seeded.AccountId,
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = actorRole,
        };

    /// <summary>
    /// A command that points at a profile id that does not exist in the database.
    /// </summary>
    private static SoftDeleteDoctorCommand BuildCommandForMissingProfile(string actorRole) =>
        new()
        {
            SagaId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            TargetAccountId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = actorRole,
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
