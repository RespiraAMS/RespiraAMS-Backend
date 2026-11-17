using Respira.Patient.Domain.Utils;
using Xunit;

namespace Respira.Patient.Domain.Test.Utils
{
    public class PhoneNumberValidatorTest
    {
        [Theory]
        [InlineData("0312345678")]
        [InlineData("0512345678")]
        [InlineData("0712345678")]
        [InlineData("0812345678")]
        [InlineData("0912345678")]
        [InlineData("84912345678")]
        [InlineData("+84912345678")]
        public void PhoneNumberValidator_IsValid(string phoneNumber)
        {
            Assert.True(PhoneNumberValidator.IsValid(phoneNumber));
        }

        [Theory]
        [InlineData("01123456789")]
        [InlineData("02123456789")]
        [InlineData("04123456789")]
        [InlineData("06123456789")]
        [InlineData("07123456789")]
        [InlineData("019123456789")]
        [InlineData("8409123456789")]
        [InlineData("VN912345678")]
        public void PhoneNumberValidator_IsNotValid(string phoneNumber)
        {
            Assert.False(PhoneNumberValidator.IsValid(phoneNumber));
        }
    }
}
