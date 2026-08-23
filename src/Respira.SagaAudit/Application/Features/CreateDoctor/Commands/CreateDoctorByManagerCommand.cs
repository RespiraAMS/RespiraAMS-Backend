using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.Application.Features.CreateDoctor.Commands;

/// <summary>
/// Command that starts the CreateDoctor saga. Issued by a manager/admin doctor and
/// provisions a new doctor account across Auth -> Doctor -> Media, linking the
/// uploaded avatar to the doctor profile at the end.
/// </summary>
public record CreateDoctorByManagerCommand : ICommand
{
    public required Guid SagaId { get; init; }

    /// <summary>Id of the manager/admin doctor that initiated the provisioning</summary>
    public required Guid ManagerDoctorId { get; init; }

    // --- Auth account ---
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Phone { get; init; }
    public required RoleType Role { get; init; }

    // --- Doctor profile ---
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required List<DegreeType> Degrees { get; init; }
    public required AcademicTitleEnum AcademicTitle { get; init; }
    public required PositionType Position { get; init; }
    public GenderType Gender { get; init; }
    public required string CitizenIdentificationNumber { get; init; }
    public DateTimeOffset? DateOfBirth { get; init; }
    public required string Address { get; init; }

    // --- Avatar (pre-uploaded via the Media upload endpoint) ---
    public required Guid MediaId { get; init; }
}
