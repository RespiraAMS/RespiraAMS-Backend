using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Treatments.UpdateTreatment
{
    public record UpdateTreatmentCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<List<Guid>> Medicines { get; set; } = [];
        public List<Guid> Pathogens { get; set; } = [];
        public List<Guid> Criteria { get; set; } = [];
    }
}
