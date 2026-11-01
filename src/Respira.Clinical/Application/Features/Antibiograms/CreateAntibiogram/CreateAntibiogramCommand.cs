using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram
{
    public record CreateAntibiogramCommand : ICommand
    {
        public required Guid PathogenId { get; set; }
        public required MicCategory MicCategory { get; set; }
        public required List<Guid> AntibioticIds { get; set; }
    }

    public record CreateAntibiogramResult(Guid Id);
}
