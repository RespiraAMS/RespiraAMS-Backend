using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.SuspectedCauses.DeleteSuspectedCause
{
    public record DeleteSuspectedCauseCommand : ICommand
    {
        /// <summary>
        /// ID of suspected cause to delete
        /// </summary>
        public required Guid Id { get; set; }
    }
}
