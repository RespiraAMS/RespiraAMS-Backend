using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Auth.API.Controllers;

/// <summary>
/// Internal API for managing auth doctor accounts (used by downstream services).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthDoctorController(IAuthDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Retrieves a doctor's auth account by its unique identifier.
    /// Used internally by other microservices to resolve authenticated doctor information.
    /// </summary>
    /// <param name="id">The unique identifier of the auth doctor account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The auth doctor DTO if found, otherwise a 404 response.</returns>
    [HttpGet]
    [Route("doctors/{id:guid}")]
    [ProducesResponseType<ApiResponse<AuthDoctorDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.AuthDoctors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (doctor is null)
        {
            return NotFound(ApiResponse.Fail("Doctor account not found", StatusCodes.Status404NotFound));
        }

        var dto = new AuthDoctorDto
        {
            Id = doctor.Id,
            Email = doctor.Email,
            Phone = doctor.Phone,
            Role = doctor.Role.ToString(),
            IsEmailConfirmed = doctor.IsEmailConfirmed,
            Status = doctor.Status.ToString(),
            CreatedAt = doctor.CreatedAt,
        };

        return Ok(ApiResponse<AuthDoctorDto>.Ok(dto));
    }
}

/// <summary>
/// DTO representing a doctor's authenticated account information.
/// </summary>
public record AuthDoctorDto
{
    /// <summary>Unique identifier of the auth doctor account.</summary>
    public Guid Id { get; init; }

    /// <summary>Email address of the doctor.</summary>
    public required string Email { get; init; }

    /// <summary>Phone number of the doctor.</summary>
    public required string Phone { get; init; }

    /// <summary>Role assigned to the doctor (e.g., Admin, SuperAdmin).</summary>
    public required string Role { get; init; }

    /// <summary>Indicates whether the email has been verified.</summary>
    public bool IsEmailConfirmed { get; init; }

    /// <summary>Current status of the doctor account (Active, Locked, etc.).</summary>
    public required string Status { get; init; }

    /// <summary>Timestamp when the account was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
