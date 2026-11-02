using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.GetCriterionById
{
    public record GetCriterionByIdQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record CriterionResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string FormulaString { get; set; }
        public required FormulaDto Formula { get; set; }
    }
}
