using Respira.Patient.Domain.Utils;
using Respira.ServiceDefaults.Models;

namespace Respira.Patient.Domain.Entities
{
    public class NextOfKin : Base
    {
        public required Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public required string FullName
        {
            get; set
            {
                field = NameNormalizer.NameNormalize(field);
            }
        }
        public required string PhoneNumber { get; set; }
        public required string Relationship { get; set; }
    }
}
