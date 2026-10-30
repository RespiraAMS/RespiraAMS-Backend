using Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.API.Dtos
{
    public record UpdateTreatmentRequestDto
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<List<Guid>> Medicines { get; set; } = [];
        public List<Guid> Pathogens { get; set; } = [];
        public List<Guid> Criteria { get; set; } = [];

        public UpdateTreatmentProtocolCommand ToCommand(Guid id)
        {
            return new UpdateTreatmentProtocolCommand
            {
                Id = id,
                Severity = Severity,
                TreatmentSite = TreatmentSite,
                Medicines = Medicines,
                Pathogens = Pathogens,
                Criteria = Criteria,
            };
        }
    }
}
