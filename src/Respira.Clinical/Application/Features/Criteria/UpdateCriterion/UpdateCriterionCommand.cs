using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.UpdateCriterion
{
    public record UpdateCriterionCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required FormulaDto Formula { get; set; }
    }
}
