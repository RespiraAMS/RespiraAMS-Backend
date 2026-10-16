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
        public required string FirstName { get; set => field = NameNormalizer.NameNormalize(value); }

        /// <summary>
        /// Patient last name
        /// </summary>
        public required string LastName { get; set => field = NameNormalizer.NameNormalize(value); }

        /// <summary>
        /// Patient's phone number. Should conform to Vietnamese format:<br/>
        /// 1. 03/05/07/08/09xxxxxxxx (accordding to the 2018 migrations from 11 digits to 10)<br/>
        /// 2. The internation format (84[3/5/7/8/9]xxxxxxxx or +84[3/5/7/8/9]xxxxxxxx)<br/>
        /// Note: the 3/5/7/8/9 are mobile network prefixes in Vietnam.
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

        /// <summary>
        /// Calculate a patient age based on their date of birth. Age is
        /// only calculate using year
        /// </summary>
        /// <param name="dob">Date of birth</param>
        /// <returns>Patient age</returns>
        public static int Age(DateOnly dob) => DateTimeOffset.Now.Year - dob.Year;

        /// <summary>
        /// Check if patient is above 15 years old (this system only accept 16 years old or older
        /// patients, children will not be covered by this system)
        /// </summary>
        /// <returns>True if patient age is valid</returns>
        public bool IsValidAge() => Age() >= 16;

        /// <summary>
        /// Check if a patient age is above 15 old
        /// </summary>
        /// <param name="dob">Date of birth</param>
        /// <returns>True if patient age is valid</returns>
        public static bool IsValidAge(DateOnly dob) => Age(dob) >= 16;

        /// <summary>
        /// Patient full name (Vietnamese style: LastName + FirstName)
        /// </summary>
        public string FullName => $"{LastName} {FirstName}";
    }
}
