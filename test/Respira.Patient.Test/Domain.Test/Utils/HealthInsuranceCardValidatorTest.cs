using Respira.Patient.Domain.Utils;
using Xunit;

namespace Respira.Patient.Domain.Test.Utils
{
    public class HealthInsuranceCardValidatorTest
    {
        [Theory]
        [InlineData("1234567890")]
        [InlineData("3123127643")]
        [InlineData("4327894237")]
        [InlineData("4378942389")]
        public void IsValidHealthInsuranceCard_Valid_Success(string healthInsuranceCard)
        {
            var result = HealthInsuranceCardValidator.IsValid(healthInsuranceCard);
            Assert.True(result);
        }

        [Theory]
        [InlineData("12345678901")]
        [InlineData("312312764")]
        [InlineData("HG43278942")]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData("$%7238777")]
        public void IsValidHealthInsuranceCard_Invalid_Success(string healthInsuranceCard)
        {
            var result = HealthInsuranceCardValidator.IsValid(healthInsuranceCard);
            Assert.False(result);
        }
    }
}
