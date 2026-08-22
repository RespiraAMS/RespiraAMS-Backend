namespace Application.Features.Authentication.CreateUser.Events;

public record CreateAuthDoctorSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}

public record CreateAuthDoctorFailure
{
    public required Guid SagaId { get; init; }
    public required string Message { get; init; }
}
