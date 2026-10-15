using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Application.Features.Patients.CreatePatient;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.CreatePatient
{
    public class CreatePatientMapperTest
    {
        /*
         * The mapper has no failure branch: it only copies the command onto a fresh
         * entity, so only the happy path exists for it. Rejection of invalid input is
         * the validator's responsibility
         */
        private readonly ICreateMapper<CreatePatientCommand, Domain.Entities.Patient> _mapper =
            new CreatePatientMapper();

        #region Happy path

        [Fact]
        public void ToModel_FullCommand_MapsEveryFieldAndAppliesDefaults()
        {
            var command = ValidCommand();
            var before = DateTimeOffset.UtcNow;

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;

            Assert.Equal(command.FirstName, model.FirstName);
            Assert.Equal(command.LastName, model.LastName);
            Assert.Equal(command.PhoneNumber, model.PhoneNumber);
            Assert.Equal(command.DateOfBirth, model.DateOfBirth);
            Assert.Equal(command.Gender, model.Gender);
            Assert.Equal(command.Height, model.Height);
            Assert.Equal(command.Weight, model.Weight);
            Assert.Equal(command.MedicalRecordCode, model.MedicalRecordCode);
            Assert.Equal(command.HealthInsuranceCard, model.HealthInsuranceCard);
            Assert.Equal(command.Address, model.Address);
            Assert.Equal(command.City, model.City);

            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.False(model.IsDeleted);

            // Defaults owned by the mapper: admission stamped at mapping time, a new
            // patient is not discharged and starts in treatment
            Assert.InRange(model.Admission, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));
            Assert.Null(model.Discharge);
            Assert.Equal(TreatmentOutcome.InTreatment, model.TreatmentOutcome);

            // Documented entity rules: year-only age and Vietnamese style full name
            Assert.Equal(DateTimeOffset.Now.Year - command.DateOfBirth.Year, model.Age());
            Assert.Equal("Nguyễn Thị Mỹ Linh", model.FullName);
        }

        // Boundary values of plausible human measurements: premature newborn to very
        // tall / severely obese adult. The mapper must carry them over untouched
        [Theory]
        [InlineData(46.5, 2.4)]
        [InlineData(156.8, 48.9)]
        [InlineData(215.5, 183.4)]
        public void ToModel_BodyMeasurements_KeepsExactValue(double height, double weight)
        {
            var result = _mapper.ToModel(ValidCommand() with
            {
                Height = (decimal)height,
                Weight = (decimal)weight,
            });

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal((decimal)height, result.Data.Height);
            Assert.Equal((decimal)weight, result.Data.Weight);
        }

        // The gender column is stored as a string in the database, the enum value
        // itself must survive the mapping
        [Theory]
        [InlineData(Gender.Male)]
        [InlineData(Gender.Female)]
        public void ToModel_Gender_PreservesEnumValue(Gender gender)
        {
            var result = _mapper.ToModel(ValidCommand() with { Gender = gender });

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(gender, result.Data.Gender);
        }

        [Fact]
        public void ToModel_MessyNames_NormalizedToVietnameseStyle()
        {
            /*
             * Business rule (Domain.Entities.Patient): both name setters normalize on
             * assignment - every word is capitalized and inner whitespace collapsed -
             * and the full name renders as "LastName FirstName" (Vietnamese style)
             */
            var command = ValidCommand() with
            {
                FirstName = "  mY   lINh ",
                LastName = " nguyễn   THỊ ",
            };

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal("My Linh", result.Data.FirstName);
            Assert.Equal("Nguyễn Thị", result.Data.LastName);
            Assert.Equal("Nguyễn Thị My Linh", result.Data.FullName);
        }

        #endregion

        private static CreatePatientCommand ValidCommand() => new()
        {
            FirstName = "Mỹ Linh",
            LastName = "Nguyễn Thị",
            PhoneNumber = "0984372615",
            DateOfBirth = new DateOnly(1996, 7, 22),
            Gender = Gender.Female,
            Height = 156.8m,
            Weight = 48.9m,
            MedicalRecordCode = "BN-2026-004817",
            HealthInsuranceCard = "8816372045",
            Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
            City = "Hà Nội",
        };
    }
}
