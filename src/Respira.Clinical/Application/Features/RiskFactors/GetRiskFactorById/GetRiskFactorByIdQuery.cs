using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.GetRiskFactorById
{
    public record GetRiskFactorByIdQuery : IQuery
    {
        /// <summary>
        /// Risk factor ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    /// <summary>
    /// Pathogen result
    /// </summary>
    /// <param name="Id">Pathogen ID</param>
    /// <param name="Name">Pathogen name</param>
    /// <param name="IsAtypical">Is this pathogen atypical</param>
    public record PathogenResult(Guid Id, string Name, bool IsAtypical);

    /// <summary>
    /// Criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record CriterionResult(Guid Id, string Name, string Formula);

    public record RiskFactorResult
    {
        /// <summary>
        /// Risk factor ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Pathogen that associated with this risk factor
        /// </summary>
        public required PathogenResult Pathogen { get; set; }

        /// <summary>
        /// Risk factor criterion
        /// </summary>
        public required CriterionResult Criterion { get; set; }
    }
}
