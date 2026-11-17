using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.UpdateCriterion
{
    public record UpdateCriterionCommand : ICommand
    {
        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Criterion name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Criterion formula
        /// </summary>
        public required FormulaDto Formula { get; set; }
    }
}
