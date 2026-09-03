using Application.Features.Shared.ManageCriterion;

namespace Application.Features.Diseases.GetDiseaseCriteria;

public record GetDiseaseCriteriaQuery(Guid Id) : IQuery
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}

public record DiseaseCriteriaResult
{
    /// <summary>
    /// List of ICU hospitalize criteria belong to the queried disease
    /// </summary>
    public required IEnumerable<CriterionItem> IcuHospitalizeCriteria { get; set; }

    /// <summary>
    /// List of resistance risk factor's criteria belong to the queried disease
    /// </summary>
    public required IEnumerable<CriterionItem> ResistanceRiskFactorCriteria { get; set; }

    /// <summary>
    /// List of other secondary criteria in empiric treatment protocols assigned to the queried disease
    /// </summary>
    public required IEnumerable<CriterionItem> OtherCriteria { get; set; }
}
