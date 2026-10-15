using Respira.Patient.Application.Features.Patients.CreatePatient;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.Application.Test.Features.Patients.CreatePatient
{
    public class CreatePatientValidatorTest
    {
        private readonly CreatePatientValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task CreatePatient_RealisticCommand_Success()
        {
            // A complete admission record with values that only differ from the
            // rules by the intended amount (DOB in the past, 10 character card, ...)
            var result = await _validator.ValidateAsync(
                ValidCommand(), TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary of "Height must be greater than 0": 0.01 is the first positive
        // decimal step above the rule boundary, 156.8 is a typical adult height and
        // 215.5 is close to the tallest plausible patient
        [Theory]
        [InlineData(0.01)]
        [InlineData(156.8)]
        [InlineData(215.5)]
        public async Task CreatePatient_ValidHeight_Success(decimal height)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Height = height }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary of "Weight must be greater than 0": 0.01 is the first positive
        // decimal step above the rule boundary, 0.5 kg is a premature newborn (the
        // smallest plausible patient), 48.9 kg an average adult and 183.4 kg severe
        // obesity
        [Theory]
        [InlineData(0.01)]
        [InlineData(0.5)]
        [InlineData(48.9)]
        [InlineData(183.4)]
        public async Task CreatePatient_ValidWeight_Success(decimal weight)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Weight = weight }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: date of birth exactly equal to today still satisfies
        // "must not be in future"
        [Fact]
        public async Task CreatePatient_DateOfBirthToday_Success()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.Today) },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CreatePatient_DateOfBirthInPast_Success()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { DateOfBirth = new DateOnly(1949, 11, 2) },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Only the members defined by the Gender enum are admissible
        [Theory]
        [InlineData(Gender.Male)]
        [InlineData(Gender.Female)]
        public async Task CreatePatient_DefinedGender_Success(Gender gender)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Gender = gender }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Business rule: only the new health insurance card format (10 characters)
        // is accepted, the old 15 character card must not pass
        [Theory]
        [InlineData("8816372045")] // exactly 10 characters, the Length(10) boundary
        [InlineData("7016382459")]
        public async Task CreatePatient_InsuranceCardOfTenCharacters_Success(string card)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = card },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary of "Height must be greater than 0": 0 is the boundary itself
        // (rejected, the rule is exclusive) and -0.01 is the first decimal step below
        [Theory]
        [InlineData(0)]
        [InlineData(-0.01)]
        public async Task CreatePatient_InvalidHeight_Fail(decimal height)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Height = height }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Height", result.Errors[0].PropertyName);
        }

        // Boundary of "Weight must be greater than 0"
        [Theory]
        [InlineData(0)]
        [InlineData(-0.01)]
        public async Task CreatePatient_InvalidWeight_Fail(decimal weight)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Weight = weight }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Weight", result.Errors[0].PropertyName);
        }

        // Boundary: 2 is one step past the last member (Female) and -1 one step
        // below the first member (Male), both fall outside the enum
        [Theory]
        [InlineData((Gender)2)]
        [InlineData((Gender)(-1))]
        public async Task CreatePatient_UndefinedGender_Fail(Gender gender)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Gender = gender }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Gender", result.Errors[0].PropertyName);
        }

        // Boundary: 9 and 11 characters sit on either side of the Length(10) rule,
        // 15 characters is the old card format the rule comment documents as out of
        // scope. The values are non-empty, so only the length rule can reject them
        [Theory]
        [InlineData("881637204")] // 9 characters, just below the boundary
        [InlineData("88163720451")] // 11 characters, just above the boundary
        [InlineData("881637204512345")] // 15 characters, the old card format
        public async Task CreatePatient_InsuranceCardWrongLength_Fail(string card)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = card },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("HealthInsuranceCard", result.Errors[0].PropertyName);
        }

        // Boundary: the empty string is the lower boundary of any string rule
        [Fact]
        public async Task CreatePatient_EmptyInsuranceCard_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = "" },
                TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "HealthInsuranceCard");
        }

        [Fact]
        public async Task CreatePatient_WhitespaceInsuranceCard_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = "   " },
                TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "HealthInsuranceCard");
        }

        [Fact]
        public async Task CreatePatient_NonNumericInsuranceCard_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = "SG26045189" },
                TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "HealthInsuranceCard");
        }

        // Boundary: an empty string and whitespace-only are the two boundaries
        // NotEmpty rejects, one row per required string field so every rule is
        // proven in isolation
        [Theory]
        [InlineData(nameof(CreatePatientCommand.FirstName), "")]
        [InlineData(nameof(CreatePatientCommand.FirstName), "   ")]
        [InlineData(nameof(CreatePatientCommand.LastName), "")]
        [InlineData(nameof(CreatePatientCommand.LastName), "   ")]
        [InlineData(nameof(CreatePatientCommand.PhoneNumber), "")]
        [InlineData(nameof(CreatePatientCommand.PhoneNumber), "   ")]
        [InlineData(nameof(CreatePatientCommand.MedicalRecordCode), "")]
        [InlineData(nameof(CreatePatientCommand.MedicalRecordCode), "   ")]
        [InlineData(nameof(CreatePatientCommand.Address), "")]
        [InlineData(nameof(CreatePatientCommand.Address), "   ")]
        [InlineData(nameof(CreatePatientCommand.City), "")]
        [InlineData(nameof(CreatePatientCommand.City), "   ")]
        public async Task CreatePatient_EmptyRequiredField_Fail(string property, string value)
        {
            var command = ValidCommand();
            switch (property)
            {
                case nameof(CreatePatientCommand.FirstName):
                    command.FirstName = value;
                    break;
                case nameof(CreatePatientCommand.LastName):
                    command.LastName = value;
                    break;
                case nameof(CreatePatientCommand.PhoneNumber):
                    command.PhoneNumber = value;
                    break;
                case nameof(CreatePatientCommand.MedicalRecordCode):
                    command.MedicalRecordCode = value;
                    break;
                case nameof(CreatePatientCommand.Address):
                    command.Address = value;
                    break;
                case nameof(CreatePatientCommand.City):
                    command.City = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(property), property, "Not a required string field under test");
            }

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(property, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreatePatient_AllFieldsInvalid_Fail()
        {
            /*
             * Boundary: every rule that can fail at once fails independently, so the
             * client learns about all mistakes in a single response
             */
            var command = new CreatePatientCommand
            {
                FirstName = "",
                LastName = "   ",
                PhoneNumber = "",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Today).AddDays(1), // boundary: one day past the limit
                Gender = (Gender)2, // boundary: one step past the last member
                Height = 0, // boundary: the rule is exclusive, 0 itself is rejected
                Weight = -0.01m, // boundary: first decimal step below the rule boundary
                MedicalRecordCode = "",
                HealthInsuranceCard = "",
                Address = "",
                City = "",
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "FirstName");
            Assert.Contains(result.Errors, x => x.PropertyName == "LastName");
            Assert.Contains(result.Errors, x => x.PropertyName == "PhoneNumber");
            Assert.Contains(result.Errors, x => x.PropertyName == "DateOfBirth");
            Assert.Contains(result.Errors, x => x.PropertyName == "Gender");
            Assert.Contains(result.Errors, x => x.PropertyName == "Height");
            Assert.Contains(result.Errors, x => x.PropertyName == "Weight");
            Assert.Contains(result.Errors, x => x.PropertyName == "MedicalRecordCode");
            Assert.Contains(result.Errors, x => x.PropertyName == "HealthInsuranceCard");
            Assert.Contains(result.Errors, x => x.PropertyName == "Address");
            Assert.Contains(result.Errors, x => x.PropertyName == "City");
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
