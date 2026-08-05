namespace Application.Features.Causes.DeleteCause;

public class DeleteCauseCommand : ICommand
{
    public required Guid Id { get; set; }
}