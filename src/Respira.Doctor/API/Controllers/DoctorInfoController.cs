using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Respira.Doctor.API.Clients;
using Respira.Doctor.API.Dtos;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Doctor.API.Controllers;

/// <summary>
/// Admin endpoints for viewing doctor information across services.
/// </summary>
[ApiController]
[Route("api/v1/doctors")]
[Authorize(Roles = "Admin,Manager")]
public class DoctorInfoController(
    Application.Abstracts.Data.IDoctorDbContext dbContext,
    AuthClient authClient,
    MediaClient mediaClient
) : ControllerBase
{
    /// <summary>
    /// Gets complete doctor information by ID, combining data from Auth, Doctor, and Media services.
    /// Admin/Manager only.
    /// </summary>
    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<ApiResponse<DoctorFullInfoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (doctor is null)
        {
            return NotFound(ApiResponse.Fail("Doctor not found", StatusCodes.Status404NotFound));
        }

        // Get auth info
        var authInfo = await authClient.GetDoctorAsync(doctor.Id, cancellationToken);
        if (authInfo is null)
        {
            return NotFound(ApiResponse.Fail("Doctor account not found", StatusCodes.Status404NotFound));
        }

        // Get avatar info if exists
        MediaAssetInfo? avatarInfo = null;
        if (doctor.MediaId.HasValue)
        {
            avatarInfo = await mediaClient.GetMediaAsync(doctor.MediaId.Value, cancellationToken);
        }

        var dto = new DoctorFullInfoDto
        {
            // Doctor profile
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            FullName = $"{doctor.FirstName} {doctor.LastName}",
            Degrees = doctor.Degrees.Select(d => d.ToString()).ToList(),
            AcademicTitle = doctor.AcademicTitle.ToString(),
            Position = doctor.Position.ToString(),
            Gender = doctor.Gender.ToString(),
            CitizenIdentificationNumber = doctor.CitizenIdentificationNumber,
            DateOfBirth = doctor.DateOfBirth,
            Address = doctor.Address,

            // Auth info
            Email = authInfo.Email,
            Phone = authInfo.Phone,
            Role = authInfo.Role,
            IsEmailConfirmed = authInfo.IsEmailConfirmed,
            Status = authInfo.Status,
            CreatedAt = authInfo.CreatedAt,

            // Avatar info
            AvatarUrl = avatarInfo?.Url,
            AvatarFileName = avatarInfo?.FileName,
        };

        return Ok(ApiResponse<DoctorFullInfoDto>.Ok(dto));
    }

    /// <summary>
    /// Gets a paginated list of doctors with summary information.
    /// Admin/Manager only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<Pagination<DoctorSummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Doctors.AsNoTracking().AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.FirstName.Contains(search) ||
                d.LastName.Contains(search) ||
                d.CitizenIdentificationNumber.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);

        var doctors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(d => new
            {
                d.Id,
                d.FirstName,
                d.LastName,
                d.Position,
                d.AcademicTitle,
                d.MediaId,
                d.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        // Get auth info and avatar for each doctor in parallel
        var doctorIds = doctors.Select(d => d.Id).ToList();
        var authTasks = doctorIds.Select(id => authClient.GetDoctorAsync(id, cancellationToken));
        var authResults = await Task.WhenAll(authTasks);
        var authMap = authResults.Where(a => a != null).ToDictionary(a => a!.Id);

        var mediaIds = doctors.Where(d => d.MediaId.HasValue).Select(d => d.MediaId!.Value).Distinct().ToList();
        var mediaTasks = mediaIds.Select(id => mediaClient.GetMediaAsync(id, cancellationToken));
        var mediaResults = await Task.WhenAll(mediaTasks);
        var mediaMap = mediaResults.Where(m => m != null).ToDictionary(m => m!.Id);

        var items = doctors.Select(d => new DoctorSummaryDto
        {
            Id = d.Id,
            FullName = $"{d.FirstName} {d.LastName}",
            Email = authMap.GetValueOrDefault(d.Id)?.Email ?? "N/A",
            Phone = authMap.GetValueOrDefault(d.Id)?.Phone ?? "N/A",
            Position = d.Position.ToString(),
            AcademicTitle = d.AcademicTitle.ToString(),
            Status = authMap.GetValueOrDefault(d.Id)?.Status ?? "Unknown",
            AvatarUrl = d.MediaId.HasValue ? mediaMap.GetValueOrDefault(d.MediaId.Value)?.Url : null,
            CreatedAt = d.CreatedAt,
        }).ToList();

        var metadata = new PaginationMetadata
        {
            CurrentPage = page,
            PageSize = size,
            TotalItemCount = total,
            PageCount = (int)Math.Ceiling(total / (double)size),
            HasNextPage = page < (int)Math.Ceiling(total / (double)size),
            HasPreviousPage = page > 1,
        };

        var result = new Pagination<DoctorSummaryDto>(metadata, items);

        return Ok(ApiResponse<Pagination<DoctorSummaryDto>>.Ok(result));
    }
}
