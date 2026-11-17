using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.TreatmentPlans.CloseTreatmentPlan
{
    public record CloseTreatmentPlanCommand : ICommand
    {
        public required Guid TreatmentPlanId { get; set; }
        public required string Reason { get; set; }
    }
}
