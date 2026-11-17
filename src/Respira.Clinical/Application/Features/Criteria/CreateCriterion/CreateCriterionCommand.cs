using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.CreateCriterion
{
    public record CreateCriterionCommand : ICommand
    {
        /// <summary>
        /// Criterion name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Criterion formula
        /// </summary>
        public required FormulaDto Formula { get; set; }
    }

    /// <summary>
    /// Create criterion result
    /// </summary>
    /// <param name="Id">Created criterion ID</param>
    public record CreateCriterionResult(Guid Id);
}
