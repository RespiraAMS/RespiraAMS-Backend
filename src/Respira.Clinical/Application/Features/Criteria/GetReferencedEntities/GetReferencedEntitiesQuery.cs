using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.GetReferencedEntities
{
    public record GetReferencedEntitiesQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record ReferencedEntitiesResult
    {
        public required bool HasReferencedEntities { get; set; }
        public required IEnumerable<Guid> RiskFactorReference { get; set; }
        public required IEnumerable<Guid> MetricsRuleReference { get; set; }
        public required IEnumerable<Guid> TreatmentReference { get; set; }
    }
}
