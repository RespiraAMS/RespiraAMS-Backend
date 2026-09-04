using Application.Features.Patients.CreatePatient;

namespace Application.Test.Features.Patients.CreatePatient;

public class CreatePatientValidatorTest
{
    private readonly CreatePatientValidator _validator = new();

    # region Valid command

    [Theory]
    // Standard adult patient
    [InlineData("Nguyen Van A", 1985, 5, 12, true, "MRN-2025-0001", "0482037195", "12 Le Loi", "Ho Chi Minh", "Viet Nam")]
    // Newborn patient (boundary: born today is the most recent valid date of birth)
    [InlineData("Tran Thi B", 2026, 9, 4, false, "MRN-2025-0002", "0482037196", "23 Tran Hung Dao", "Ha Noi", "Viet Nam")]
    // Elderly patient (boundary: very old date of birth is still valid as long as not in the future)
    [InlineData("Le Van C", 1932, 1, 1, true, "MRN-2025-0003", "0482037197", "8 Nguyen Hue", "Da Nang", "Viet Nam")]
    // Multi-word name with diacritics
    [InlineData("Pham Thi My Duyen", 1990, 11, 25, false, "MRN-2025-0004", "0482037198", "45 Phan Chu Trinh", "Can Tho", "Viet Nam")]
    public async Task CreatePatient_Success(
        string fullName, int year, int month, int day,
        bool isMale, string medicalRecordCode, string healthInsuranceCardNumber,
        string address, string city, string country)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
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

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CreatePatient_TodayAsDateOfBirth_Success()
    {
        // Boundary: the validator accepts today as the most recent date of birth
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Newborn Baby",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-9999",
            HealthInsuranceCardNumber = "0123456789",
            Address = "Hospital A",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    /*
     * Each row in this Theory targets exactly one property. Unrelated fields are
     * kept valid so the only errors come from the targeted field. The exception
     * is HealthInsuranceCardNumber below, which uses .NotEmpty().Length(10): an
     * empty value triggers BOTH rules, so a separate Theory covers those cases.
     */

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_EmptyFullName_Fail(string fullName)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = fullName,
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0123456789",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("FullName", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_EmptyMedicalRecordCode_Fail(string medicalRecordCode)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = medicalRecordCode,
            HealthInsuranceCardNumber = "0123456789",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("MedicalRecordCode", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_EmptyAddress_Fail(string address)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0123456789",
            Address = address,
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Address", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_EmptyCity_Fail(string city)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0123456789",
            Address = "12 Le Loi",
            City = city,
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("City", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_EmptyCountry_Fail(string country)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0123456789",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = country,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Country", result.Errors[0].PropertyName);
    }

    [Theory]
    // Length boundaries for the new health insurance card format (10 chars).
    // 9 chars: too short (below boundary)
    // 11 chars: too long (above boundary)
    // 15 chars: the old format, also rejected
    [InlineData("012345678")]
    [InlineData("01234567890")]
    [InlineData("012345678901234")]
    public async Task CreatePatient_HealthInsuranceCardNumberWrongLength_Fail(string healthInsuranceCardNumber)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = healthInsuranceCardNumber,
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        // Length 10 is exact, so a non-empty value that is not exactly 10 chars
        // produces exactly one error: the Length(10) rule
        _ = Assert.Single(result.Errors);
        Assert.Equal("HealthInsuranceCardNumber", result.Errors[0].PropertyName);
    }

    [Theory]
    // The .NotEmpty().Length(10) chain on HealthInsuranceCardNumber means an
    // empty value fails both rules, so the validator produces two errors.
    // Both must point at the same property.
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePatient_EmptyHealthInsuranceCardNumber_Fail(string healthInsuranceCardNumber)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1985, 5, 12),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = healthInsuranceCardNumber,
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Equal("HealthInsuranceCardNumber", e.PropertyName));
    }

    [Theory]
    // DateOfBirth in the future (boundary: tomorrow is the first invalid day)
    [InlineData(2026, 9, 5)] // tomorrow
    [InlineData(2030, 1, 1)]
    [InlineData(2027, 12, 31)]
    public async Task CreatePatient_FutureDateOfBirth_Fail(int year, int month, int day)
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "Future Baby",
            DateOfBirth = new DateOnly(year, month, day),
            IsMale = true,
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0123456789",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("DateOfBirth", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreatePatient_AllFieldsInvalid_Fail()
    {
        var result = await _validator.ValidateAsync(new CreatePatientCommand
        {
            FullName = "",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today).AddDays(1),
            IsMale = true,
            MedicalRecordCode = "",
            HealthInsuranceCardNumber = "123",
            Address = "",
            City = "",
            Country = "",
        }, TestContext.Current.CancellationToken);

        // 1 error per invalid field: FullName, DateOfBirth, MedicalRecordCode,
        // HealthInsuranceCardNumber (Length 10 only, since "123" passes NotEmpty),
        // Address, City, Country = 7 errors
        Assert.Equal(7, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "FullName");
        Assert.Contains(result.Errors, x => x.PropertyName == "DateOfBirth");
        Assert.Contains(result.Errors, x => x.PropertyName == "MedicalRecordCode");
        Assert.Contains(result.Errors, x => x.PropertyName == "HealthInsuranceCardNumber");
        Assert.Contains(result.Errors, x => x.PropertyName == "Address");
        Assert.Contains(result.Errors, x => x.PropertyName == "City");
        Assert.Contains(result.Errors, x => x.PropertyName == "Country");
    }

    # endregion
}
