using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.GetCriteria
{
    public record GetCriteriaQuery : IQuery;

    /// <summary>
    /// Criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    public record CriterionItem(Guid Id, string Name);

    /// <summary>
    /// Get criteria result
    /// </summary>
    /// <param name="Criteria">Criteria</param>
    public record GetCriteriaResult(IEnumerable<CriterionItem> Criteria);

}
