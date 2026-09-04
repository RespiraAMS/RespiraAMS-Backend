using Application.Contracts.Data;
using Application.Features.Patients.CreatePatient;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Patients.CreatePatient;

public class CreatePatientHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreatePatientHandler _handler;
    private readonly IDbContext _context;

    public CreatePatientHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreatePatientMapper();

        // Initialize handler
        _handler = new(_context, mapper);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Treatments reference patients through an FK, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.Treatments.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Patients.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Theory]
    // Adult male patient
    [InlineData("Nguyen Van A", 1985, 5, 12, true, "MRN-2025-0001", "0482037195", "12 Le Loi", "Ho Chi Minh", "Viet Nam")]
    // Newborn female patient (boundary: born today)
    [InlineData("Tran Thi B", 2026, 9, 4, false, "MRN-2025-0002", "0482037196", "23 Tran Hung Dao", "Ha Noi", "Viet Nam")]
    // Elderly patient
    [InlineData("Le Van C", 1932, 1, 1, true, "MRN-2025-0003", "0482037197", "8 Nguyen Hue", "Da Nang", "Viet Nam")]
    // Patient with multi-word name containing diacritics
    [InlineData("Pham Thi My Duyen", 1990, 11, 25, false, "MRN-2025-0004", "0482037198", "45 Phan Chu Trinh", "Can Tho", "Viet Nam")]
    public async Task CreatePatient_Success(
        string fullName, int year, int month, int day,
        bool isMale, string medicalRecordCode, string healthInsuranceCardNumber,
        string address, string city, string country)
    {
        var result = await _handler.HandleAsync(new CreatePatientCommand
        {
            FullName = fullName,
            DateOfBirth = new DateOnly(year, month, day),
            IsMale = isMale,
            MedicalRecordCode = medicalRecordCode,
            HealthInsuranceCardNumber = healthInsuranceCardNumber,
            Address = address,
            City = city,
            Country = country,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Created, result.StatusCode);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Patients
            .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);

        // FullName must be normalized to title case as documented
        Assert.Equal(Patient.FullNameNormalize(fullName), saved.FullName);
        Assert.Equal(new DateOnly(year, month, day), saved.DateOfBirth);
        Assert.Equal(isMale, saved.IsMale);
        Assert.Equal(medicalRecordCode, saved.MedicalRecordCode);
        Assert.Equal(healthInsuranceCardNumber, saved.HealthInsuranceCardNumber);
        Assert.Equal(address, saved.Address);
        Assert.Equal(city, saved.City);
        Assert.Equal(country, saved.Country);

        // A newly created patient must be InTreatment, with Discharge set to null,
        // and Admission set to right now
        Assert.Equal(PatientStatus.InTreatment, saved.Status);
        Assert.Null(saved.Discharge);
        Assert.True((DateTimeOffset.UtcNow - saved.Admission).Duration() < TimeSpan.FromMinutes(1));
    }

    # endregion
}
