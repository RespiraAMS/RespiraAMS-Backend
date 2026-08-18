using Application.Features.Diagnose.Shared;

namespace Application.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseQuery : IQuery
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient gender
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's weight in kilogram
    /// </summary>
    public required decimal Weight { get; set; }

    /// <summary>
    /// Patient's height in meter
    /// </summary>
    public required decimal Height { get; set; }

    /// <summary>
    /// Serum creatine used for calculate GFR
    /// </summary>
    public required decimal SerumCreatine { get; set; }
}


public class TargetedDiagnoseResult
{
    public required decimal Crcl { get; set; }
    public required List<AntibioticResult> Recommendations { get; set; } = [];
    public required List<AntibioticResult> Medicines { get; set; } = [];
}
