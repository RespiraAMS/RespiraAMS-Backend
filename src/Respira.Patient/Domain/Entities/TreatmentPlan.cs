using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Models;
using Respira.ServiceDefaults.Models;

namespace Respira.Patient.Domain.Entities
{
    public class TreatmentPlan : Base
    {
        public required Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public required Guid DoctorId { get; set; }
        public required Guid DiagnosisId { get; set; }
        public required string Diagnosis { get; set; }

        /// <summary>
        /// Store as JSONB, so we use List here instead of ICollection
        /// </summary>
        public List<Antibiotic> Medicines { get; set; } = [];
        public string? ReasonForMedicineChoice { get; set; }
        public required AdherenceState AdherenceState { get; set; }
        public required TreatmentPlanStatus Status { get; set; }
        public string? CloseReason { get; set; }
    }
}
