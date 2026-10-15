using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Utils;
using Respira.ServiceDefaults.Models;

namespace Respira.Patient.Domain.Entities
{
    public class Patient : Base
    {
        public required string FirstName
        {
            get; set
            {
                field = NameNormalizer.NameNormalize(value);
            }
        }
        public required string LastName
        {
            get; set
            {
                field = NameNormalizer.NameNormalize(value);
            }
        }
        public required string PhoneNumber { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required Gender Gender { get; set; }
        public required decimal Height { get; set; }
        public required decimal Weight { get; set; }
        public required string MedicalRecordCode { get; set; }
        public required string HealthInsuranceCard { get; set; }
        public required string Address { get; set; }
        public required string City { get; set; }
        public required DateTimeOffset Admission { get; set; }
        public DateTimeOffset? Discharge { get; set; }
        public required TreatmentOutcome TreatmentOutcome { get; set; } = TreatmentOutcome.InTreatment;
        public ICollection<NextOfKin> NextOfKins { get; set; } = [];
        public ICollection<Treatment> Treatments { get; set; } = [];

        /// <summary>
        /// Age is only calculated using year, not full date
        /// </summary>
        public int Age => DateTimeOffset.Now.Year - DateOfBirth.Year;

        /// <summary>
        /// Patient full name (Vietnamese style: LastName + FirstName)
        /// </summary>
        public string FullName => $"{LastName} {FirstName}";
    }
}
