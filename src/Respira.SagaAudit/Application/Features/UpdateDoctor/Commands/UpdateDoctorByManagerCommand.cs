using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;

/// <summary>
/// Starts the UpdateDoctor saga. Issued by a manager/admin doctor; carries both the new
/// values to apply and the old values required to compensate (revert) a failed step.
/// </summary>
public record UpdateDoctorByManagerCommand : ICommand
{
    /// <summary>Id of the manager/admin doctor initiating the update.</summary>
    public required Guid ManagerDoctorId { get; init; }

    /// <summary>The shared entity ID — same for both Auth and Doctor tables (AuthUserId == DoctorId).</summary>
    public required Guid EntityId { get; init; }

    // Deprecated: use EntityId instead
    /// <summary>Deprecated — always equals EntityId.</summary>
    [Obsolete("Use EntityId instead")]
    public Guid AuthUserId => EntityId;

    /// <summary>Deprecated — always equals EntityId.</summary>
    [Obsolete("Use EntityId instead")]
    public Guid DoctorId => EntityId;
    /// <summary>Current avatar media id of the doctor.</summary>
    public required Guid MediaId { get; init; }

    // --- Auth (new) ---
    /// <summary>New email to apply to the auth account.</summary>
    public required string Email { get; init; }
    /// <summary>New phone to apply to the auth account.</summary>
    public required string Phone { get; init; }
    /// <summary>New role to apply to the auth account.</summary>
    public required Domain.Enums.RoleType Role { get; init; }

    // --- Auth (old) ---
    /// <summary>Previous email, used to compensate a failed auth update.</summary>
    public required string OldEmail { get; init; }
    /// <summary>Previous phone, used to compensate a failed auth update.</summary>
    public required string OldPhone { get; init; }
    /// <summary>Previous role, used to compensate a failed auth update.</summary>
    public required Domain.Enums.RoleType OldRole { get; init; }

    // --- Doctor (new) ---
    /// <summary>New first name to apply to the doctor profile.</summary>
    public required string FirstName { get; init; }
    /// <summary>New last name to apply to the doctor profile.</summary>
    public required string LastName { get; init; }
    /// <summary>New degrees to apply to the doctor profile.</summary>
    public required List<Domain.Enums.DegreeType> Degrees { get; init; }
    /// <summary>New academic title to apply to the doctor profile.</summary>
    public required Domain.Enums.AcademicTitleEnum AcademicTitle { get; init; }
    /// <summary>New position to apply to the doctor profile.</summary>
    public required Domain.Enums.PositionType Position { get; init; }
    /// <summary>New gender to apply to the doctor profile.</summary>
    public Domain.Enums.GenderType Gender { get; init; }
    /// <summary>New citizen identification number to apply to the doctor profile.</summary>
    public required string CitizenIdentificationNumber { get; init; }
    /// <summary>New date of birth to apply to the doctor profile, if provided.</summary>
    public DateTimeOffset? DateOfBirth { get; init; }
    /// <summary>New address to apply to the doctor profile.</summary>
    public required string Address { get; init; }

    // --- Doctor (old) ---
    /// <summary>Previous first name, used to compensate a failed doctor update.</summary>
    public required string OldFirstName { get; init; }
    /// <summary>Previous last name, used to compensate a failed doctor update.</summary>
    public required string OldLastName { get; init; }
    /// <summary>Previous degrees, used to compensate a failed doctor update.</summary>
    public required List<Domain.Enums.DegreeType> OldDegrees { get; init; }
    /// <summary>Previous academic title, used to compensate a failed doctor update.</summary>
    public required Domain.Enums.AcademicTitleEnum OldAcademicTitle { get; init; }
    /// <summary>Previous position, used to compensate a failed doctor update.</summary>
    public required Domain.Enums.PositionType OldPosition { get; init; }
    /// <summary>Previous gender, used to compensate a failed doctor update.</summary>
    public Domain.Enums.GenderType OldGender { get; init; }
    /// <summary>Previous citizen identification number, used to compensate a failed doctor update.</summary>
    public required string OldCitizenIdentificationNumber { get; init; }
    /// <summary>Previous date of birth, used to compensate a failed doctor update, if provided.</summary>
    public DateTimeOffset? OldDateOfBirth { get; init; }
    /// <summary>Previous address, used to compensate a failed doctor update.</summary>
    public required string OldAddress { get; init; }

    // --- Optional new avatar (pre-uploaded via Media upload endpoint) ---
    /// <summary>When true, the avatar is replaced with the newly uploaded media.</summary>
    public bool HasNewMedia { get; init; }
    /// <summary>Id of the newly uploaded avatar media, when <see cref="HasNewMedia"/> is true.</summary>
    public Guid? NewMediaId { get; init; }
}
