using Domain.Services.Dtos;

namespace Application.Features.Diagnose;

public class DiagnoseQuery : IQuery
{
    public required Guid DiseaseId { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required bool IsMale { get; set; }
    public required decimal Weight { get; set; }
    public required decimal SerumCreatine { get; set; }
    public required bool Confusion { get; set; }
    public required decimal? Urea { get; set; }
    public required int Respiratory { get; set; }
    public required decimal SystolicBloodPressure { get; set; }
    public required decimal DiastolicBloodPressure { get; set; }
    public required List<Guid> IcuHospitalizeCriteria { get; set; }
    public required List<Guid> ResistanceRiskFactors { get; set; }
    public required List<Guid> OtherCriteria { get; set; }
}