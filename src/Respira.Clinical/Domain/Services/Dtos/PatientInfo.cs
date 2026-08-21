namespace Domain.Services.Dtos;

/// <summary>
/// Patient's information, like gender, age, height, weight,...
/// </summary>
public class PatientInfo
{
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
