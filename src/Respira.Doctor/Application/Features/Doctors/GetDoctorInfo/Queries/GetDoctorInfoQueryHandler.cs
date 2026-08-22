using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.GetDoctorInfo.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Application.Features.Doctors.GetDoctorInfo.Queries;

/// <summary>
/// Returns a doctor's profile information, using a cache-aside strategy:
/// reads from the cache first and falls back to the database, then repopulates the cache.
/// </summary>
/// <param name="dbContext">Doctor database context</param>
/// <param name="cacheService">Cache for doctor info (by id)</param>
/// <param name="logger">Logger</param>
public class GetDoctorInfoQueryHandler(
    IDoctorDbContext dbContext,
    ICacheService cacheService,
    ILogger<GetDoctorInfoQueryHandler> logger
) : IQueryHandler<GetDoctorInfoQuery, ApiResponse<GetDoctorInfoResult>>
{
    private const string CacheKeyPrefix = "doctor:info";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Handles the query: reads from cache, otherwise loads the doctor from the database.
    /// </summary>
    /// <param name="query">Query containing the doctor id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API response with the doctor info, or a 404 failure if not found</returns>
    public async Task<ApiResponse<GetDoctorInfoResult>> HandleAsync(
        GetDoctorInfoQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = $"{CacheKeyPrefix}:{query.DoctorId}";

        var cached = await cacheService.GetAsync<GetDoctorInfoResult>(cacheKey);
        if (cached is not null)
        {
            return ApiResponse<GetDoctorInfoResult>.Ok(cached);
        }

        var doctor = await dbContext
            .Doctors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.DoctorId, cancellationToken);

        if (doctor is null)
        {
            logger.LogDebug("Doctor {DoctorId} not found", query.DoctorId);
            return ApiResponse<GetDoctorInfoResult>.Fail(
                message: "Doctor not found",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        var result = Map(doctor);
        await cacheService.SetAsync(cacheKey, result, CacheTtl);

        return ApiResponse<GetDoctorInfoResult>.Ok(result);
    }

    /// <summary>
    /// Maps a <see cref="Domain.Entities.Doctor"/> entity to its result representation.
    /// </summary>
    /// <param name="doctor">Doctor entity</param>
    /// <returns>The result representation</returns>
    private static GetDoctorInfoResult Map(Domain.Entities.Doctor doctor) =>
        new()
        {
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Degrees = [.. doctor.Degrees],
            AcademicTitle = doctor.AcademicTitle,
            Position = doctor.Position,
            Gender = doctor.Gender,
            DateOfBirth = doctor.DateOfBirth,
            Address = doctor.Address,
            MediaId = doctor.MediaId,
        };
}
