using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Features.Authentication.Queries.Common;
using Respira.Authentication.Application.Features.Authentication.Queries.GetList;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Authentication.Infrastructure.Test.Features.GetList;

public sealed class GetListAccountQueryHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static readonly Guid BootstrapAdminId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111"
    );

    private readonly DbContextOptions<AuthDbContext> _options;
    private readonly AuthDbContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    public GetListAccountQueryHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        _context = new AuthDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await _context
            .Tokens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .BlacklistTokens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .Accounts.IgnoreQueryFilters()
            .Where(x => x.Id != BootstrapAdminId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenQueryIsNull()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HandleAsync_ReturnsCachedPagination_WhenCacheHitOccurs()
    {
        var expectedItem = new AccountResult
        {
            Id = Guid.CreateVersion7(),
            Email = "cached@respira.ams",
            Phone = "0901112233",
            Role = RoleType.MedicalRegulationReviewer,
            IsEmailConfirmed = true,
            Status = StatusType.Active,
        };
        var cachedPagination = new Pagination<AccountResult>(
            new PaginationMetadata
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalItemCount = 1,
                PageCount = 1,
                HasPreviousPage = false,
                HasNextPage = false,
            },
            [expectedItem]
        );

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<Pagination<AccountResult>>(
                    "auth:accounts:list:role:MedicalRegulationReviewer:status:Active:confirmed:True:page:1:size:10",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(cachedPagination);

        var handler = CreateHandler();
        var query = new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AccountFilter
            {
                Role = RoleType.MedicalRegulationReviewer,
                Status = StatusType.Active,
                IsEmailConfirmed = true,
            },
            CallerRole = RoleType.Admin,
        };

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.Same(cachedPagination, result.Data);
        _cacheServiceMock.Verify(
            x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Pagination<AccountResult>>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task HandleAsync_WithoutFilter_ReturnsAllNonDeletedAccountsOrderedAndCachesResult()
    {
        var t1 = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);

        var doctor = await SeedAccountAsync("doctor@respira.ams", RoleType.Doctor, t1);
        var hod = await SeedAccountAsync(
            "hod@respira.ams",
            RoleType.HeadDepartmentDoctor,
            t2
        );
        var reviewer = await SeedAccountAsync(
            "reviewer@respira.ams",
            RoleType.MedicalRegulationReviewer,
            t3
        );
        await SeedAccountAsync(
            "deleted@respira.ams",
            RoleType.Doctor,
            t3.AddDays(1),
            isDeleted: true
        );

        var handler = CreateHandler();
        var query = new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = 1, Size = 10 },
            CallerRole = RoleType.Admin,
        };

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(4, result.Data!.Metadata.TotalItemCount);
        Assert.Equal(
            [reviewer.Id, hod.Id, doctor.Id, BootstrapAdminId],
            result.Data.Items.Select(x => x.Id).ToArray()
        );

        _cacheServiceMock.Verify(
            x =>
                x.SetAsync(
                    "auth:accounts:list:role:all:status:all:confirmed:all:page:1:size:10",
                    It.Is<Pagination<AccountResult>>(p => p.Metadata.TotalItemCount == 4),
                    TimeSpan.FromMinutes(2),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(RoleType.MedicalRegulationReviewer)]
    [InlineData(RoleType.HeadDepartmentDoctor)]
    [InlineData(RoleType.Doctor)]
    public async Task HandleAsync_WithRoleFilter_ReturnsOnlyMatchingRoleAndCachesUnderRoleKey(
        RoleType filterRole
    )
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await SeedAccountAsync("doctor1@respira.ams", RoleType.Doctor, baseTime);
        await SeedAccountAsync(
            "hod1@respira.ams",
            RoleType.HeadDepartmentDoctor,
            baseTime.AddHours(1)
        );
        await SeedAccountAsync(
            "hod2@respira.ams",
            RoleType.HeadDepartmentDoctor,
            baseTime.AddHours(2)
        );
        await SeedAccountAsync(
            "reviewer1@respira.ams",
            RoleType.MedicalRegulationReviewer,
            baseTime.AddHours(3)
        );
        await SeedAccountAsync(
            "deleted-role@respira.ams",
            filterRole,
            baseTime.AddHours(4),
            isDeleted: true
        );

        var handler = CreateHandler();
        var query = new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AccountFilter { Role = filterRole },
            CallerRole = RoleType.Admin,
        };

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.Items);
        Assert.All(result.Data.Items, item => Assert.Equal(filterRole, item.Role));

        var expectedCacheKey =
            $"auth:accounts:list:role:{filterRole}:status:all:confirmed:all:page:1:size:10";
        _cacheServiceMock.Verify(
            x =>
                x.SetAsync(
                    expectedCacheKey,
                    It.IsAny<Pagination<AccountResult>>(),
                    TimeSpan.FromMinutes(2),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WithStatusAndEmailConfirmedFilters_ReturnsOnlyMatchingAccounts()
    {
        var baseTime = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var activeConfirmed = await SeedAccountAsync(
            "active-confirmed@respira.ams",
            RoleType.Doctor,
            baseTime,
            status: StatusType.Active,
            isEmailConfirmed: true
        );
        await SeedAccountAsync(
            "active-unconfirmed@respira.ams",
            RoleType.Doctor,
            baseTime.AddMinutes(1),
            status: StatusType.Active,
            isEmailConfirmed: false
        );
        var inactiveUnconfirmed = await SeedAccountAsync(
            "inactive-unconfirmed@respira.ams",
            RoleType.Doctor,
            baseTime.AddMinutes(2),
            status: StatusType.Inactive,
            isEmailConfirmed: false
        );

        var handler = CreateHandler();

        var confirmedActiveResult = await handler.HandleAsync(
            new GetListAccountQuery
            {
                PaginationParam = new PaginationParam { Page = 1, Size = 10 },
                Filter = new AccountFilter
                {
                    Role = RoleType.Doctor,
                    Status = StatusType.Active,
                    IsEmailConfirmed = true,
                },
                CallerRole = RoleType.Admin,
            },
            TestContext.Current.CancellationToken
        );

        var inactiveResult = await handler.HandleAsync(
            new GetListAccountQuery
            {
                PaginationParam = new PaginationParam { Page = 1, Size = 10 },
                Filter = new AccountFilter
                {
                    Role = RoleType.Doctor,
                    Status = StatusType.Inactive,
                    IsEmailConfirmed = false,
                },
                CallerRole = RoleType.Admin,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(confirmedActiveResult.IsSuccess());
        var confirmedItem = Assert.Single(confirmedActiveResult.Data!.Items);
        Assert.Equal(activeConfirmed.Id, confirmedItem.Id);

        Assert.True(inactiveResult.IsSuccess());
        var inactiveItem = Assert.Single(inactiveResult.Data!.Items);
        Assert.Equal(inactiveUnconfirmed.Id, inactiveItem.Id);
    }

    [Theory]
    [InlineData(RoleType.Doctor)]
    [InlineData(RoleType.MedicalRegulationReviewer)]
    public async Task HandleAsync_ReturnsRestricted_WhenCallerRoleIsNotAdminOrHeadDepartmentDoctor(
        RoleType unauthorizedCallerRole
    )
    {
        var handler = CreateHandler();
        var query = new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = 1, Size = 10 },
            CallerRole = unauthorizedCallerRole,
        };

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
    }

    [Theory]
    [InlineData(RoleType.Admin)]
    [InlineData(RoleType.HeadDepartmentDoctor)]
    [InlineData(RoleType.MedicalRegulationReviewer)]
    public async Task HandleAsync_ReturnsRestricted_WhenHeadDepartmentDoctorRequestsNonDoctorRole(
        RoleType requestedRole
    )
    {
        var handler = CreateHandler();
        var query = new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = 1, Size = 10 },
            Filter = new AccountFilter { Role = requestedRole },
            CallerRole = RoleType.HeadDepartmentDoctor,
        };

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ScopesToDoctorRole_WhenCallerIsHeadDepartmentDoctorWithoutRoleFilter()
    {
        var baseTime = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var doctor = await SeedAccountAsync("hod-visible-doc@respira.ams", RoleType.Doctor, baseTime);
        await SeedAccountAsync(
            "hod-other@respira.ams",
            RoleType.HeadDepartmentDoctor,
            baseTime.AddMinutes(1)
        );
        await SeedAccountAsync(
            "reviewer-other@respira.ams",
            RoleType.MedicalRegulationReviewer,
            baseTime.AddMinutes(2)
        );

        var handler = CreateHandler();
        var query = new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = 1, Size = 10 },
            CallerRole = RoleType.HeadDepartmentDoctor,
        };

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        var single = Assert.Single(result.Data!.Items);
        Assert.Equal(doctor.Id, single.Id);
        Assert.Equal(RoleType.Doctor, single.Role);
    }

    [Fact]
    public async Task HandleAsync_PaginationMetadata_SplitsAcrossPagesAccurately()
    {
        var baseTime = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var hod1 = await SeedAccountAsync(
            "hod-p1@respira.ams",
            RoleType.HeadDepartmentDoctor,
            baseTime
        );
        var hod2 = await SeedAccountAsync(
            "hod-p2@respira.ams",
            RoleType.HeadDepartmentDoctor,
            baseTime.AddMinutes(1)
        );
        var hod3 = await SeedAccountAsync(
            "hod-p3@respira.ams",
            RoleType.HeadDepartmentDoctor,
            baseTime.AddMinutes(2)
        );

        var handler = CreateHandler();

        var page1 = await handler.HandleAsync(
            new GetListAccountQuery
            {
                PaginationParam = new PaginationParam { Page = 1, Size = 2 },
                Filter = new AccountFilter { Role = RoleType.HeadDepartmentDoctor },
                CallerRole = RoleType.Admin,
            },
            TestContext.Current.CancellationToken
        );
        var page2 = await handler.HandleAsync(
            new GetListAccountQuery
            {
                PaginationParam = new PaginationParam { Page = 2, Size = 2 },
                Filter = new AccountFilter { Role = RoleType.HeadDepartmentDoctor },
                CallerRole = RoleType.Admin,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(page1.IsSuccess());
        Assert.Equal([hod3.Id, hod2.Id], page1.Data!.Items.Select(x => x.Id).ToArray());
        Assert.Equal(3, page1.Data.Metadata.TotalItemCount);
        Assert.Equal(2, page1.Data.Metadata.PageCount);
        Assert.False(page1.Data.Metadata.HasPreviousPage);
        Assert.True(page1.Data.Metadata.HasNextPage);

        Assert.True(page2.IsSuccess());
        Assert.Equal([hod1.Id], page2.Data!.Items.Select(x => x.Id).ToArray());
        Assert.True(page2.Data.Metadata.HasPreviousPage);
        Assert.False(page2.Data.Metadata.HasNextPage);
    }

    private GetListAccountQueryHandler CreateHandler() =>
        new(_context, _cacheServiceMock.Object, NullLogger<GetListAccountQueryHandler>.Instance);

    private async Task<Account> SeedAccountAsync(
        string email,
        RoleType role,
        DateTimeOffset createdAt,
        bool isDeleted = false,
        StatusType status = StatusType.Active,
        bool isEmailConfirmed = true
    )
    {
        var account = new Account
        {
            Email = email,
            HashPassword = "$2a$12$hashedpassword",
            Phone = "0901234567",
            Role = role,
            IsEmailConfirmed = isEmailConfirmed,
            Status = status,
            CreatedAt = createdAt,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? createdAt.AddHours(1) : null,
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return account;
    }
}
