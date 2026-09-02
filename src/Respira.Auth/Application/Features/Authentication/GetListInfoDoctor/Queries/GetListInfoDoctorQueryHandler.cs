using Application.Abstracts.Data;
using Application.Features.Authentication.GetListInfoDoctor.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Application.Features.Authentication.GetListInfoDoctor.Queries;

/// <summary>
/// Returns the auth-side details for the requested set of doctor ids. Missing ids
/// are silently skipped (a doctor profile may exist without an auth account).
/// </summary>
/// <param name="dbContext">Auth database context.</param>
/// <param name="logger">Logger.</param>
public class GetListInfoDoctorQueryHandler(
    IAuthDbContext dbContext,
    ILogger<GetListInfoDoctorQueryHandler> logger
) : IQueryHandler<GetListInfoDoctorQuery, Result<IEnumerable<GetAuthDoctorListResult>>>
{
    /// <summary>
    /// Loads the requested auth doctors and projects them to the list result.
    /// </summary>
    /// <param name="query">Query holding the doctor ids.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth details for the found doctors.</returns>
    public async Task<Result<IEnumerable<GetAuthDoctorListResult>>> HandleAsync(
        GetListInfoDoctorQuery query,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (query.Ids.Count == 0)
            {
                return Result<IEnumerable<GetAuthDoctorListResult>>.Ok(
                    new List<GetAuthDoctorListResult>()
                );
            }

            var idSet = query.Ids.ToHashSet();
            var doctors = await dbContext
                .AuthDoctors.AsNoTracking()
                .Where(x => idSet.Contains(x.Id))
                .ToListAsync(cancellationToken);

            var result = doctors.Select(
                d =>
                    new GetAuthDoctorListResult
                    {
                        Id = d.Id,
                        Email = d.Email,
                        Phone = d.Phone,
                        Role = d.Role.ToString(),
                        IsEmailConfirmed = d.IsEmailConfirmed,
                        Status = d.Status.ToString(),
                    }
            );

            return Result<IEnumerable<GetAuthDoctorListResult>>.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving doctor list info");
            return Result<IEnumerable<GetAuthDoctorListResult>>.Fail(
                "Error retrieving doctor list info"
            );
        }
    }
}
