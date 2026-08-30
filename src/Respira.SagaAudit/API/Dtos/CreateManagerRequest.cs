using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.API.Dtos;

/// <summary>HTTP request body for creating a manager on behalf of an admin.</summary>
public record CreateManagerRequest
{
    /// <summary>Id of the admin that initiated the creation.</summary>
    public required Guid ManagerDoctorId { get; init; }
    /// <summary>Email for the new auth account.</summary>
    public required string Email { get; init; }
    /// <summary>Initial password for the new auth account.</summary>
    public required string Password { get; init; }
    /// <summary>Phone number for the new auth account.</summary>
    public required string Phone { get; init; }

    /// <summary>Manager's first name.</summary>
    public required string FirstName { get; init; }
    /// <summary>Manager's last name.</summary>
    public required string LastName { get; init; }
    /// <summary>Degrees held by the manager.</summary>
    public required List<DegreeType> Degrees { get; init; }
    /// <summary>Academic title of the manager.</summary>
    public required AcademicTitleEnum AcademicTitle { get; init; }
    /// <summary>Position of the manager.</summary>
    public required PositionType Position { get; init; }
    /// <summary>Gender of the manager.</summary>
    public GenderType Gender { get; init; }
    /// <summary>Citizen identification number of the manager.</summary>
    public required string CitizenIdentificationNumber { get; init; }
    /// <summary>Date of birth of the manager, if provided.</summary>
    public DateTimeOffset? DateOfBirth { get; init; }
    /// <summary>Address of the manager.</summary>
    public required string Address { get; init; }
}
