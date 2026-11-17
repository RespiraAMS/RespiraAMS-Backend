using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment
{
    public record AntibioticQuery(Guid Id, string Name, AwareClassification Classification);

    [MessageIdentity("upsert-treatment-plan-command")]
    public class UpsertTreatmentPlanCommand : ICommand
    {
        public required Guid DoctorId { get; set; }
        public required Guid PatientId { get; set; }
        public required Guid DiagnosisId { get; set; }
        public required string Diagnosis { get; set; }
        public required List<AntibioticQuery> Medicines { get; set; }
        public string? ReasonForMedicineChoice { get; set; }
        public required bool IsAdhere { get; set; }
    }

    [MessageIdentity("upsert-treatment-plan-result")]
    public record UpsertTreatmentPlanResult(Guid Id);
}
