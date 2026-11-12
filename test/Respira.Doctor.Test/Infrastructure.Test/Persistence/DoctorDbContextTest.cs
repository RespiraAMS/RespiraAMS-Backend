using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respira.Doctor.Domain.Enums;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Utils.FullTextSearch;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Infrastructure.Test.Persistence;

public sealed class DoctorDbContextTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<DoctorDbContext> _options;
    private readonly string _connectionString;
    private readonly DoctorDbContext _context;

    public DoctorDbContextTest(PostgresFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
        _options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new DoctorDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await _context
            .Doctors.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .HeadDepartmentDoctors.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    #region Global query filters

    [Fact]
    public async Task SoftDeletedDoctor_IsHiddenFromPlainQueries()
    {
        await SeedDoctorAsync(isDeleted: true);

        await using var context = new DoctorDbContext(_options);
        var doctors = await context.Doctors.ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(doctors);
    }

    [Fact]
    public async Task SoftDeletedHeadDepartmentDoctor_IsHiddenFromPlainQueries()
    {
        await SeedHeadDepartmentDoctorAsync(isDeleted: true);

        await using var context = new DoctorDbContext(_options);
        var heads = await context
            .HeadDepartmentDoctors
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(heads);
    }

    [Fact]
    public async Task IgnoreQueryFilters_BringsTheSoftDeletedRowsBack()
    {
        await SeedDoctorAsync(isDeleted: true);
        await SeedHeadDepartmentDoctorAsync(isDeleted: true);

        await using var context = new DoctorDbContext(_options);
        var doctors = await context
            .Doctors.IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);
        var heads = await context
            .HeadDepartmentDoctors.IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(doctors);
        Assert.True(doctors[0].IsDeleted);
        Assert.NotNull(doctors[0].DeletedAt);
        Assert.Single(heads);
        Assert.True(heads[0].IsDeleted);
        Assert.NotNull(heads[0].DeletedAt);
    }

    #endregion

    #region DateTimeOffset normalisation

    [Fact]
    public async Task SaveChangesAsync_NormalizesCreatedAtToUtc()
    {
        var createdAt = new DateTimeOffset(2024, 5, 4, 8, 30, 0, TimeSpan.FromHours(7));

        var doctor = await SeedDoctorAsync(createdAt: createdAt, updatedAt: createdAt);

        // The context rewrites the value before it ever reaches the database.
        Assert.Equal(TimeSpan.Zero, doctor.CreatedAt.Offset);
        Assert.Equal(createdAt.UtcDateTime, doctor.CreatedAt.UtcDateTime);

        await using var context = new DoctorDbContext(_options);
        var persisted = await context.Doctors.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.Zero, persisted.CreatedAt.Offset);
        Assert.Equal(createdAt.UtcDateTime, persisted.CreatedAt.UtcDateTime);
    }

    [Fact]
    public async Task SaveChangesAsync_NormalizesUpdatedAtToUtc()
    {
        var updatedAt = new DateTimeOffset(2023, 11, 30, 23, 15, 0, TimeSpan.FromHours(-5));

        var doctor = await SeedDoctorAsync(updatedAt: updatedAt);

        Assert.Equal(TimeSpan.Zero, doctor.UpdatedAt.Offset);
        Assert.Equal(updatedAt.UtcDateTime, doctor.UpdatedAt.UtcDateTime);

        await using var context = new DoctorDbContext(_options);
        var persisted = await context.Doctors.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.Zero, persisted.UpdatedAt.Offset);
        Assert.Equal(updatedAt.UtcDateTime, persisted.UpdatedAt.UtcDateTime);
    }

    [Fact]
    public async Task SaveChangesAsync_NormalizesDateOfBirthToUtc()
    {
        var dateOfBirth = new DateTimeOffset(1988, 2, 29, 0, 0, 0, TimeSpan.FromHours(7));

        var doctor = await SeedDoctorAsync(dateOfBirth: dateOfBirth);

        Assert.NotNull(doctor.DateOfBirth);
        Assert.Equal(TimeSpan.Zero, doctor.DateOfBirth.Value.Offset);
        Assert.Equal(dateOfBirth.UtcDateTime, doctor.DateOfBirth.Value.UtcDateTime);

        await using var context = new DoctorDbContext(_options);
        var persisted = await context.Doctors.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.DateOfBirth);
        Assert.Equal(TimeSpan.Zero, persisted.DateOfBirth.Value.Offset);
        Assert.Equal(dateOfBirth.UtcDateTime, persisted.DateOfBirth.Value.UtcDateTime);
    }

    #endregion

    #region Full text search vector

    [Fact]
    public async Task SearchVector_IsPopulatedByTheDatabase()
    {
        var doctor = await SeedDoctorAsync(firstName: "Nguyen", lastName: "Phuong");

        var vector = await ReadSearchVectorAsync(doctor.Id);

        Assert.False(string.IsNullOrWhiteSpace(vector));
        Assert.Contains("nguyen", vector, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchVector_ContainsTheLastNameAndTheCitizenIdentificationNumber()
    {
        var doctor = await SeedDoctorAsync(
            firstName: "Nguyen",
            lastName: "Phuong",
            citizenIdentificationNumber: "001088012345"
        );

        var vector = await ReadSearchVectorAsync(doctor.Id);

        Assert.Contains("phuong", vector, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("001088012345", vector);
    }

    [Fact]
    public async Task SearchVector_IsAccentInsensitive()
    {
        var doctor = await SeedDoctorAsync(firstName: "Nguyễn", lastName: "Phuong");

        var tsQuery = FullTextSearchUtil.BuildPrefixQuery("nguyen");
        Assert.Equal("nguyen:*", tsQuery);

        var matches = await SearchDoctorIdsAsync(tsQuery);

        Assert.Contains(doctor.Id, matches);
    }

    #endregion

    #region Constraints

    [Fact]
    public async Task SavingTwoDoctorsWithTheSameAccountId_ThrowsADbUpdateException()
    {
        var accountId = Guid.CreateVersion7();
        await SeedDoctorAsync(accountId: accountId);

        await using var context = new DoctorDbContext(_options);
        context.Doctors.Add(CreateDoctor(accountId: accountId));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken)
        );

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task SavingTwoHeadDepartmentDoctorsWithTheSameAccountId_ThrowsADbUpdateException()
    {
        var accountId = Guid.CreateVersion7();
        await SeedHeadDepartmentDoctorAsync(accountId: accountId);

        await using var context = new DoctorDbContext(_options);
        context.HeadDepartmentDoctors.Add(CreateHeadDepartmentDoctor(accountId: accountId));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken)
        );

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task DeletingAHeadDepartmentDoctorThatStillHasSubordinates_IsRejectedByTheForeignKey()
    {
        var head = await SeedHeadDepartmentDoctorAsync();
        await SeedDoctorAsync(headDepartmentDoctorId: head.Id);

        await using var context = new DoctorDbContext(_options);
        var trackedHead = await context.HeadDepartmentDoctors.SingleAsync(TestContext.Current.CancellationToken);
        context.HeadDepartmentDoctors.Remove(trackedHead);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken)
        );

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
    }

    #endregion

    #region Relationships and collections

    [Fact]
    public async Task Doctor_CanReferenceItsHeadDepartmentDoctor()
    {
        var head = await SeedHeadDepartmentDoctorAsync();
        var doctor = await SeedDoctorAsync(headDepartmentDoctorId: head.Id);

        await using var context = new DoctorDbContext(_options);
        var persisted = await context
            .Doctors.AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        var persistedHead = await context
            .HeadDepartmentDoctors.Include(x => x.Subordinates)
            .AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(head.Id, persisted.HeadDepartmentDoctorId);
        Assert.Equal(doctor.Id, persisted.Id);
        // The other side of the relationship sees the doctor as a subordinate.
        Assert.Contains(persistedHead.Subordinates, x => x.Id == doctor.Id);
    }

    [Fact]
    public async Task Degrees_RoundTripThroughTheIntegerArrayColumn()
    {
        await SeedDoctorAsync(degrees: [DegreeType.Doctor, DegreeType.Master, DegreeType.PhD]);

        await using var context = new DoctorDbContext(_options);
        var persisted = await context.Doctors.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.Degrees);
        Assert.Equal(3, persisted.Degrees.Count);
        Assert.Contains(DegreeType.Doctor, persisted.Degrees);
        Assert.Contains(DegreeType.Master, persisted.Degrees);
        Assert.Contains(DegreeType.PhD, persisted.Degrees);
    }

    [Fact]
    public async Task Patients_RoundTripThroughTheUuidArrayColumn()
    {
        var patientIds = new[] { Guid.CreateVersion7(), Guid.CreateVersion7() };
        await SeedDoctorAsync(patients: patientIds);

        await using var context = new DoctorDbContext(_options);
        var persisted = await context.Doctors.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.Patients);
        Assert.Equal(2, persisted.Patients.Count);
        Assert.Contains(patientIds[0], persisted.Patients);
        Assert.Contains(patientIds[1], persisted.Patients);
    }

    #endregion

    #region Persistence

    [Fact]
    public async Task SavingADoctor_PersistsEveryMappedField()
    {
        var mediaId = Guid.CreateVersion7();
        await SeedDoctorAsync(
            firstName: "Nguyen",
            lastName: "Phuong",
            degrees: [DegreeType.Doctor, DegreeType.Master],
            academicTitle: AcademicTitle.AssociateProfessor,
            position: PositionType.SeniorDoctor,
            gender: GenderType.Female,
            citizenIdentificationNumber: "001099887766",
            address: "123 Nguyen Trai, District 5, Ho Chi Minh City",
            mediaId: mediaId
        );

        await using var context = new DoctorDbContext(_options);
        var persisted = await context.Doctors.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Nguyen", persisted.FirstName);
        Assert.Equal("Phuong", persisted.LastName);
        Assert.Equal(2, persisted.Degrees.Count);
        Assert.Contains(DegreeType.Doctor, persisted.Degrees);
        Assert.Contains(DegreeType.Master, persisted.Degrees);
        Assert.Equal(AcademicTitle.AssociateProfessor, persisted.AcademicTitle);
        Assert.Equal(PositionType.SeniorDoctor, persisted.Position);
        Assert.Equal(GenderType.Female, persisted.Gender);
        Assert.Equal("001099887766", persisted.CitizenIdentificationNumber);
        Assert.Equal("123 Nguyen Trai, District 5, Ho Chi Minh City", persisted.Address);
        Assert.Equal(mediaId, persisted.MediaId);
    }

    #endregion

    #region Helpers

    private static DoctorEntity CreateDoctor(
        Guid? accountId = null,
        string firstName = "Nguyen",
        string lastName = "Phuong",
        string? citizenIdentificationNumber = null,
        string address = "12 Le Loi, District 1, Ho Chi Minh City",
        Guid? headDepartmentDoctorId = null,
        Guid? mediaId = null,
        GenderType gender = GenderType.Male,
        AcademicTitle academicTitle = AcademicTitle.None,
        PositionType position = PositionType.StaffDoctor,
        DateTimeOffset? dateOfBirth = null,
        ICollection<DegreeType>? degrees = null,
        ICollection<Guid>? patients = null
    ) =>
        new()
        {
            AccountId = accountId ?? Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Degrees = degrees ?? [DegreeType.Doctor, DegreeType.Master],
            AcademicTitle = academicTitle,
            Position = position,
            Gender = gender,
            CitizenIdentificationNumber = citizenIdentificationNumber ?? UniqueCitizenIdentificationNumber("9"),
            DateOfBirth = dateOfBirth,
            Address = address,
            HeadDepartmentDoctorId = headDepartmentDoctorId,
            MediaId = mediaId,
            Patients = patients,
        };

    private static HeadDepartmentDoctorEntity CreateHeadDepartmentDoctor(
        Guid? accountId = null,
        string firstName = "Tran",
        string lastName = "Thi B",
        string? citizenIdentificationNumber = null,
        Guid? mediaId = null,
        GenderType gender = GenderType.Female,
        AcademicTitle academicTitle = AcademicTitle.Professor,
        PositionType position = PositionType.HeadOfDepartment,
        DateTimeOffset? dateOfBirth = null,
        ICollection<DegreeType>? degrees = null,
        ICollection<Guid>? patients = null
    ) =>
        new()
        {
            AccountId = accountId ?? Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Degrees = degrees ?? [DegreeType.Doctor, DegreeType.PhD],
            AcademicTitle = academicTitle,
            Position = position,
            Gender = gender,
            CitizenIdentificationNumber = citizenIdentificationNumber ?? UniqueCitizenIdentificationNumber("8"),
            DateOfBirth = dateOfBirth,
            Address = "45 Le Loi, District 1, Ho Chi Minh City",
            MediaId = mediaId,
            Patients = patients,
        };

    private static string UniqueCitizenIdentificationNumber(string prefix) =>
        $"{prefix}{Guid.CreateVersion7():N}"[..12];

    private async Task<DoctorEntity> SeedDoctorAsync(
        Guid? accountId = null,
        string firstName = "Nguyen",
        string lastName = "Phuong",
        string? citizenIdentificationNumber = null,
        string address = "12 Le Loi, District 1, Ho Chi Minh City",
        Guid? headDepartmentDoctorId = null,
        Guid? mediaId = null,
        GenderType gender = GenderType.Male,
        AcademicTitle academicTitle = AcademicTitle.None,
        PositionType position = PositionType.StaffDoctor,
        DateTimeOffset? dateOfBirth = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        ICollection<DegreeType>? degrees = null,
        ICollection<Guid>? patients = null,
        bool isDeleted = false
    )
    {
        var doctor = CreateDoctor(
            accountId,
            firstName,
            lastName,
            citizenIdentificationNumber,
            address,
            headDepartmentDoctorId,
            mediaId,
            gender,
            academicTitle,
            position,
            dateOfBirth,
            degrees,
            patients
        );

        if (createdAt.HasValue)
        {
            doctor.CreatedAt = createdAt.Value;
            doctor.UpdatedAt = updatedAt ?? createdAt.Value;
        }
        else if (updatedAt.HasValue)
        {
            doctor.UpdatedAt = updatedAt.Value;
        }

        if (isDeleted)
        {
            doctor.IsDeleted = true;
            doctor.DeletedAt = createdAt ?? DateTimeOffset.UtcNow;
        }

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return doctor;
    }

    private async Task<HeadDepartmentDoctorEntity> SeedHeadDepartmentDoctorAsync(
        Guid? accountId = null,
        string firstName = "Tran",
        string lastName = "Thi B",
        string? citizenIdentificationNumber = null,
        Guid? mediaId = null,
        GenderType gender = GenderType.Female,
        AcademicTitle academicTitle = AcademicTitle.Professor,
        PositionType position = PositionType.HeadOfDepartment,
        DateTimeOffset? dateOfBirth = null,
        ICollection<DegreeType>? degrees = null,
        ICollection<Guid>? patients = null,
        bool isDeleted = false
    )
    {
        var head = CreateHeadDepartmentDoctor(
            accountId,
            firstName,
            lastName,
            citizenIdentificationNumber,
            mediaId,
            gender,
            academicTitle,
            position,
            dateOfBirth,
            degrees,
            patients
        );

        if (isDeleted)
        {
            head.IsDeleted = true;
            head.DeletedAt = DateTimeOffset.UtcNow;
        }

        _context.HeadDepartmentDoctors.Add(head);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return head;
    }

    private async Task<string> ReadSearchVectorAsync(Guid doctorId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(
            """SELECT "SearchVector"::text FROM "Doctors" WHERE "Id" = @id""",
            connection
        );
        command.Parameters.AddWithValue("id", doctorId);

        var value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        return value?.ToString() ?? string.Empty;
    }

    private async Task<List<Guid>> SearchDoctorIdsAsync(string tsQuery)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT "Id"
            FROM "Doctors"
            WHERE "SearchVector" @@ to_tsquery('respira_vi', @query)
            """,
            connection
        );
        command.Parameters.AddWithValue("query", tsQuery);

        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    #endregion
}
