namespace Application.Features.Authentication.DeleteUser.Events;

public record DeleteAuthDoctorSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
}

public record DeleteAuthDoctorFailure
{
    public required Guid SagaId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required string Message { get; init; }
}
