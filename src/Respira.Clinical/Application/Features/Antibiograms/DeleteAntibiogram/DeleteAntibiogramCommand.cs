using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Antibiograms.DeleteAntibiogram
{
    public record DeleteAntibiogramCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
