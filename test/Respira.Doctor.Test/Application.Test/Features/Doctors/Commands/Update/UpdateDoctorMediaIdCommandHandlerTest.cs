using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Commands.Update;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Commands.Update;

public sealed class UpdateDoctorMediaIdCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly Mock<IMessageBus> _bus = new();
    private readonly UpdateDoctorMediaIdCommandHandler _handler;

    public UpdateDoctorMediaIdCommandHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new DoctorDbContext(_options);
        var logger = Mock.Of<ILogger<UpdateDoctorMediaIdCommandHandler>>();

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
    public async Task HandleAsync_WithMediaId_LinksMediaToDoctorProfile()
    {
        var mediaId = Guid.CreateVersion7();
        var seeded = await SeedDoctorAsync();

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: mediaId),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(mediaId, saved.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithMediaId_LinksMediaToHeadDepartmentDoctorProfile()
    {
        var mediaId = Guid.CreateVersion7();
        var seeded = await SeedHeadAsync();

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: mediaId),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.HeadDepartmentDoctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(mediaId, saved.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithMediaId_UpdatesTimestamp()
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var seeded = await SeedDoctorAsync(createdAt: createdAt);

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );

        Assert.True(saved.UpdatedAt > createdAt);
    }

    [Fact]
    public async Task HandleAsync_PublishesDoctorMediaUpdated()
    {
        var mediaId = Guid.CreateVersion7();
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin", mediaId: mediaId);

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var updated = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorMediaUpdated>()
        );
        Assert.Equal(command.SagaId, updated.SagaId);
        Assert.Equal(seeded.Id, updated.DoctorId);
        Assert.Equal(mediaId, updated.MediaId);
    }

    [Fact]
    public async Task HandleAsync_PublishesAuditEvent()
    {
        var seeded = await SeedDoctorAsync();
        var sagaId = Guid.CreateVersion7();
        var actorAccountId = Guid.CreateVersion7();
        var command = BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.CreateVersion7());
        command.SagaId = sagaId;
        command.ActorAccountId = actorAccountId;

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("Doctor.ProfileMediaLinked", auditEvent.Action);
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
            BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken
        );

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("Doctor.ProfileMediaLinked", auditEvent.Action);
        Assert.Equal("HeadDepartmentDoctor", auditEvent.EntityType);
        Assert.Equal(seeded.Id, auditEvent.EntityId);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyActorAccount_PublishesAuditEventWithoutActor()
    {
        var seeded = await SeedDoctorAsync();
        var command = BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.CreateVersion7());
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
        var command = BuildCommand(seeded, actorRole: "   ", mediaId: Guid.CreateVersion7());

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var auditEvent = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<AuditEvent>()
        );
        Assert.Equal("System", auditEvent.ActorRole);
    }

    [Fact]
    public async Task HandleAsync_WithExistingMediaId_ReplacesItWithTheNewOne()
    {
        var oldMediaId = Guid.CreateVersion7();
        var newMediaId = Guid.CreateVersion7();
        var seeded = await SeedDoctorAsync(mediaId: oldMediaId);

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: newMediaId),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(newMediaId, saved.MediaId);
        Assert.NotEqual(oldMediaId, saved.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyMediaId_WritesEmptyGuidAsIs()
    {
        // The handler does not guard against an empty media id, it is stored as is.
        var seeded = await SeedDoctorAsync(mediaId: Guid.CreateVersion7());

        await _handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.Empty),
            TestContext.Current.CancellationToken
        );

        await using var verification = new DoctorDbContext(_options);
        var saved = await verification.Doctors.SingleAsync(
            x => x.Id == seeded.Id,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(Guid.Empty, saved.MediaId);
    }

    #endregion

    #region Fail path

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_PublishesFailure()
    {
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorMediaUpdateFailure>()
        );
        Assert.Equal("Profile not found", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_DoesNotPublishDoctorMediaUpdated()
    {
        var command = BuildCommandForMissingProfile(actorRole: "Admin");

        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorMediaUpdated>()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_PublishesFailureWithExceptionMessage()
    {
        var seeded = await SeedDoctorAsync();
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new UpdateDoctorMediaIdCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<UpdateDoctorMediaIdCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken
        );

        var failure = Assert.Single(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorMediaUpdateFailure>()
        );
        Assert.Equal("save failed", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_DoesNotPublishDoctorMediaUpdated()
    {
        var seeded = await SeedDoctorAsync();
        var failingContext = new FailingSaveDoctorDbContext(new DoctorDbContext(_options));
        var handler = new UpdateDoctorMediaIdCommandHandler(
            failingContext,
            _bus.Object,
            Mock.Of<ILogger<UpdateDoctorMediaIdCommandHandler>>()
        );

        await handler.HandleAsync(
            BuildCommand(seeded, actorRole: "Admin", mediaId: Guid.CreateVersion7()),
            TestContext.Current.CancellationToken
        );

        Assert.Empty(
            _bus.Invocations.Select(x => x.Arguments[0]).OfType<DoctorMediaUpdated>()
        );
    }

    #endregion

    private async Task<DoctorEntity> SeedDoctorAsync(
        Guid? mediaId = null,
        DateTimeOffset? createdAt = null
    )
    {
        await using var context = new DoctorDbContext(_options);
        var doctor = TestData.DoctorFactory.CreateDoctor(
            mediaId: mediaId,
            createdAt: createdAt,
            citizenIdentificationNumber: NextCitizenIdentificationNumber()
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<HeadDepartmentDoctorEntity> SeedHeadAsync(
        Guid? mediaId = null,
        DateTimeOffset? createdAt = null
    )
    {
        await using var context = new DoctorDbContext(_options);
        var head = TestData.DoctorFactory.CreateHeadDepartmentDoctor(
            mediaId: mediaId,
            createdAt: createdAt,
            citizenIdentificationNumber: NextCitizenIdentificationNumber()
        );
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return head;
    }

    private static string NextCitizenIdentificationNumber() =>
        Guid.CreateVersion7().ToString("N")[..12];

    private static UpdateDoctorMediaIdCommand BuildCommand(
        BaseDoctor seeded,
        string actorRole,
        Guid mediaId
    ) =>
        new()
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = actorRole,
            DoctorId = seeded.Id,
            MediaId = mediaId,
        };

    /// <summary>
    /// A command that points at a profile id that does not exist in the database.
    /// </summary>
    private static UpdateDoctorMediaIdCommand BuildCommandForMissingProfile(string actorRole) =>
        new()
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = actorRole,
            DoctorId = Guid.CreateVersion7(),
            MediaId = Guid.CreateVersion7(),
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
