using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthDoctorController(IAuthDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Gets an auth doctor account by ID (internal use by other services).
    /// </summary>
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

public record AuthDoctorDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Role { get; init; }
    public bool IsEmailConfirmed { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
