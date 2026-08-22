namespace Application.Features.Authentication.UpdateUser.Rollback.Events;

public record RollbackUpdateAuthDoctorSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}

public record RollbackUpdateAuthDoctorFailure
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}
