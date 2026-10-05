using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.GetCriteria
{
    public record GetCriteriaQuery : IQuery;
    public record CriterionItem(Guid Id, string Name);
    public record GetCriteriaResult(IEnumerable<CriterionItem> Criteria);

}
