using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.DeleteTreatmentProtocol
{
    public record DeleteTreatmentProtocolCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
