using Application.Features.Shared.ManageCriterion;

namespace Application.Features.Diseases.GetDiseaseCriteria;

public class GetDiseaseCriteriaQuery : IQuery
{
    public required Guid Id { get; set; }
}

public class DiseaseCriteriaResult
{
    public required IEnumerable<CriterionItem> IcuHospitalizeCriteria { get; set; }
    public required IEnumerable<CriterionItem> ResistanceRiskFactorCriteria { get; set; }
    public required IEnumerable<CriterionItem> OtherCriteria { get; set; }
}