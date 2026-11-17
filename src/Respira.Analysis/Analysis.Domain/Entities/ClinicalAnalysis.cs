using Respira.ServiceDefaults.Models;

namespace Respira.Analysis.Domain.Entities
{
    public sealed class ClinicalAnalysis : Base
    {
        public Guid? DoctorId { get; set; }
        public Guid? MedicineId { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string PatientIdsJson { get; set; } = "[]";
        public string TreatmentPlanIdsJson { get; set; } = "[]";
        public string ReportJson { get; set; } = "{}";
    }
}
