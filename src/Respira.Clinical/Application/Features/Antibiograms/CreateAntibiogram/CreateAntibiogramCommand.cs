using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram
{
    public record CreateAntibiogramCommand : ICommand
    {
        /// <summary>
        /// MIC category
        /// </summary>
        public required MicCategory MicCategory { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// List of antibiotics used to treat this pathogen.
        /// Note that, this list is a OR semantic. See more
        /// at <see cref="Antibiogram.Antibiotics" />
        /// </summary>
        public required List<Guid> AntibioticIds { get; set; }
    }

    /// <summary>
    /// Create antibiogram result
    /// </summary>
    /// <param name="Id">Created antibiogram ID</param>
    public record CreateAntibiogramResult(Guid Id);
}
