using System.Text.RegularExpressions;

namespace Respira.Patient.Domain.Utils
{
    public static partial class PhoneNumberValidator
    {
        /// <summary>
        ///  Regex for Vietnamese phone number.
        ///  +84 is the international dialing code for Vietnam ('+' is optional).
        ///  03/05/07/08/09 are valid Vietnamese mobile network prefixes
        /// (after the 2018 mobile number standardization by the Ministry of Information and Communications).
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex(@"^(\+84|84|0)(3|5|7|8|9)[0-9]{8}$")]
        private static partial Regex PhoneNumberRegex();

        public static bool IsValid(string phoneNumber)
        {
            return PhoneNumberRegex().IsMatch(phoneNumber);
        }
    }
}
