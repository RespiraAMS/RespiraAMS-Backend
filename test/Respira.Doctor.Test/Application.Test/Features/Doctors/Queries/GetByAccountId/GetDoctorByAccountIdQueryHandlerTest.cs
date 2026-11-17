using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Contracts.Enrichment;
using Respira.Doctor.Application.Features.Doctors.Queries.GetByAccountId;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Accounts;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetByAccountId;

public sealed class GetDoctorByAccountIdQueryHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;

    public GetDoctorByAccountIdQueryHandlerTest(PostgresFixture fixture)
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

    #region Happy path

    [Fact]
    public async Task HandleAsync_WithExistingDoctor_ReturnsSuccessWithEveryFieldMapped()
    {
        var accountId = Guid.CreateVersion7();
        var mediaId = Guid.CreateVersion7();
        var createdAt = new DateTimeOffset(2024, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var dateOfBirth = new DateTimeOffset(1990, 5, 20, 0, 0, 0, TimeSpan.Zero);

        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                accountId: accountId,
                firstName: "Nguyen",
                lastName: "Van A",
                citizenIdentificationNumber: "001088012345",
                mediaId: mediaId,
                gender: GenderType.Male,
                academicTitle: AcademicTitle.AssociateProfessor,
                position: PositionType.SeniorDoctor,
                dateOfBirth: dateOfBirth,
                createdAt: createdAt
            )
        );

        var result = await HandleAsync(accountId);

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.Null(result.Error);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(doctor.Id, data.Id);
        Assert.Equal(accountId, data.AccountId);
        Assert.Equal(ProfileRole.Doctor, data.Role);
        Assert.Equal("Nguyen", data.FirstName);
        Assert.Equal("Van A", data.LastName);
        Assert.Equal(AcademicTitle.AssociateProfessor, data.AcademicTitle);
        Assert.Equal(PositionType.SeniorDoctor, data.Position);
        Assert.Equal(GenderType.Male, data.Gender);
        Assert.Equal("001088012345", data.CitizenIdentificationNumber);
        Assert.Equal(dateOfBirth, data.DateOfBirth);
        Assert.Equal("12 Le Loi, District 1, Ho Chi Minh City", data.Address);
        Assert.Equal(mediaId, data.MediaId);
        Assert.Equal(createdAt, data.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_WithExistingHeadDepartmentDoctor_ReturnsSuccessWithHeadRole()
    {
        var accountId = Guid.CreateVersion7();
        var createdAt = new DateTimeOffset(2024, 4, 2, 9, 30, 0, TimeSpan.Zero);

        var head = await SeedHeadDepartmentDoctorAsync(
            TestData.DoctorFactory.CreateHeadDepartmentDoctor(
                accountId: accountId,
                firstName: "Tran",
                lastName: "Thi B",
                citizenIdentificationNumber: "001088054321",
                createdAt: createdAt
            )
        );

        var result = await HandleAsync(accountId);

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(head.Id, data.Id);
        Assert.Equal(accountId, data.AccountId);
        Assert.Equal(ProfileRole.HeadDepartmentDoctor, data.Role);
    }

    [Fact]
    public async Task HandleAsync_WithExistingDoctor_EnrichesPhoneAndEmailFromAuth()
    {
        var accountId = Guid.CreateVersion7();
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(accountId: accountId)
        );

        var busMock = new Mock<IMessageBus>();
        busMock
            .Setup(b => b.InvokeAsync<Result<List<AccountSummary>>>(
                It.Is<GetAccountsByIdsQuery>(q => q.AccountIds.Contains(accountId)),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()
            ))
            .ReturnsAsync(
                Result<List<AccountSummary>>.Success(
                    ApplicationStatus.Success,
                    [
                        new AccountSummary
                        {
                            Id = accountId,
                            Email = "doctor@respira.com",
                            Phone = "0987654321",
                            Role = "Doctor",
                            Status = "Active",
                        },
                    ]
                )
            );

        var result = await HandleAsync(accountId, busMock.Object);

        Assert.True(result.IsSuccess());
        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(doctor.Id, data.Id);
        Assert.Equal("0987654321", data.Phone);
        Assert.Equal("doctor@respira.com", data.Email);
    }

    [Fact]
    public async Task HandleAsync_WhenAuthFails_StillReturnsDoctorProfileWithNullPhoneAndEmail()
    {
        var accountId = Guid.CreateVersion7();
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(accountId: accountId)
        );

        var busMock = new Mock<IMessageBus>();
        busMock
            .Setup(b => b.InvokeAsync<Result<List<AccountSummary>>>(
                It.IsAny<GetAccountsByIdsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()
            ))
            .ThrowsAsync(new InvalidOperationException("Auth service unavailable"));

        var result = await HandleAsync(accountId, busMock.Object);

        Assert.True(result.IsSuccess());
        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(doctor.Id, data.Id);
        Assert.Null(data.Phone);
        Assert.Null(data.Email);
    }

    [Fact]
    public async Task HandleAsync_WithLinkedMedia_ReturnsResolvedAvatarUrl()
    {
        var accountId = Guid.CreateVersion7();
        var mediaId = Guid.CreateVersion7();
        await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(accountId: accountId, mediaId: mediaId)
        );

        var enricherMock = new Mock<IDoctorProfileEnricher>();
        enricherMock
            .Setup(e =>
                e.ResolveAvatarUrlAsync(mediaId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("https://cdn.hospital.vn/avatar.png");

        var result = await HandleAsync(accountId, enricher: enricherMock.Object);

        Assert.True(result.IsSuccess());
        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal("https://cdn.hospital.vn/avatar.png", data.AvatarUrl);
        enricherMock.Verify(
            e => e.ResolveAvatarUrlAsync(mediaId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WithoutMedia_ReturnsNullAvatarUrl()
    {
        var accountId = Guid.CreateVersion7();
        await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(accountId: accountId, mediaId: null)
        );

        var result = await HandleAsync(accountId);

        Assert.True(result.IsSuccess());
        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Null(data.AvatarUrl);
    }

    #endregion

    #region Not found / Validation

    [Fact]
    public async Task HandleAsync_WhenNotFound_ReturnsResourceNotFound()
    {
        var missingAccountId = Guid.CreateVersion7();

        var result = await HandleAsync(missingAccountId);

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Equal("Doctor profile not found", result.Error?.Description);
    }

    [Fact]
    public async Task HandleAsync_WhenEmptyAccountId_ReturnsBadRequest()
    {
        var result = await HandleAsync(Guid.Empty);

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        Assert.Equal("Account identifier is required", result.Error?.Description);
    }

    [Fact]
    public async Task HandleAsync_WhenQueryIsNull_ThrowsArgumentNullException()
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new GetDoctorByAccountIdQueryHandler(
            context,
            Mock.Of<IMessageBus>(),
            CreateEnricher(),
            Mock.Of<ILogger<GetDoctorByAccountIdQueryHandler>>()
        );

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    #endregion

    #region Helpers

    private async Task<Result<DoctorResult>> HandleAsync(
        Guid accountId,
        IMessageBus? bus = null,
        IDoctorProfileEnricher? enricher = null
    )
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new GetDoctorByAccountIdQueryHandler(
            context,
            bus ?? Mock.Of<IMessageBus>(),
            enricher ?? CreateEnricher(),
            Mock.Of<ILogger<GetDoctorByAccountIdQueryHandler>>()
        );

        return await handler.HandleAsync(
            new GetDoctorByAccountIdQuery { AccountId = accountId },
            TestContext.Current.CancellationToken
        );
    }

    private static IDoctorProfileEnricher CreateEnricher(string? avatarUrl = null)
    {
        var mock = new Mock<IDoctorProfileEnricher>();
        mock.Setup(e =>
                e.ResolveAvatarUrlAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(avatarUrl);

        return mock.Object;
    }

    private async Task<DoctorEntity> SeedDoctorAsync(DoctorEntity entity)
    {
        await using var context = new DoctorDbContext(_options);
        context.Doctors.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    private async Task<Respira.Doctor.Domain.Entities.HeadDepartmentDoctor> SeedHeadDepartmentDoctorAsync(
        Respira.Doctor.Domain.Entities.HeadDepartmentDoctor entity
    )
    {
        await using var context = new DoctorDbContext(_options);
        context.HeadDepartmentDoctors.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    #endregion
}
