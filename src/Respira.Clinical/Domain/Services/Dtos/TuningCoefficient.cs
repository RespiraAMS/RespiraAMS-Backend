namespace Domain.Services.Dtos;

public class TuningCoefficient
{
    public decimal SeverityMatchWeight { get; set; } = 350m;
    public decimal TreatmentSiteMatchWeight { get; set; } = 350m;
    public decimal SpecialInfectionWeight { get; set; } = 200m;
    public decimal CriteriaMatchWeight { get; set; } = 150m;
    public decimal VersionWeight { get; set; } = 50m;
    public decimal IssueDateWeight { get; set; } = 30m;
}