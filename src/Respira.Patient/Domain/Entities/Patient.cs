using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Utils;
using Respira.ServiceDefaults.Models;

namespace Respira.Patient.Domain.Entities
{
    public class Patient : Base
    {
        /// <summary>
        /// Patient first name
        /// </summary>
        public required string FirstName
        {
            get; set
            {
                field = NameNormalizer.NameNormalize(value);
            }
        }

        /// <summary>
        /// Patient last name
        /// </summary>
        public required string LastName
        {
            get; set
            {
                field = NameNormalizer.NameNormalize(value);
            }
        }

        /// <summary>
        /// Patient's phone number. Should be Vietnamese format (either 0xxxxxxxxx or
        /// +84xxxxxxxxx). See more at <see cref="PhoneNumberValidator"/>
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Patient's date of birth. As per business rule: we only accept patient
        /// above 15. CAP for children won't be covered by this system
        /// </summary>
        public required DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// Patient's gender
        /// </summary>
        public required Gender Gender { get; set; }

        /// <summary>
        /// Patient's height, calculated in centimeter
        /// </summary>
        public required decimal Height { get; set; }

        /// <summary>
        /// Patient's weight, calculated in kilogram
        /// </summary>
        public required decimal Weight { get; set; }

        /// <summary>
        /// Patient's medical record code. This code is supplied by the doctor
        /// in charged when input patient data, since this system current not
        /// integrated with HIS
        /// </summary>
        public required string MedicalRecordCode { get; set; }

        /// <summary>
        /// Patient's health insurance card number. This is the Vietnamese
        /// new health insurance card number, which consist of 10 numeric digits
        /// </summary>
        public required string HealthInsuranceCard { get; set; }

        /// <summary>
        /// Patient's address
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Patient's city
        /// </summary>
        public required string City { get; set; }

        /// <summary>
        /// Patient's admission datetime
        /// </summary>
        public required DateTimeOffset Admission { get; set; }

        /// <summary>
        /// Patient's discharge datetime
        /// </summary>
        public DateTimeOffset? Discharge { get; set; }

        /// <summary>
        /// Patient's treatment outcome
        /// </summary>
        public required TreatmentOutcome TreatmentOutcome { get; set; } = TreatmentOutcome.InTreatment;

        /// <summary>
        /// Patient's next of kin
        /// </summary>
        public ICollection<NextOfKin> NextOfKins { get; set; } = [];

        /// <summary>
        /// Patient's treatments
        /// </summary>
        public ICollection<Treatment> Treatments { get; set; } = [];

        /// <summary>
        /// Age is only calculated using year, not full date
        /// </summary>
        public int Age() => DateTimeOffset.Now.Year - DateOfBirth.Year;

        public static int Age(DateOnly dob) => DateTimeOffset.Now.Year - dob.Year;

        /// <summary>
        /// Check if patient is above 16 years old (this system only accept 16 years old or older
        /// patients, children will not be covered by this system)
        ///
        /// </summary>
        /// <returns>True if patient age is valid</returns>
        public bool IsValidAge() => Age() >= 16;

        public static bool IsValidAge(DateOnly dob) => Age(dob) >= 16;

        /// <summary>
        /// Patient full name (Vietnamese style: LastName + FirstName)
        /// </summary>
        public string FullName => $"{LastName} {FirstName}";
    }
}
