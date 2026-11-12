using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Contracts.Enrichment;
using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Mappers;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.SearchDoctor;

public sealed class SearchDoctorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly StubEnricher _enricher = new();

    public SearchDoctorHandlerTest(PostgresFixture fixture)
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
    public async Task HandleAsync_WithEmptySearchTerm_ReturnsEmptyResult()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync(string.Empty);

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        var items = result.Data!.Items.ToList();
        Assert.Empty(items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        Assert.Equal(0, result.Data.Metadata.PageCount);
        Assert.False(result.Data.Metadata.HasNextPage);
        Assert.False(result.Data.Metadata.HasPreviousPage);
    }

    [Fact]
    public async Task HandleAsync_WithEmptySearchTerm_SkipsTheEnricher()
    {
        var result = await HandleAsync(string.Empty);

        Assert.True(result.IsSuccess());
        Assert.Equal(0, _enricher.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithSymbolOnlySearchTerm_ReturnsEmptyResult()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("!!!@@@");

        Assert.True(result.IsSuccess());
        Assert.Empty(result.Data!.Items.ToList());
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_WithSingleCharacterSearchTerm_ReturnsEmptyResult()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("a");

        Assert.True(result.IsSuccess());
        Assert.Empty(result.Data!.Items.ToList());
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_MatchingFirstName_ReturnsTheProfile()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");
        await SeedDoctorAsync("Le", "Quyen");

        var result = await HandleAsync("nguyen");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Nguyen", item.FirstName);
        Assert.Equal("Phuong", item.LastName);
        Assert.Equal(ProfileRole.Doctor, item.Role);
        Assert.Equal(1, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_MatchingLastName_ReturnsTheProfile()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");
        await SeedDoctorAsync("Le", "Quyen");

        var result = await HandleAsync("quyen");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Le", item.FirstName);
        Assert.Equal("Quyen", item.LastName);
    }

    [Fact]
    public async Task HandleAsync_MatchingCitizenIdentificationNumber_ReturnsTheProfile()
    {
        await SeedDoctorAsync("Nguyen", "Phuong", citizenIdentificationNumber: "001088012345");
        await SeedDoctorAsync(
            "Le",
            "Quyen",
            citizenIdentificationNumber: "001088022222"
        );

        var result = await HandleAsync("001088012345");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Nguyen", item.FirstName);
        Assert.Equal("001088012345", item.CitizenIdentificationNumber);
    }

    [Fact]
    public async Task HandleAsync_WithPartialWord_ReturnsTheProfile()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("ngu");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Nguyen", item.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentCase_ReturnsTheProfile()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("nguyen");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Nguyen", item.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WithAccentedStoredName_IgnoresTheAccent()
    {
        await SeedDoctorAsync("Nguyễn", "Phuong");

        var result = await HandleAsync("nguyen");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Nguyễn", item.FirstName);
    }

    [Fact]
    public async Task HandleAsync_WithAccentedSearchTerm_IgnoresTheAccent()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("nguyễn");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Nguyen", item.FirstName);
    }

    [Fact]
    public async Task HandleAsync_MatchingHeadDepartmentDoctor_ReturnsTheHeadProfile()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");
        await SeedHeadAsync("Tran", "Binh");

        var result = await HandleAsync("tran");

        Assert.True(result.IsSuccess());
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal("Tran", item.FirstName);
        Assert.Equal(ProfileRole.HeadDepartmentDoctor, item.Role);
    }

    [Fact]
    public async Task HandleAsync_WithoutMatch_ReturnsEmptyResult()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("zzzzz");

        Assert.True(result.IsSuccess());
        Assert.Empty(result.Data!.Items.ToList());
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_ExcludesSoftDeletedProfiles()
    {
        await SeedDoctorAsync("Pham", "Thao", isDeleted: true);
        await SeedHeadAsync("Dang", "Binh", isDeleted: true);

        var result = await HandleAsync("pham");

        Assert.True(result.IsSuccess());
        Assert.Empty(result.Data!.Items.ToList());
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Theory]
    [InlineData("!")]
    [InlineData("&")]
    [InlineData("a&b")]
    [InlineData("a!b")]
    [InlineData("a(b")]
    [InlineData("nguyen & !pham")]
    [InlineData("beta-lactam")]
    public async Task HandleAsync_WithSanitizedSearchTerm_DoesNotThrowAndReturnsEmptyResult(
        string searchTerm
    )
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync(searchTerm);

        Assert.True(result.IsSuccess());
        Assert.Empty(result.Data!.Items.ToList());
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccessStatusCode()
    {
        await SeedDoctorAsync("Nguyen", "Phuong");

        var result = await HandleAsync("nguyen");

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.Null(result.Error);
    }

    #endregion

    private async Task<Result<Pagination<SearchDoctorItem>>> HandleAsync(string searchTerm)
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new SearchDoctorHandler(context, new PaginationFactory(), _enricher);

        return await handler.HandleAsync(
            new SearchDoctorQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            },
            TestContext.Current.CancellationToken
        );
    }

    private async Task<DoctorEntity> SeedDoctorAsync(
        string firstName,
        string lastName,
        string? citizenIdentificationNumber = null,
        bool isDeleted = false
    )
    {
        await using var context = new DoctorDbContext(_options);
        var doctor = TestData.DoctorFactory.CreateDoctor(
            firstName: firstName,
            lastName: lastName,
            citizenIdentificationNumber:
                citizenIdentificationNumber ?? $"9{Guid.CreateVersion7():N}"[..12],
            createdAt: DateTimeOffset.UtcNow,
            isDeleted: isDeleted
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task SeedHeadAsync(
        string firstName,
        string lastName,
        string? citizenIdentificationNumber = null,
        bool isDeleted = false
    )
    {
        await using var context = new DoctorDbContext(_options);
        var head = TestData.DoctorFactory.CreateHeadDepartmentDoctor(
            firstName: firstName,
            lastName: lastName,
            citizenIdentificationNumber:
                citizenIdentificationNumber ?? $"8{Guid.CreateVersion7():N}"[..12],
            createdAt: DateTimeOffset.UtcNow,
            isDeleted: isDeleted
        );
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Enricher stub: it never talks to the other microservices.
    /// </summary>
    private sealed class StubEnricher : IDoctorProfileEnricher
    {
        public int CallCount { get; private set; }

        public Task EnrichAsync(
            IEnumerable<DoctorProfileItem> profiles,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
