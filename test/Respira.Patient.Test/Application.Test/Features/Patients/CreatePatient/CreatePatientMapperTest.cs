using Application.Contracts.Mappers;
using Application.Features.Patients.CreatePatient;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.Patients.CreatePatient;

public class CreatePatientMapperTest
{
    private readonly ICreateMapper<Patient, CreatePatientCommand> _mapper = new CreatePatientMapper();

    # region Happy path

    [Fact]
    public void ToModel_CopiesAllCommandFields_Success()
    {
        var command = new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            IsMale = true,
            DateOfBirth = new DateOnly(1985, 5, 12),
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0482037195",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        };

        var model = _mapper.ToModel(command);

        Assert.Equal(command.DateOfBirth, model.DateOfBirth);
        Assert.Equal(command.IsMale, model.IsMale);
        Assert.Equal(command.MedicalRecordCode, model.MedicalRecordCode);
        Assert.Equal(command.HealthInsuranceCardNumber, model.HealthInsuranceCardNumber);
        Assert.Equal(command.Address, model.Address);
        Assert.Equal(command.City, model.City);
        Assert.Equal(command.Country, model.Country);
    }

    [Fact]
    public void ToModel_NormalizesFullNameToTitleCase_Success()
    {
        // The mapper delegates to Patient.FullNameNormalize, which capitalizes the
        // first letter of every whitespace-separated word and trims surrounding
        // whitespace. A name with mixed case must end up in title case.
        var command = new CreatePatientCommand
        {
            FullName = "  ngUYEN    vAN   a  ",
            IsMale = true,
            DateOfBirth = new DateOnly(1985, 5, 12),
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0482037195",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        };

        var model = _mapper.ToModel(command);

        Assert.Equal("Nguyen Van A", model.FullName);
    }

    [Fact]
    public void ToModel_SetsDefaults_Success()
    {
        // A freshly mapped patient must carry the same default values the handler
        // relies on: Admission = now, Discharge = null, Status = InTreatment,
        // and a non-empty generated Id.
        var before = DateTimeOffset.UtcNow;
        var command = new CreatePatientCommand
        {
            FullName = "Nguyen Van A",
            IsMale = true,
            DateOfBirth = new DateOnly(1985, 5, 12),
            MedicalRecordCode = "MRN-2025-0001",
            HealthInsuranceCardNumber = "0482037195",
            Address = "12 Le Loi",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        };

        var model = _mapper.ToModel(command);
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Null(model.Discharge);
        Assert.Equal(PatientStatus.InTreatment, model.Status);
        // Admission should be set to a timestamp between before and after
        Assert.InRange(model.Admission, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void ToModel_FemalePatient_KeepsIsMaleFalse()
    {
        // The mapper is a straight copy of IsMale, so a female patient
        // must come out the other side with IsMale = false
        var command = new CreatePatientCommand
        {
            FullName = "Tran Thi B",
            IsMale = false,
            DateOfBirth = new DateOnly(1990, 11, 25),
            MedicalRecordCode = "MRN-2025-0002",
            HealthInsuranceCardNumber = "0482037196",
            Address = "23 Tran Hung Dao",
            City = "Ha Noi",
            Country = "Viet Nam",
        };

        var model = _mapper.ToModel(command);

        Assert.False(model.IsMale);
    }

    [Fact]
    public void ToModel_NewbornPatient_KeepsTodayDateOfBirth()
    {
        // Boundary: a baby born today is a valid input; the mapper should
        // preserve today's date without modification
        var today = DateOnly.FromDateTime(DateTime.Today);
        var command = new CreatePatientCommand
        {
            FullName = "Newborn Baby",
            IsMale = true,
            DateOfBirth = today,
            MedicalRecordCode = "MRN-2025-9999",
            HealthInsuranceCardNumber = "0123456789",
            Address = "Hospital A",
            City = "Ho Chi Minh",
            Country = "Viet Nam",
        };

        var model = _mapper.ToModel(command);

        Assert.Equal(today, model.DateOfBirth);
    }

    # endregion
}
