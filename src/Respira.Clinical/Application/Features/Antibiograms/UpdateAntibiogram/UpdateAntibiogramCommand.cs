using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram
{
    public record UpdateAntibiogramCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required Guid PathogenId { get; set; }
        public required MicCategory MicCategory { get; set; }
        public required List<Guid> AntibioticIds { get; set; }
    }
}
