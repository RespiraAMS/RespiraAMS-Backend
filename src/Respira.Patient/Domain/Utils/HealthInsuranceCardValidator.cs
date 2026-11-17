using System.Text.RegularExpressions;

namespace Respira.Patient.Domain.Utils
{
    public static partial class HealthInsuranceCardValidator
    {
        [GeneratedRegex("^[0-9]{10}$")]
        private static partial Regex HealthInsuranceCardRegex();

        public static bool IsValid(string healthInsuranceCard)
        {
            return HealthInsuranceCardRegex().IsMatch(healthInsuranceCard);
        }
    }
}
