using Respira.Patient.Domain.Enums;
using Respira.Patient.Domain.Models;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.TreatmentPlans.GetTreatmentPlanById
{
    public record GetTreatmentPlanByIdQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record PatientResult(Guid Id, string Name);

    public record TreatmentPlanResult
    {
        public required Guid Id { get; set; }
        public PatientResult Patient { get; set; } = null!;
        public required Guid DoctorId { get; set; }
        public required string Diagnosis { get; set; }
        public List<Antibiotic> Medicines { get; set; } = [];
        public string? ReasonForMedicineChoice { get; set; }
        public required AdherenceState AdherenceState { get; set; }
        public required TreatmentPlanStatus Status { get; set; }
        public string? CloseReason { get; set; }
    }
}
