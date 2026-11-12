using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Contracts.Enrichment;
using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Mappers;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetPagedDoctor;

public sealed class GetPagedDoctorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly StubEnricher _enricher = new();

    public GetPagedDoctorHandlerTest(PostgresFixture fixture)
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
    public async Task HandleAsync_WithoutRows_ReturnsEmptyItemsAndZeroMetadata()
    {
        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        var items = result.Data!.Items.ToList();
        Assert.Empty(items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        Assert.True(result.Data.Metadata.PageCount is 0 or 1);
        Assert.False(result.Data.Metadata.HasNextPage);
        Assert.False(result.Data.Metadata.HasPreviousPage);
        Assert.Equal(1, result.Data.Metadata.CurrentPage);
    }

    [Fact]
    public async Task HandleAsync_WithoutRoleFilter_ReturnsDoctorsAndHeadDepartmentDoctors()
    {
        await SeedHeadAsync("Tran", "Thi B", createdAt: Hour(1));
        await SeedDoctorAsync("Nguyen", "Van A", createdAt: Hour(2));
        await SeedDoctorAsync("Le", "Thi C", createdAt: Hour(3));

        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        var items = result.Data!.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(3, result.Data.Metadata.TotalItemCount);
        Assert.Contains(items, x => x.Role == ProfileRole.Doctor);
        Assert.Contains(items, x => x.Role == ProfileRole.HeadDepartmentDoctor);
    }

    [Fact]
    public async Task HandleAsync_WithRoleDoctor_ReturnsOnlyDoctors()
    {
        await SeedHeadAsync("Tran", "Thi B", createdAt: Hour(1));
        await SeedDoctorAsync("Nguyen", "Van A", createdAt: Hour(2));
        await SeedDoctorAsync("Le", "Thi C", createdAt: Hour(3));

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter { Role = ProfileRole.Doctor },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        Assert.All(items, x => Assert.Equal(ProfileRole.Doctor, x.Role));
        Assert.DoesNotContain(items, x => x.Role == ProfileRole.HeadDepartmentDoctor);
    }

    [Fact]
    public async Task HandleAsync_WithRoleHeadDepartmentDoctor_ReturnsOnlyHeadDepartmentDoctors()
    {
        await SeedHeadAsync("Tran", "Thi B", createdAt: Hour(1));
        await SeedDoctorAsync("Nguyen", "Van A", createdAt: Hour(2));
        await SeedDoctorAsync("Le", "Thi C", createdAt: Hour(3));

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter { Role = ProfileRole.HeadDepartmentDoctor },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Tran", items[0].FirstName);
        Assert.Equal(ProfileRole.HeadDepartmentDoctor, items[0].Role);
    }

    [Fact]
    public async Task HandleAsync_WithGenderFilter_ReturnsOnlyMatchingGender()
    {
        await SeedDoctorAsync("Nguyen", "Van A", gender: GenderType.Male, createdAt: Hour(1));
        await SeedDoctorAsync("Le", "Thi C", gender: GenderType.Female, createdAt: Hour(2));
        await SeedHeadAsync("Tran", "Thi B", gender: GenderType.Female, createdAt: Hour(3));

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter { Gender = GenderType.Female },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.Equal(GenderType.Female, x.Gender));
    }

    [Fact]
    public async Task HandleAsync_WithAcademicTitleFilter_ReturnsOnlyMatchingAcademicTitle()
    {
        await SeedDoctorAsync(
            "Nguyen",
            "Van A",
            academicTitle: AcademicTitle.None,
            createdAt: Hour(1)
        );
        await SeedDoctorAsync(
            "Le",
            "Thi C",
            academicTitle: AcademicTitle.AssociateProfessor,
            createdAt: Hour(2)
        );
        await SeedHeadAsync(
            "Tran",
            "Thi B",
            academicTitle: AcademicTitle.Professor,
            createdAt: Hour(3)
        );

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter { AcademicTitle = AcademicTitle.Professor },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Tran", items[0].FirstName);
        Assert.Equal(AcademicTitle.Professor, items[0].AcademicTitle);
    }

    [Fact]
    public async Task HandleAsync_WithPositionFilter_ReturnsOnlyMatchingPosition()
    {
        await SeedDoctorAsync(
            "Nguyen",
            "Van A",
            position: PositionType.StaffDoctor,
            createdAt: Hour(1)
        );
        await SeedDoctorAsync(
            "Le",
            "Thi C",
            position: PositionType.SeniorDoctor,
            createdAt: Hour(2)
        );
        await SeedHeadAsync(
            "Tran",
            "Thi B",
            position: PositionType.HeadOfDepartment,
            createdAt: Hour(3)
        );

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter { Position = PositionType.SeniorDoctor },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Le", items[0].FirstName);
        Assert.Equal(PositionType.SeniorDoctor, items[0].Position);
    }

    [Fact]
    public async Task HandleAsync_WithRoleAndGenderFilter_ReturnsMatchingCombination()
    {
        await SeedDoctorAsync("Nguyen", "Van A", gender: GenderType.Male, createdAt: Hour(1));
        await SeedDoctorAsync(
            "Le",
            "Thi C",
            gender: GenderType.Female,
            createdAt: Hour(2)
        );
        await SeedHeadAsync("Tran", "Thi B", gender: GenderType.Female, createdAt: Hour(3));

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter
                {
                    Role = ProfileRole.Doctor,
                    Gender = GenderType.Female,
                },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Le", items[0].FirstName);
        Assert.Equal(ProfileRole.Doctor, items[0].Role);
        Assert.Equal(GenderType.Female, items[0].Gender);
    }

    [Fact]
    public async Task HandleAsync_WithFilterMatchingNothing_ReturnsEmptyItems()
    {
        await SeedDoctorAsync("Nguyen", "Van A", createdAt: Hour(1));
        await SeedHeadAsync("Tran", "Thi B", createdAt: Hour(2));

        var result = await HandleAsync(
            new GetPagedDoctorQuery
            {
                Param = BuildParam(),
                Filter = new DoctorFilter { Position = PositionType.Director },
            }
        );

        var items = result.Data!.Items.ToList();
        Assert.Empty(items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_OrdersByCreatedAtDescending()
    {
        await SeedDoctorAsync("Oldest", "Van A", createdAt: Hour(1));
        await SeedDoctorAsync("Middle", "Thi C", createdAt: Hour(2));
        await SeedDoctorAsync("Newest", "Thi D", createdAt: Hour(3));

        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        var items = result.Data!.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal("Newest", items[0].FirstName);
        Assert.Equal("Middle", items[1].FirstName);
        Assert.Equal("Oldest", items[2].FirstName);
    }

    [Fact]
    public async Task HandleAsync_WithSameCreatedAt_OrdersByIdAscending()
    {
        var sameInstant = Hour(5);
        var first = await SeedDoctorAsync("First", "Van A", createdAt: sameInstant);
        var second = await SeedDoctorAsync("Second", "Thi C", createdAt: sameInstant);

        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        var items = result.Data!.Items.ToList();
        Assert.Equal(2, items.Count);
        var expectedOrder = new[] { first.Id, second.Id }.OrderBy(x => x).ToList();
        Assert.Equal(expectedOrder, items.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task HandleAsync_WithFirstPageOfFiveRows_ReturnsNextPageAndPageCount()
    {
        for (var i = 0; i < 5; i++)
        {
            await SeedDoctorAsync($"Doctor{i}", "Van A", createdAt: Hour(i + 1));
        }

        var result = await HandleAsync(
            new GetPagedDoctorQuery { Param = new PaginationParam { Page = 1, Size = 2 } }
        );

        var items = result.Data!.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(5, result.Data.Metadata.TotalItemCount);
        Assert.Equal(3, result.Data.Metadata.PageCount);
        Assert.True(result.Data.Metadata.HasNextPage);
        Assert.False(result.Data.Metadata.HasPreviousPage);
        Assert.Equal(1, result.Data.Metadata.CurrentPage);
        Assert.Equal(2, result.Data.Metadata.PageSize);
    }

    [Fact]
    public async Task HandleAsync_WithLastPage_ReturnsPreviousPageAndNoNextPage()
    {
        for (var i = 0; i < 5; i++)
        {
            await SeedDoctorAsync($"Doctor{i}", "Van A", createdAt: Hour(i + 1));
        }

        var result = await HandleAsync(
            new GetPagedDoctorQuery { Param = new PaginationParam { Page = 3, Size = 2 } }
        );

        var items = result.Data!.Items.ToList();
        Assert.Single(items);
        Assert.Equal(5, result.Data.Metadata.TotalItemCount);
        Assert.Equal(3, result.Data.Metadata.PageCount);
        Assert.False(result.Data.Metadata.HasNextPage);
        Assert.True(result.Data.Metadata.HasPreviousPage);
        Assert.Equal(3, result.Data.Metadata.CurrentPage);
    }

    [Fact]
    public async Task HandleAsync_WithPageBeyondRange_ReturnsEmptyItems()
    {
        for (var i = 0; i < 5; i++)
        {
            await SeedDoctorAsync($"Doctor{i}", "Van A", createdAt: Hour(i + 1));
        }

        var result = await HandleAsync(
            new GetPagedDoctorQuery { Param = new PaginationParam { Page = 10, Size = 2 } }
        );

        var items = result.Data!.Items.ToList();
        Assert.Empty(items);
        Assert.Equal(5, result.Data.Metadata.TotalItemCount);
        Assert.Equal(3, result.Data.Metadata.PageCount);
    }

    [Fact]
    public async Task HandleAsync_ExcludesSoftDeletedProfiles()
    {
        await SeedDoctorAsync("Visible", "Van A", createdAt: Hour(1));
        await SeedDoctorAsync(
            "Hidden",
            "Thi C",
            isDeleted: true,
            createdAt: Hour(2)
        );
        await SeedHeadAsync(
            "HiddenHead",
            "Thi D",
            isDeleted: true,
            createdAt: Hour(3)
        );

        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        var items = result.Data!.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Visible", items[0].FirstName);
        Assert.Equal(1, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_PassesThePagedProfilesToTheEnricher()
    {
        await SeedDoctorAsync("Nguyen", "Van A", createdAt: Hour(1));

        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        Assert.Equal(1, _enricher.CallCount);
        Assert.Equal(1, _enricher.EnrichedItemCount);
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal($"enriched-{item.AccountId}", item.Email);
        Assert.Equal("Doctor", item.AccountRole);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccessStatusCode()
    {
        await SeedDoctorAsync("Nguyen", "Van A", createdAt: Hour(1));

        var result = await HandleAsync(new GetPagedDoctorQuery { Param = BuildParam() });

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.Null(result.Error);
    }

    #endregion

    private async Task<Result<Pagination<PagedDoctorItem>>> HandleAsync(GetPagedDoctorQuery query)
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new GetPagedDoctorHandler(context, new PaginationFactory(), _enricher);

        return await handler.HandleAsync(query, TestContext.Current.CancellationToken);
    }

    private static PaginationParam BuildParam() => new() { Page = 1, Size = 10 };

    private static DateTimeOffset Hour(int offset) =>
        new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero).AddHours(offset);

    private async Task<DoctorEntity> SeedDoctorAsync(
        string firstName,
        string lastName,
        GenderType gender = GenderType.Male,
        AcademicTitle academicTitle = AcademicTitle.None,
        PositionType position = PositionType.StaffDoctor,
        DateTimeOffset? createdAt = null,
        bool isDeleted = false
    )
    {
        await using var context = new DoctorDbContext(_options);
        var doctor = TestData.DoctorFactory.CreateDoctor(
            firstName: firstName,
            lastName: lastName,
            citizenIdentificationNumber: $"9{Guid.CreateVersion7():N}"[..12],
            gender: gender,
            academicTitle: academicTitle,
            position: position,
            createdAt: createdAt ?? DateTimeOffset.UtcNow,
            isDeleted: isDeleted
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task SeedHeadAsync(
        string firstName,
        string lastName,
        GenderType gender = GenderType.Female,
        AcademicTitle academicTitle = AcademicTitle.Professor,
        PositionType position = PositionType.HeadOfDepartment,
        DateTimeOffset? createdAt = null,
        bool isDeleted = false
    )
    {
        await using var context = new DoctorDbContext(_options);
        var head = TestData.DoctorFactory.CreateHeadDepartmentDoctor(
            firstName: firstName,
            lastName: lastName,
            citizenIdentificationNumber: $"8{Guid.CreateVersion7():N}"[..12],
            gender: gender,
            academicTitle: academicTitle,
            position: position,
            createdAt: createdAt ?? DateTimeOffset.UtcNow,
            isDeleted: isDeleted
        );
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Enricher stub: it never talks to the other microservices, it only fills the
    /// account fields with deterministic fake data.
    /// </summary>
    private sealed class StubEnricher : IDoctorProfileEnricher
    {
        public int CallCount { get; private set; }

        public int EnrichedItemCount { get; private set; }

        public Task EnrichAsync(
            IEnumerable<DoctorProfileItem> profiles,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            var items = profiles.ToList();
            EnrichedItemCount = items.Count;

            foreach (var item in items)
            {
                item.Email = $"enriched-{item.AccountId}";
                item.Phone = "0900000000";
                item.AccountRole = "Doctor";
                item.AccountStatus = "Active";

                if (item.MediaId.HasValue)
                {
                    item.AvatarUrl = $"https://media.test/{item.MediaId.Value}";
                }
            }

            return Task.CompletedTask;
        }
    }
}
