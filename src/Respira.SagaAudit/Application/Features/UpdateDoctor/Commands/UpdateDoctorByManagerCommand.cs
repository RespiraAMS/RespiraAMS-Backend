using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;

/// <summary>
/// Starts the UpdateDoctor saga. Issued by a manager/admin doctor; carries both the new
/// values to apply and the old values required to compensate (revert) a failed step.
/// </summary>
public record UpdateDoctorByManagerCommand : ICommand
{
    public required Guid ManagerDoctorId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid MediaId { get; init; }

    // --- Auth (new) ---
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required Domain.Enums.RoleType Role { get; init; }

    // --- Auth (old) ---
    public required string OldEmail { get; init; }
    public required string OldPhone { get; init; }
    public required Domain.Enums.RoleType OldRole { get; init; }

    // --- Doctor (new) ---
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required List<Domain.Enums.DegreeType> Degrees { get; init; }
    public required Domain.Enums.AcademicTitleEnum AcademicTitle { get; init; }
    public required Domain.Enums.PositionType Position { get; init; }
    public Domain.Enums.GenderType Gender { get; init; }
    public required string CitizenIdentificationNumber { get; init; }
    public DateTimeOffset? DateOfBirth { get; init; }
    public required string Address { get; init; }

    // --- Doctor (old) ---
    public required string OldFirstName { get; init; }
    public required string OldLastName { get; init; }
    public required List<Domain.Enums.DegreeType> OldDegrees { get; init; }
    public required Domain.Enums.AcademicTitleEnum OldAcademicTitle { get; init; }
    public required Domain.Enums.PositionType OldPosition { get; init; }
    public Domain.Enums.GenderType OldGender { get; init; }
    public required string OldCitizenIdentificationNumber { get; init; }
    public DateTimeOffset? OldDateOfBirth { get; init; }
    public required string OldAddress { get; init; }

    // --- Optional new avatar (pre-uploaded via Media upload endpoint) ---
    public bool HasNewMedia { get; init; }
    public Guid? NewMediaId { get; init; }
}
