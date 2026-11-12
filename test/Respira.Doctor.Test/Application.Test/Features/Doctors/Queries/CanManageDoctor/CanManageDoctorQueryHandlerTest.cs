using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Features.Doctors.Queries.CanManageDoctor;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Doctors;
using Respira.ServiceDefaults.Contracts.Results;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.CanManageDoctor;

public sealed class CanManageDoctorQueryHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;

    public CanManageDoctorQueryHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
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

    #region Allowed

    [Fact]
    public async Task HandleAsync_WithAdminActor_AllowsManagingADoctor()
    {
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithAdminActor_AllowsManagingAHeadDepartmentDoctor()
    {
        var head = await SeedHeadAsync();

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: head.Id,
            targetAccountId: head.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithDoctorActorAndMatchingAccount_AllowsManagingItself()
    {
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: doctor.AccountId,
            actorRole: "Doctor",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithHeadActor_AllowsManagingItsOwnSubordinateDoctor()
    {
        var head = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: head.Id);

        var result = await HandleAsync(
            actorAccountId: head.AccountId,
            actorRole: "HeadDepartmentDoctor",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithHeadActor_AllowsManagingItsOwnHeadProfile()
    {
        var head = await SeedHeadAsync();

        var result = await HandleAsync(
            actorAccountId: head.AccountId,
            actorRole: "HeadDepartmentDoctor",
            doctorId: head.Id,
            targetAccountId: head.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.True(result.Data!.Allowed);
    }

    #endregion

    #region Denied

    [Fact]
    public async Task HandleAsync_WithDoctorActorAndDifferentAccount_DoesNotAllowManaging()
    {
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Doctor",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithHeadActor_ThatDoesNotManageTheDoctor_DoesNotAllowManaging()
    {
        var head = await SeedHeadAsync();
        var otherHead = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: head.Id);

        var result = await HandleAsync(
            actorAccountId: otherHead.AccountId,
            actorRole: "HeadDepartmentDoctor",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithHeadActor_AndUnassignedDoctor_DoesNotAllowManaging()
    {
        var head = await SeedHeadAsync();
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: head.AccountId,
            actorRole: "HeadDepartmentDoctor",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownRole_DoesNotAllowManaging()
    {
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: doctor.AccountId,
            actorRole: "Guest",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyActorRole_DoesNotAllowManaging()
    {
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: doctor.AccountId,
            actorRole: string.Empty,
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownDoctorId_DoesNotAllowManaging()
    {
        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: Guid.CreateVersion7(),
            targetAccountId: Guid.CreateVersion7()
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
        Assert.Null(result.Data!.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithMismatchedTargetAccountId_DoesNotAllowManaging()
    {
        var doctor = await SeedDoctorAsync();

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: doctor.Id,
            targetAccountId: Guid.CreateVersion7()
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
        Assert.Null(result.Data!.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedDoctor_DoesNotAllowManaging()
    {
        var doctor = await SeedDoctorAsync(isDeleted: true);

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
        Assert.Null(result.Data!.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithDoctorActor_TargetingAHeadProfile_DoesNotAllowManaging()
    {
        var head = await SeedHeadAsync();

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Doctor",
            doctorId: head.Id,
            targetAccountId: head.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.False(result.Data!.Allowed);
    }

    #endregion

    #region Media

    [Fact]
    public async Task HandleAsync_WithProfileThatHasMedia_ReturnsTheMediaId()
    {
        var mediaId = Guid.CreateVersion7();
        var doctor = await SeedDoctorAsync(mediaId: mediaId);

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(mediaId, result.Data!.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithProfileWithoutMedia_ReturnsNullMediaId()
    {
        var doctor = await SeedDoctorAsync(mediaId: null);

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: doctor.Id,
            targetAccountId: doctor.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.Null(result.Data!.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithHeadProfileThatHasMedia_ReturnsTheMediaId()
    {
        var mediaId = Guid.CreateVersion7();
        var head = await SeedHeadAsync(mediaId: mediaId);

        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: head.Id,
            targetAccountId: head.AccountId
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(mediaId, result.Data!.MediaId);
    }

    #endregion

    [Fact]
    public async Task HandleAsync_AlwaysReturnsSuccessStatusCode()
    {
        var result = await HandleAsync(
            actorAccountId: Guid.CreateVersion7(),
            actorRole: "Admin",
            doctorId: Guid.CreateVersion7(),
            targetAccountId: Guid.CreateVersion7()
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
    }

    private async Task<Result<DoctorManagementAuthorization>> HandleAsync(
        Guid actorAccountId,
        string actorRole,
        Guid doctorId,
        Guid targetAccountId
    )
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new CanManageDoctorQueryHandler(context);

        return await handler.HandleAsync(
            new CanManageDoctorQuery
            {
                ActorAccountId = actorAccountId,
                ActorRole = actorRole,
                DoctorId = doctorId,
                TargetAccountId = targetAccountId,
            },
            TestContext.Current.CancellationToken
        );
    }

    private async Task<DoctorEntity> SeedDoctorAsync(
        Guid? headDepartmentDoctorId = null,
        Guid? mediaId = null,
        bool isDeleted = false
    )
    {
        await using var context = new DoctorDbContext(_options);
        var doctor = TestData.DoctorFactory.CreateDoctor(
            citizenIdentificationNumber: $"9{Guid.CreateVersion7():N}"[..12],
            headDepartmentDoctorId: headDepartmentDoctorId,
            mediaId: mediaId,
            createdAt: DateTimeOffset.UtcNow,
            isDeleted: isDeleted
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<Respira.Doctor.Domain.Entities.HeadDepartmentDoctor> SeedHeadAsync(
        Guid? mediaId = null
    )
    {
        await using var context = new DoctorDbContext(_options);
        var head = TestData.DoctorFactory.CreateHeadDepartmentDoctor(
            citizenIdentificationNumber: $"8{Guid.CreateVersion7():N}"[..12],
            mediaId: mediaId,
            createdAt: DateTimeOffset.UtcNow
        );
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return head;
    }
}
