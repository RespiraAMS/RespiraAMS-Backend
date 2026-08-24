using Application.Abstracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Exceptions;
using Respira.ServiceDefaults.Messages;

namespace Application.Features.Authentication.GetUser.Queries
{
    public class GetUserQueryHandler(IAuthDbContext dbContext, ILogger<GetUserQueryHandler> logger)
        : IQueryHandler<GetUserQuery, GetAuthDoctorResult>
    {
        public async Task<GetAuthDoctorResult> HandleAsync(
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
                    throw new NotFoundException("Doctor", query.Id);
                }
                else
                {
                    return new GetAuthDoctorResult
                    {
                        Email = doctor.Email,
                        Phone = doctor.Phone,
                        Role = doctor.Role.ToString(),
                        IsEmailConfirmed = doctor.IsEmailConfirmed,
                        Status = doctor.Status.ToString(),
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving doctor");
                throw new ServerException(ex);
            }
        }
    }
}
