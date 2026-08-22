namespace Application.Features.Authentication.UpdateUser.Events;

public record UpdateAuthDoctorSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}

public record UpdateAuthDoctorFailure
{
    public required Guid SagaId { get; init; }
    public required string Message { get; init; }
}
