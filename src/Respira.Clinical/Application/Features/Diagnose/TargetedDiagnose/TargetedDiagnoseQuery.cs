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
    /// <summary>
    /// Patient's creatine clearance calculated
    /// </summary>
    public required decimal Crcl { get; set; }

    /// <summary>
    /// List of recommended medicines
    /// </summary>
    public required List<AntibioticResult> Recommendations { get; set; } = [];

    /// <summary>
    /// List of all medicines that are relevent with patient's symptoms.
    /// Even if doctors disagree with the Recommendations list,
    /// they should only picked medicines from this list
    /// </summary>
    public required List<AntibioticResult> Medicines { get; set; } = [];
}
