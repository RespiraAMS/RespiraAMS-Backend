namespace Application.Features.Diagnose.Shared;

public class AntibioticResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid AntibioticGroupId { get; set; }
    public required string AntibioticGroupName { get; set; }
    public required string Dose { get; set; }
}
