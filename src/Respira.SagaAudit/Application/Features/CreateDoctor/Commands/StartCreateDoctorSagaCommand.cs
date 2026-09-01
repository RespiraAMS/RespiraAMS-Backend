using Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.SagaAudit.Application.Features.CreateDoctor.Commands;

/// <summary>
/// Command issued from the HTTP boundary to start the CreateDoctor saga slice.
/// Registers the process tracker and then dispatches
/// <see cref="CreateDoctorByManagerCommand"/> to the saga. Used both for creating
/// doctors (Manager/Admin) and managers (Admin, with <see cref="RoleType.Manager"/>).
/// </summary>
public record StartCreateDoctorSagaCommand : ICommand
{
    /// <summary>Id of the manager/admin doctor initiating the creation.</summary>
    public required Guid ManagerDoctorId { get; init; }

    // --- Auth account ---
    /// <summary>Email for the new auth account.</summary>
    public required string Email { get; init; }
    /// <summary>Initial password for the new auth account.</summary>
    public required string Password { get; init; }
    /// <summary>Phone number for the new auth account.</summary>
    public required string Phone { get; init; }
    /// <summary>Role assigned to the new auth account.</summary>
    public required RoleType Role { get; init; }

    // --- Doctor profile ---
    /// <summary>Doctor's first name.</summary>
    public required string FirstName { get; init; }
    /// <summary>Doctor's last name.</summary>
    public required string LastName { get; init; }
    /// <summary>Degrees held by the doctor.</summary>
    public required List<DegreeType> Degrees { get; init; }
    /// <summary>Academic title of the doctor.</summary>
    public required AcademicTitleEnum AcademicTitle { get; init; }
    /// <summary>Position of the doctor.</summary>
    public required PositionType Position { get; init; }
    /// <summary>Gender of the doctor.</summary>
    public GenderType Gender { get; init; }
    /// <summary>Citizen identification number of the doctor.</summary>
    public required string CitizenIdentificationNumber { get; init; }
    /// <summary>Date of birth of the doctor, if provided.</summary>
    public DateTimeOffset? DateOfBirth { get; init; }
    /// <summary>Address of the doctor.</summary>
    public required string Address { get; init; }

    // --- Avatar (pre-uploaded via the Media upload endpoint) ---
    /// <summary>Id of the avatar media (pre-uploaded to the Media service) to link.</summary>
    public required Guid MediaId { get; init; }
}
