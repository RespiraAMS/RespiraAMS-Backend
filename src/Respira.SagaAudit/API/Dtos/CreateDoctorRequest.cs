using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.API.Dtos;

/// <summary>HTTP request body for creating a doctor on behalf of a manager/admin doctor.</summary>
public record CreateDoctorRequest
{
    public required Guid ManagerDoctorId { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Phone { get; init; }
    public required RoleType Role { get; init; }

    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required List<DegreeType> Degrees { get; init; }
    public required AcademicTitleEnum AcademicTitle { get; init; }
    public required PositionType Position { get; init; }
    public GenderType Gender { get; init; }
    public required string CitizenIdentificationNumber { get; init; }
    public DateTimeOffset? DateOfBirth { get; init; }
    public required string Address { get; init; }

    public required string MediaFileName { get; init; }
    public required string MediaContentType { get; init; }
    public long MediaSize { get; init; }
    public required byte[] MediaData { get; init; }
}
