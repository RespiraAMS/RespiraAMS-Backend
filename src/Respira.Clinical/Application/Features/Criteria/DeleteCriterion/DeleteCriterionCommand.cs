using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.DeleteCriterion
{
    public record DeleteCriterionCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
