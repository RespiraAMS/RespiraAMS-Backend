using Application.Features.Shared.ManageCriterion;

namespace Application.Features.Diseases.GetDiseaseCriteria;

public class GetDiseaseCriteriaQuery : IQuery
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid Id { get; set; }
}

public class DiseaseCriteriaResult
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
