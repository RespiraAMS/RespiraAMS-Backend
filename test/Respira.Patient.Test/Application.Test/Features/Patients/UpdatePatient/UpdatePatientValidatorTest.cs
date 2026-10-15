using Respira.Patient.Application.Features.Patients.UpdatePatient;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.Application.Test.Features.Patients.UpdatePatient
{
    public class UpdatePatientValidatorTest
    {
        private readonly UpdatePatientValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task UpdatePatient_RealisticCommand_Success()
        {
            // A complete amendment with values that only differ from the rules by
            // the intended amount (ID present, DOB in the past, 10 numeric card, ...)
            var result = await _validator.ValidateAsync(
                ValidCommand(), TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task UpdatePatient_ValidId_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = id }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary of "Height must be greater than 0": 0.01 is the first positive
        // decimal step above the rule boundary, 156.8 is a typical adult height and
        // 215.5 is close to the tallest plausible patient
        [Theory]
        [InlineData(0.01)]
        [InlineData(156.8)]
        [InlineData(215.5)]
        public async Task UpdatePatient_ValidHeight_Success(decimal height)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Height = height }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary of "Weight must be greater than 0": 0.01 is the first positive
        // decimal step above the rule boundary, 0.5 kg is a premature newborn (the
        // smallest plausible patient), 51.7 kg an average adult and 183.4 kg severe
        // obesity
        [Theory]
        [InlineData(0.01)]
        [InlineData(0.5)]
        [InlineData(51.7)]
        [InlineData(183.4)]
        public async Task UpdatePatient_ValidWeight_Success(decimal weight)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Weight = weight }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: date of birth exactly equal to today still satisfies
        // "must not be in future"
        [Fact]
        public async Task UpdatePatient_DateOfBirthToday_Success()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.Today) },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdatePatient_DateOfBirthInPast_Success()
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
        public async Task UpdatePatient_DefinedGender_Success(Gender gender)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Gender = gender }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Business rule (rule message and HealthInsuranceCardValidator): the card is
        // the Vietnamese new format - exactly 10 numeric characters
        [Theory]
        [InlineData("7016382459")] // exactly 10 characters, the format boundary
        [InlineData("8816372045")]
        public async Task UpdatePatient_InsuranceCardOfTenNumericCharacters_Success(string card)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = card },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
        [Fact]
        public async Task UpdatePatient_EmptyId_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = Guid.Empty }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        // Boundary of "Height must be greater than 0": 0 is the boundary itself
        // (rejected, the rule is exclusive) and -0.01 is the first decimal step below
        [Theory]
        [InlineData(0)]
        [InlineData(-0.01)]
        public async Task UpdatePatient_InvalidHeight_Fail(decimal height)
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
        public async Task UpdatePatient_InvalidWeight_Fail(decimal weight)
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
        public async Task UpdatePatient_UndefinedGender_Fail(Gender gender)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Gender = gender }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Gender", result.Errors[0].PropertyName);
        }

        // Boundary: tomorrow is the first rejected day of the "not in future" rule
        [Fact]
        public async Task UpdatePatient_DateOfBirthTomorrow_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.Today).AddDays(1) },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("DateOfBirth", result.Errors[0].PropertyName);
        }

        // The card rule accepts exactly 10 numeric characters: 9 and 11 characters
        // sit on either side of the format boundary, 15 characters is the old card
        // format documented as out of scope
        [Theory]
        [InlineData("881637204")] // 9 characters, just below the boundary
        [InlineData("88163720451")] // 11 characters, just above the boundary
        [InlineData("881637204512345")] // 15 characters, the old card format
        [InlineData("SG26045189")] // 10 characters but not all numeric
        [InlineData("")] // boundary: no characters at all
        [InlineData("   ")] // boundary: whitespace only
        public async Task UpdatePatient_InvalidInsuranceCard_Fail(string card)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { HealthInsuranceCard = card },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("HealthInsuranceCard", result.Errors[0].PropertyName);
        }

        // Boundary: an empty string and whitespace-only are the two boundaries
        // NotEmpty rejects, one row per required string field so every rule is
        // proven in isolation
        [Theory]
        [InlineData(nameof(UpdatePatientCommand.FirstName), "")]
        [InlineData(nameof(UpdatePatientCommand.FirstName), "   ")]
        [InlineData(nameof(UpdatePatientCommand.LastName), "")]
        [InlineData(nameof(UpdatePatientCommand.LastName), "   ")]
        [InlineData(nameof(UpdatePatientCommand.PhoneNumber), "")]
        [InlineData(nameof(UpdatePatientCommand.PhoneNumber), "   ")]
        [InlineData(nameof(UpdatePatientCommand.MedicalRecordCode), "")]
        [InlineData(nameof(UpdatePatientCommand.MedicalRecordCode), "   ")]
        [InlineData(nameof(UpdatePatientCommand.Address), "")]
        [InlineData(nameof(UpdatePatientCommand.Address), "   ")]
        [InlineData(nameof(UpdatePatientCommand.City), "")]
        [InlineData(nameof(UpdatePatientCommand.City), "   ")]
        public async Task UpdatePatient_EmptyRequiredField_Fail(string property, string value)
        {
            var command = ValidCommand();
            switch (property)
            {
                case nameof(UpdatePatientCommand.FirstName):
                    command.FirstName = value;
                    break;
                case nameof(UpdatePatientCommand.LastName):
                    command.LastName = value;
                    break;
                case nameof(UpdatePatientCommand.PhoneNumber):
                    command.PhoneNumber = value;
                    break;
                case nameof(UpdatePatientCommand.MedicalRecordCode):
                    command.MedicalRecordCode = value;
                    break;
                case nameof(UpdatePatientCommand.Address):
                    command.Address = value;
                    break;
                case nameof(UpdatePatientCommand.City):
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
        public async Task UpdatePatient_AllFieldsInvalid_Fail()
        {
            /*
             * Boundary: every rule that can fail at once fails independently, so the
             * client learns about all mistakes in a single response
             */
            var command = new UpdatePatientCommand
            {
                Id = Guid.Empty,
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

            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
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

        private static UpdatePatientCommand ValidCommand() => new()
        {
            Id = Guid.CreateVersion7(),
            FirstName = "Minh Quân",
            LastName = "Trần Hoàng Minh",
            PhoneNumber = "0976251843",
            DateOfBirth = new DateOnly(1991, 11, 9),
            Gender = Gender.Male,
            Height = 172.5m,
            Weight = 68.4m,
            MedicalRecordCode = "BN-2026-005291",
            HealthInsuranceCard = "7016382459",
            Address = "18 Lý Thái Tổ, quận Hoàn Kiếm",
            City = "Hà Nội",
        };
    }
}
