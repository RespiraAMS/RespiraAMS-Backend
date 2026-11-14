using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Contracts.Enrichment;
using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.Doctor.Application.Features.Doctors.Queries.GetSubordinates;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Mappers;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetSubordinates;

public sealed class GetSubordinateDoctorsHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly StubEnricher _enricher = new();

    public GetSubordinateDoctorsHandlerTest(PostgresFixture fixture)
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

    [Fact]
    public async Task HandleAsync_WhenHeadDoctorNotFound_ReturnsResourceNotFound()
    {
        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = Guid.NewGuid(),
                Param = BuildParam(),
            }
        );

        Assert.False(result.IsSuccess());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WithoutSubordinates_ReturnsEmptyItems()
    {
        var head = await SeedHeadAsync("Head", "A");

        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = head.AccountId,
                Param = BuildParam(),
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.Metadata.TotalItemCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlySubordinatesOfCallingHeadDoctor()
    {
        var head1 = await SeedHeadAsync("Head", "One");
        var head2 = await SeedHeadAsync("Head", "Two");

        var sub1 = await SeedDoctorAsync("Doctor", "Sub1", headDepartmentDoctorId: head1.Id);
        var sub2 = await SeedDoctorAsync("Doctor", "Sub2", headDepartmentDoctorId: head1.Id);
        var otherDoc = await SeedDoctorAsync("Doctor", "Other", headDepartmentDoctorId: head2.Id);
        var unassignedDoc = await SeedDoctorAsync("Doctor", "Unassigned", headDepartmentDoctorId: null);

        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = head1.AccountId,
                Param = BuildParam(),
            }
        );

        Assert.True(result.IsSuccess());
        var items = result.Data!.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, result.Data.Metadata.TotalItemCount);

        var returnedIds = items.Select(x => x.Id).ToList();
        Assert.Contains(sub1.Id, returnedIds);
        Assert.Contains(sub2.Id, returnedIds);
        Assert.DoesNotContain(otherDoc.Id, returnedIds);
        Assert.DoesNotContain(unassignedDoc.Id, returnedIds);
        Assert.DoesNotContain(head1.Id, returnedIds);
        Assert.DoesNotContain(head2.Id, returnedIds);
    }

    [Fact]
    public async Task HandleAsync_WithGenderFilter_ReturnsOnlyMatchingSubordinates()
    {
        var head = await SeedHeadAsync("Head", "A");
        await SeedDoctorAsync("MaleDoc", "Sub", headDepartmentDoctorId: head.Id, gender: GenderType.Male);
        var femaleDoc = await SeedDoctorAsync("FemaleDoc", "Sub", headDepartmentDoctorId: head.Id, gender: GenderType.Female);

        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = head.AccountId,
                Param = BuildParam(),
                Filter = new SubordinateDoctorFilter { Gender = GenderType.Female },
            }
        );

        Assert.True(result.IsSuccess());
        var items = result.Data!.Items.ToList();
        var item = Assert.Single(items);
        Assert.Equal(femaleDoc.Id, item.Id);
        Assert.Equal(GenderType.Female, item.Gender);
    }

    [Fact]
    public async Task HandleAsync_WithPositionFilter_ReturnsOnlyMatchingSubordinates()
    {
        var head = await SeedHeadAsync("Head", "A");
        await SeedDoctorAsync("Staff", "Doc", headDepartmentDoctorId: head.Id, position: PositionType.StaffDoctor);
        var senior = await SeedDoctorAsync("Senior", "Doc", headDepartmentDoctorId: head.Id, position: PositionType.SeniorDoctor);

        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = head.AccountId,
                Param = BuildParam(),
                Filter = new SubordinateDoctorFilter { Position = PositionType.SeniorDoctor },
            }
        );

        Assert.True(result.IsSuccess());
        var items = result.Data!.Items.ToList();
        var item = Assert.Single(items);
        Assert.Equal(senior.Id, item.Id);
        Assert.Equal(PositionType.SeniorDoctor, item.Position);
    }

    [Fact]
    public async Task HandleAsync_ExcludesSoftDeletedSubordinates()
    {
        var head = await SeedHeadAsync("Head", "A");
        var active = await SeedDoctorAsync("Active", "Doc", headDepartmentDoctorId: head.Id, isDeleted: false);
        await SeedDoctorAsync("Deleted", "Doc", headDepartmentDoctorId: head.Id, isDeleted: true);

        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = head.AccountId,
                Param = BuildParam(),
            }
        );

        Assert.True(result.IsSuccess());
        var items = result.Data!.Items.ToList();
        var item = Assert.Single(items);
        Assert.Equal(active.Id, item.Id);
    }

    [Fact]
    public async Task HandleAsync_PassesProfilesToEnricher()
    {
        var head = await SeedHeadAsync("Head", "A");
        await SeedDoctorAsync("Doctor", "Sub", headDepartmentDoctorId: head.Id);

        var result = await HandleAsync(
            new GetSubordinateDoctorsQuery
            {
                HeadDoctorAccountId = head.AccountId,
                Param = BuildParam(),
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(1, _enricher.CallCount);
        var item = Assert.Single(result.Data!.Items.ToList());
        Assert.Equal($"enriched-{item.AccountId}", item.Email);
        Assert.Equal("Doctor", item.AccountRole);
    }

    private async Task<Result<Pagination<PagedDoctorItem>>> HandleAsync(GetSubordinateDoctorsQuery query)
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new GetSubordinateDoctorsHandler(context, new PaginationFactory(), _enricher);

        return await handler.HandleAsync(query, TestContext.Current.CancellationToken);
    }

    private static PaginationParam BuildParam() => new() { Page = 1, Size = 10 };

    private async Task<DoctorEntity> SeedDoctorAsync(
        string firstName,
        string lastName,
        Guid? headDepartmentDoctorId = null,
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
            headDepartmentDoctorId: headDepartmentDoctorId,
            createdAt: createdAt ?? DateTimeOffset.UtcNow,
            isDeleted: isDeleted
        );
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<HeadDepartmentDoctor> SeedHeadAsync(
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
        return head;
    }

    private sealed class StubEnricher : IDoctorProfileEnricher
    {
        public int CallCount { get; private set; }

        public Task EnrichAsync(
            IEnumerable<DoctorProfileItem> profiles,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            foreach (var item in profiles)
            {
                item.Email = $"enriched-{item.AccountId}";
                item.Phone = "0900000000";
                item.AccountRole = "Doctor";
                item.AccountStatus = "Active";
            }

            return Task.CompletedTask;
        }
    }
}
