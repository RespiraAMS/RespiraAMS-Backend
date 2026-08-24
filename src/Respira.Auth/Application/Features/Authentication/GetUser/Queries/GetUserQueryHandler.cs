using Application.Abstracts.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Application.Features.Authentication.GetUser.Queries
{
    public class GetUserQueryHandler(IAuthDbContext dbContext, ILogger<GetUserQueryHandler> logger)
        : IQueryHandler<GetUserQuery, ApiResponse<GetAuthDoctorResult>>
    {
        public async Task<ApiResponse<GetAuthDoctorResult>> HandleAsync(
            GetUserQuery query,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var doctor = await dbContext
                    .AuthDoctors.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
                if (doctor is null)
                {
                    return ApiResponse<GetAuthDoctorResult>.Fail(
                        "Doctor not found",
                        StatusCodes.Status404NotFound
                    );
                }
                else
                {
                    return ApiResponse<GetAuthDoctorResult>.Ok(
                        new GetAuthDoctorResult
                        {
                            Email = doctor.Email,
                            Phone = doctor.Phone,
                            Role = doctor.Role.ToString(),
                            IsEmailConfirmed = doctor.IsEmailConfirmed,
                            Status = doctor.Status.ToString(),
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving doctor");
                return ApiResponse<GetAuthDoctorResult>.Fail("Error retrieving doctor");
            }
        }
    }
}
