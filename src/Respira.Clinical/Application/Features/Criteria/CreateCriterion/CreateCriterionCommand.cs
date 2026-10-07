using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.CreateCriterion
{
    public record CreateCriterionCommand : ICommand
    {
        public required string Name { get; set; }
        public required FormulaDto Formula { get; set; }
    }

    public record CreateCriterionResult(Guid Id);
}
