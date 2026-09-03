namespace Application.Features.Causes.DeleteCause;

public record DeleteCauseCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Disease's cause ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}