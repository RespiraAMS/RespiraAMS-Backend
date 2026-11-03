using Respira.Patient.Domain.Models;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan
{
    [MessageIdentity("upsert-treatment-plan-command")]
    public record UpsertTreatmentPlanCommand : ICommand
    {
        public required Guid DoctorId { get; set; }
        public required Guid PatientId { get; set; }
        public required Guid DiagnosisId { get; set; }
        public required string Diagnosis { get; set; }
        public required List<Antibiotic> Medicines { get; set; }
        public string? ReasonForMedicineChoice { get; set; }
        public required bool IsAdhere { get; set; }
    }

    [MessageIdentity("upsert-treatment-plan-result")]
    public record UpsertTreatmentPlanResult(Guid Id);
}
