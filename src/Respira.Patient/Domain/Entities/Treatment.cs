using Respira.ServiceDefaults.Models;

namespace Respira.Patient.Domain.Entities
{
    public class Treatment : Base
    {
        public required Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
    }
}
