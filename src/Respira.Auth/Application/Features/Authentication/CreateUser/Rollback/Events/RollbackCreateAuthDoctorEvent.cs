namespace Application.Features.Authentication.CreateUser.Rollback.Events;

public record RollbackCreateAuthDoctorSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}

public record RollbackCreateAuthDoctorFailure
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}
