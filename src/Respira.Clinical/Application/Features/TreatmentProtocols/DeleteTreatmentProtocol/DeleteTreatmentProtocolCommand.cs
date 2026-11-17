using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.DeleteTreatmentProtocol
{
    public record DeleteTreatmentProtocolCommand : ICommand
    {
        /// <summary>
        /// Treatment protocol ID
        /// </summary>
        public required Guid Id { get; set; }
    }
}
