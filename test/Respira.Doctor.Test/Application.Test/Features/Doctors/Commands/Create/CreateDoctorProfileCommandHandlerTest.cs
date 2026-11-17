using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Commands.Create;
using Respira.Doctor.Application.Features.Doctors.Commands.Create.Events;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;

namespace Respira.Doctor.Application.Test.Features.Doctors.Commands.Create;

public sealed class CreateDoctorProfileCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly Mock<IMessageBus> _bus = new();
    private readonly CreateDoctorProfileCommandHandler _handler;

    public CreateDoctorProfileCommandHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new DoctorDbContext(_options);
        var logger = Mock.Of<ILogger<CreateDoctorProfileCommandHandler>>();

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
    public async Task HandleAsync_DoctorRole_PersistsDoctorProfile()
    {
        var accountId = Guid.CreateVersion7();
        var command = BuildCommand(accountId: accountId);

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.AccountId == accountId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(command.FirstName, saved.FirstName);
        Assert.Equal(command.LastName, saved.LastName);
        Assert.Equal(command.CitizenIdentificationNumber, saved.CitizenIdentificationNumber);
        Assert.Equal(command.Address, saved.Address);
        Assert.Equal(command.Gender, saved.Gender);
        Assert.Equal(command.Position, saved.Position);
        Assert.Equal(command.AcademicTitle, saved.AcademicTitle);
        Assert.Equal(command.Degrees, saved.Degrees);
        Assert.Null(saved.HeadDepartmentDoctorId);
    }

    [Fact]
    public async Task HandleAsync_DoctorRole_PublishesDoctorProfileCreated()
    {
        var sagaId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();

        await _handler.HandleAsync(
            BuildCommand(sagaId: sagaId, accountId: accountId),
            TestContext.Current.CancellationToken
        );

        var created = Assert.Single(
            _bus.Invocations
                .Select(x => x.Arguments[0])
                .OfType<DoctorProfileCreated>()
        );
        Assert.Equal(sagaId, created.SagaId);
        Assert.Equal(accountId, created.AccountId);
        Assert.Equal(ProfileRole.Doctor, created.Role);
        Assert.NotEqual(Guid.Empty, created.DoctorId);
    }

    [Fact]
    public async Task HandleAsync_HeadDepartmentDoctorRole_PersistsHeadProfile()
    {
        var accountId = Guid.CreateVersion7();

        await _handler.HandleAsync(
            BuildCommand(accountId: accountId, role: ProfileRole.HeadDepartmentDoctor),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.HeadDepartmentDoctors.SingleAsync(
            x => x.AccountId == accountId,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Nguyen", saved.FirstName);

        Assert.Empty(
            await verification.Doctors.Where(x => x.AccountId == accountId).ToListAsync(
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task HandleAsync_HeadDepartmentDoctorRole_PublishesCreatedWithHeadRole()
    {
        var sagaId = Guid.CreateVersion7();

        await _handler.HandleAsync(
            BuildCommand(sagaId: sagaId, role: ProfileRole.HeadDepartmentDoctor),
            TestContext.Current.CancellationToken
        );

        var created = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileCreated>()
        );
        Assert.Equal(ProfileRole.HeadDepartmentDoctor, created.Role);
    }

    [Fact]
    public async Task HandleAsync_WithHeadDepartmentDoctorAccount_LinksHeadProfile()
    {
        var headAccountId = Guid.CreateVersion7();
        await using (var seed = new DoctorDbContext(_options))
        {
            seed.HeadDepartmentDoctors.Add(
                TestData.DoctorFactory.CreateHeadDepartmentDoctor(accountId: headAccountId)
            );
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var verification = new DoctorDbContext(_options))
        {
            var head = await verification.HeadDepartmentDoctors.SingleAsync(
                x => x.AccountId == headAccountId,
                TestContext.Current.CancellationToken
            );

            await _handler.HandleAsync(
                BuildCommand(headDepartmentDoctorAccountId: headAccountId),
                TestContext.Current.CancellationToken
            );

            var saved = await verification.Doctors.SingleAsync(
                x => x.AccountId != headAccountId,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(head.Id, saved.HeadDepartmentDoctorId);
        }
    }

    [Fact]
    public async Task HandleAsync_WithUnknownHeadDepartmentDoctorAccount_PublishesFailure()
    {
        var sagaId = Guid.CreateVersion7();
        var unknownHeadAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(
            BuildCommand(
                sagaId: sagaId,
                headDepartmentDoctorAccountId: unknownHeadAccountId
            ),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileFailure>()
        );
        Assert.Equal(sagaId, failure.SagaId);
        Assert.Equal(
            "The creating head of department doctor profile was not found",
            failure.Reason
        );
    }

    [Fact]
    public async Task HandleAsync_WithUnknownHeadDepartmentDoctorAccount_DoesNotPersistDoctor()
    {
        await _handler.HandleAsync(
            BuildCommand(headDepartmentDoctorAccountId: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        Assert.Empty(await verification.Doctors.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHeadDepartmentDoctorAccount_SkipsHeadLookup()
    {
        // Guid.Empty is the "no head" marker, so no head profile is required.
        await _handler.HandleAsync(
            BuildCommand(headDepartmentDoctorAccountId: Guid.Empty),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Null(saved.HeadDepartmentDoctorId);
    }

    [Fact]
    public async Task HandleAsync_PublishesAuditEvent()
    {
        var sagaId = Guid.CreateVersion7();
        var creatorAccountId = Guid.CreateVersion7();

        await _handler.HandleAsync(
            BuildCommand(sagaId: sagaId, creatorAccountId: creatorAccountId, creatorRole: "Admin"),
            TestContext.Current.CancellationToken
        );

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("Doctor.ProfileCreated", auditEvent.Action);
        Assert.Equal("Doctor", auditEvent.EntityType);
        Assert.Equal(creatorAccountId, auditEvent.ActorAccountId);
        Assert.Equal("Admin", auditEvent.ActorRole);
        Assert.Equal(sagaId, auditEvent.CorrelationId);
        Assert.NotNull(auditEvent.EntityId);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyCreatorAccount_PublishesAuditEventWithoutActor()
    {
        await _handler.HandleAsync(
            BuildCommand(creatorAccountId: Guid.Empty, creatorRole: "Admin"),
            TestContext.Current.CancellationToken
        );
        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Null(auditEvent.ActorAccountId);
    }

    [Fact]
    public async Task HandleAsync_WithBlankCreatorRole_PublishesAuditEventAsSystem()
    {
        await _handler.HandleAsync(
            BuildCommand(creatorRole: "   "),
            TestContext.Current.CancellationToken
        );

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("System", auditEvent.ActorRole);
    }

    #endregion

    #region Fail path

    [Fact]
    public async Task HandleAsync_WhenDoctorProfileAlreadyExists_PublishesFailure()
    {
        var accountId = Guid.CreateVersion7();
        await using (var seed = new DoctorDbContext(_options))
        {
            seed.Doctors.Add(TestData.DoctorFactory.CreateDoctor(accountId: accountId));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var sagaId = Guid.CreateVersion7();
        await _handler.HandleAsync(
            BuildCommand(sagaId: sagaId, accountId: accountId),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileFailure>()
        );
        Assert.Equal(sagaId, failure.SagaId);
        Assert.Equal("Profile already exists", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenDoctorProfileAlreadyExists_DoesNotPublishCreated()
    {
        var accountId = Guid.CreateVersion7();
        await using (var seed = new DoctorDbContext(_options))
        {
            seed.Doctors.Add(TestData.DoctorFactory.CreateDoctor(accountId: accountId));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _handler.HandleAsync(
            BuildCommand(accountId: accountId),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileCreated>()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenHeadProfileAlreadyExists_PublishesFailure()
    {
        var accountId = Guid.CreateVersion7();
        await using (var seed = new DoctorDbContext(_options))
        {
            seed.HeadDepartmentDoctors.Add(
                TestData.DoctorFactory.CreateHeadDepartmentDoctor(accountId: accountId)
            );
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _handler.HandleAsync(
            BuildCommand(accountId: accountId),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileFailure>()
        );
        Assert.Equal("Profile already exists", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenHeadProfileAlreadyExists_DoesNotPersistSecondProfile()
    {
        var accountId = Guid.CreateVersion7();
        await using (var seed = new DoctorDbContext(_options))
        {
            seed.HeadDepartmentDoctors.Add(
                TestData.DoctorFactory.CreateHeadDepartmentDoctor(accountId: accountId)
            );
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _handler.HandleAsync(
            BuildCommand(accountId: accountId),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        Assert.Equal(
            1,
            await verification.HeadDepartmentDoctors.CountAsync(TestContext.Current.CancellationToken)
        );
        Assert.Empty(await verification.Doctors.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_PublishesFailureWithExceptionMessage()
    {
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new CreateDoctorProfileCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<CreateDoctorProfileCommandHandler>>()
        );
        var sagaId = Guid.CreateVersion7();

        await handler.HandleAsync(
            BuildCommand(sagaId: sagaId),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileFailure>()
        );
        Assert.Equal(sagaId, failure.SagaId);
        Assert.Equal("save failed", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_DoesNotPublishCreated()
    {
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new CreateDoctorProfileCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<CreateDoctorProfileCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorProfileCreated>()
        );
    }

    #endregion

    private static CreateDoctorProfileCommand BuildCommand(
        Guid? sagaId = null,
        Guid? accountId = null,
        Guid? creatorAccountId = null,
        string creatorRole = "Admin",
        Guid? headDepartmentDoctorAccountId = null,
        ProfileRole role = ProfileRole.Doctor
    ) =>
        new()
        {
            SagaId = sagaId ?? Guid.CreateVersion7(),
            AccountId = accountId ?? Guid.CreateVersion7(),
            CreatorAccountId = creatorAccountId ?? Guid.CreateVersion7(),
            CreatorRole = creatorRole,
            HeadDepartmentDoctorAccountId = headDepartmentDoctorAccountId ?? Guid.Empty,
            Role = role,
            FirstName = "Nguyen",
            LastName = "Van A",
            Degrees = [DegreeType.Doctor],
            AcademicTitle = AcademicTitle.None,
            Position = PositionType.StaffDoctor,
            Gender = GenderType.Male,
            CitizenIdentificationNumber = "001088012345",
            DateOfBirth = new DateTimeOffset(1990, 5, 20, 0, 0, 0, TimeSpan.Zero),
            Address = "12 Le Loi, District 1, Ho Chi Minh City",
        };

    /// <summary>
    /// Wraps a real context so the handler queries hit the database but the write fails.
    /// </summary>
    private sealed class FailingSaveDoctorDbContext(DoctorDbContext inner)
        : Respira.Doctor.Application.Contracts.Data.IDoctorDbContext
    {
        public DbSet<Respira.Doctor.Domain.Entities.Doctor> Doctors => inner.Doctors;

        public DbSet<HeadDepartmentDoctor> HeadDepartmentDoctors =>
            inner.HeadDepartmentDoctors;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("save failed");
    }
}
