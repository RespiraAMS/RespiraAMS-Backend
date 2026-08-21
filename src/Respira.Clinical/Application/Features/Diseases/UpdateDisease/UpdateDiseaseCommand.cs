namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseCommand : ICommand
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Disease name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Disease description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Disease's ICU score minimum threshold (&gt;= threshold) to consider needing ICU hospitalization
    /// </summary>
    public required int IcuScoreThreshold { get; set; }
}
