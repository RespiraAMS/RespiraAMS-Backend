namespace Application.Features.Causes.DeleteCause;

public class DeleteCauseCommand : ICommand
{
    /// <summary>
    /// Disease's cause ID
    /// </summary>
    public required Guid Id { get; set; }
}