using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Criteria.GetReferencedEntities
{
    public record GetReferencedEntitiesQuery : IQuery
    {
        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    public record ReferencedEntitiesResult
    {
        /// <summary>
        /// Is there any referenced entities that reference this criterion
        /// </summary>
        public required bool HasReferencedEntities { get; set; }

        /// <summary>
        /// The list of risk factor IDs that reference this criterion
        /// </summary>
        public required IEnumerable<Guid> RiskFactorReference { get; set; }

        /// <summary>
        /// The list of metric rule IDs that reference this criterion
        /// </summary>
        public required IEnumerable<Guid> MetricRuleReference { get; set; }

        /// <summary>
        /// The list of treatment IDs that reference this criterion
        /// </summary>
        public required IEnumerable<Guid> TreatmentReference { get; set; }
    }
}
