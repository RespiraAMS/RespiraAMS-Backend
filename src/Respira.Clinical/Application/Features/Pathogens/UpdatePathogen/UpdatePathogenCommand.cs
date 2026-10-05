using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Pathogens.UpdatePathogen
{
    public record UpdatePathogenCommand : ICommand
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Pathogen name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Pathogen description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Boolean flag to indicate if the pathogen is atypical.
        /// </summary>
        public required bool IsAtypical { get; set; }
    }
}
