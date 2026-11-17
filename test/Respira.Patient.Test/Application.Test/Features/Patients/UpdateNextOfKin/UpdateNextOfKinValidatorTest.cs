using Respira.Patient.Application.Features.Patients.UpdateNextOfKin;

namespace Respira.Patient.Application.Test.Features.Patients.UpdateNextOfKin
{
    public class UpdateNextOfKinValidatorTest
    {
        private readonly UpdateNextOfKinValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task UpdateNextOfKin_RealisticCommand_Success()
        {
            // Correcting a registered relative: UUID v7 kin ID, a Vietnamese full
            // name, a mobile number in the 0-prefix form and a relationship
            var result = await _validator.ValidateAsync(
                ValidCommand(), TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        /*
         * Business rule (PhoneNumberValidator doc): (+84 | 84 | 0) dialing prefix,
         * one of the post-2018 mobile prefixes (3/5/7/8/9) and exactly 8 more
         * digits. Boundary data: the 0-form is exactly 10 digits (shortest valid),
         * the allowed prefix set is exercised at both ends (3 and 9), and the two
         * international dialing forms drop the leading zero
         */
        [Theory]
        [InlineData("0912345678")]    // 0-form, upper prefix boundary 9, 10 digits
        [InlineData("0368472915")]    // 0-form, lower prefix boundary 3, 10 digits
        [InlineData("+84912345678")]  // +84 international form
        [InlineData("84912345678")]   // 84 international form
        public async Task UpdateNextOfKin_ValidPhoneNumber_Success(string phoneNumber)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { PhoneNumber = phoneNumber },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary of NotEmpty on a Guid: Guid.Empty is the only rejected value
        [Fact]
        public async Task UpdateNextOfKin_EmptyId_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        // NotEmpty rejects both the empty string and a whitespace-only name
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateNextOfKin_BlankFullName_Fail(string fullName)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { FullName = fullName },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("FullName", result.Errors[0].PropertyName);
        }

        /*
         * Boundaries of the phone regex: exactly 8 digits after the prefix, so 7
         * is one short and 9 is one too long; the allowed prefix set {3,5,7,8,9}
         * is pierced at its neighbors (2 below, 4 and 6 in the gaps); a number
         * without any dialing prefix and a non-digit separator never match
         */
        [Theory]
        [InlineData("091234567")]    // 7 digits after the prefix: one short
        [InlineData("09123456789")]   // 9 digits after the prefix: one too long
        [InlineData("0212345678")]    // prefix 2: just below the allowed minimum 3
        [InlineData("0412345678")]    // prefix 4: in the gap between 3 and 5
        [InlineData("0612345678")]    // prefix 6: in the gap between 5 and 7
        [InlineData("912345678")]     // missing the 0 / 84 / +84 dialing prefix
        [InlineData("0912-345678")]   // separator instead of pure digits
        [InlineData("")]              // no phone at all
        public async Task UpdateNextOfKin_InvalidPhoneNumber_Fail(string phoneNumber)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { PhoneNumber = phoneNumber },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("PhoneNumber", result.Errors[0].PropertyName);
        }

        // NotEmpty rejects both the empty string and a whitespace-only relationship
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateNextOfKin_BlankRelationship_Fail(string relationship)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Relationship = relationship },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Relationship", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateNextOfKin_AllFieldsInvalid_Fail()
        {
            // Every rule that can fail at once fails independently, so the client
            // learns about all mistakes in a single response
            var command = new UpdateNextOfKinCommand
            {
                Id = Guid.Empty,
                FullName = "",
                PhoneNumber = "0412345678", // invalid prefix 4
                Relationship = "",
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Equal(4, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "FullName");
            Assert.Contains(result.Errors, x => x.PropertyName == "PhoneNumber");
            Assert.Contains(result.Errors, x => x.PropertyName == "Relationship");
        }

        #endregion

        private static UpdateNextOfKinCommand ValidCommand() => new()
        {
            Id = Guid.CreateVersion7(),
            FullName = "Nguyễn Thị Mai Anh",
            PhoneNumber = "0987654321",
            Relationship = "Mẹ",
        };
    }
}
