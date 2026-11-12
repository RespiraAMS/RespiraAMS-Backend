using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Results;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetById;

public sealed class GetDoctorByIdQueryHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;

    public GetDoctorByIdQueryHandlerTest(PostgresFixture fixture)
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

        var result = await HandleAsync(doctor.Id);

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

        var result = await HandleAsync(head.Id);

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(head.Id, data.Id);
        Assert.Equal(accountId, data.AccountId);
        Assert.Equal(ProfileRole.HeadDepartmentDoctor, data.Role);
        Assert.Equal("Tran", data.FirstName);
        Assert.Equal("Thi B", data.LastName);
        Assert.Equal(AcademicTitle.Professor, data.AcademicTitle);
        Assert.Equal(PositionType.HeadOfDepartment, data.Position);
        Assert.Equal(GenderType.Female, data.Gender);
    }

    [Fact]
    public async Task HandleAsync_WithDoctor_MapsDegrees()
    {
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011111",
                createdAt: new DateTimeOffset(2024, 1, 5, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal([DegreeType.Doctor, DegreeType.Master], data.Degrees);
        Assert.Equal(2, data.Degrees.Count);
    }

    [Fact]
    public async Task HandleAsync_WithDoctorWithoutMedia_ReturnsNullMediaId()
    {
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011112",
                mediaId: null,
                createdAt: new DateTimeOffset(2024, 1, 6, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Null(data.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithDoctorWithMedia_ReturnsMediaId()
    {
        var mediaId = Guid.CreateVersion7();
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011113",
                mediaId: mediaId,
                createdAt: new DateTimeOffset(2024, 1, 7, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(mediaId, data.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithDoctorWithoutDateOfBirth_ReturnsNullDateOfBirth()
    {
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011114",
                dateOfBirth: null,
                createdAt: new DateTimeOffset(2024, 1, 8, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Null(data.DateOfBirth);
    }

    [Fact]
    public async Task HandleAsync_WithDoctorWithDateOfBirth_ReturnsDateOfBirth()
    {
        var dateOfBirth = new DateTimeOffset(1988, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011115",
                dateOfBirth: dateOfBirth,
                createdAt: new DateTimeOffset(2024, 1, 9, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(dateOfBirth, data.DateOfBirth);
    }

    [Fact]
    public async Task HandleAsync_WithHeadDepartmentDoctorId_ResolvesTheHeadAndNotTheDoctor()
    {
        var head = await SeedHeadDepartmentDoctorAsync(
            TestData.DoctorFactory.CreateHeadDepartmentDoctor(
                firstName: "Tran",
                lastName: "Thi B",
                citizenIdentificationNumber: "001088054322",
                createdAt: new DateTimeOffset(2024, 2, 1, 8, 0, 0, TimeSpan.Zero)
            )
        );
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                firstName: "Nguyen",
                lastName: "Van A",
                citizenIdentificationNumber: "001088011116",
                createdAt: new DateTimeOffset(2024, 2, 2, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(head.Id);

        Assert.True(result.IsSuccess());
        var data = Assert.IsType<DoctorResult>(result.Data);
        Assert.Equal(head.Id, data.Id);
        Assert.NotEqual(doctor.Id, data.Id);
        Assert.Equal(ProfileRole.HeadDepartmentDoctor, data.Role);
        Assert.Equal("Tran", data.FirstName);
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ReturnsDataAndNoError()
    {
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011117",
                createdAt: new DateTimeOffset(2024, 5, 5, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.False(result.IsFailure());
    }

    #endregion

    #region Fail path

    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsResourceNotFoundFailure()
    {
        var result = await HandleAsync(Guid.CreateVersion7());

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.Error!.StatusCode);
        Assert.Equal("Doctor not found", result.Error.Description);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_ReturnsResourceNotFoundFailure()
    {
        var result = await HandleAsync(Guid.Empty);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedDoctor_ReturnsResourceNotFoundFailure()
    {
        var doctor = await SeedDoctorAsync(
            TestData.DoctorFactory.CreateDoctor(
                citizenIdentificationNumber: "001088011118",
                isDeleted: true,
                createdAt: new DateTimeOffset(2024, 6, 6, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(doctor.Id);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedHeadDepartmentDoctor_ReturnsResourceNotFoundFailure()
    {
        var head = await SeedHeadDepartmentDoctorAsync(
            TestData.DoctorFactory.CreateHeadDepartmentDoctor(
                citizenIdentificationNumber: "001088054323",
                isDeleted: true,
                createdAt: new DateTimeOffset(2024, 6, 7, 8, 0, 0, TimeSpan.Zero)
            )
        );

        var result = await HandleAsync(head.Id);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Null(result.Data);
    }

    #endregion

    private async Task<Result<DoctorResult>> HandleAsync(Guid doctorId)
    {
        await using var context = new DoctorDbContext(_options);
        var handler = new GetDoctorByIdQueryHandler(
            context,
            Mock.Of<ILogger<GetDoctorByIdQueryHandler>>()
        );

        return await handler.HandleAsync(
            new GetDoctorByIdQuery { DoctorId = doctorId },
            TestContext.Current.CancellationToken
        );
    }

    private async Task<DoctorEntity> SeedDoctorAsync(DoctorEntity doctor)
    {
        await using var context = new DoctorDbContext(_options);
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<Respira.Doctor.Domain.Entities.HeadDepartmentDoctor> SeedHeadDepartmentDoctorAsync(
        Respira.Doctor.Domain.Entities.HeadDepartmentDoctor head
    )
    {
        await using var context = new DoctorDbContext(_options);
        context.HeadDepartmentDoctors.Add(head);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return head;
    }
}
