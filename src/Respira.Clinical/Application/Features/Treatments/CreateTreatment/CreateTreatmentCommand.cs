using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Treatments.CreateTreatment
{
    public record CreateTreatmentCommand : ICommand
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<List<Guid>> Medicines { get; set; } = [];
        public List<Guid> Pathogens { get; set; } = [];
        public List<Guid> Criteria { get; set; } = [];
    }

    public record CreateTreatmentResult(Guid Id);
}
