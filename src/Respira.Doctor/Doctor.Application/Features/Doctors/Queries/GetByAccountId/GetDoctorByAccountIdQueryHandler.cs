using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Accounts;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetByAccountId
{
    public class GetDoctorByAccountIdQueryHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<GetDoctorByAccountIdQueryHandler> logger
    ) : IQueryHandler<GetDoctorByAccountIdQuery, Result<DoctorResult>>
    {
        public async Task<Result<DoctorResult>> HandleAsync(
            GetDoctorByAccountIdQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            if (query.AccountId == Guid.Empty)
            {
                return Result<DoctorResult>.Failure(
                    new ApplicationError(
                        ApplicationStatus.BadRequest,
                        "Account identifier is required"
                    )
                );
            }

            BaseDoctor? profile = await dbContext
                .Doctors.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == query.AccountId, cancellationToken);
            profile ??= await dbContext
                .HeadDepartmentDoctors.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == query.AccountId, cancellationToken);

            if (profile is null)
            {
                logger.LogDebug(
                    "Doctor profile for account {AccountId} not found",
                    query.AccountId
                );

                return Result<DoctorResult>.Failure(
                    new ApplicationError(
                        ApplicationStatus.ResourceNotFound,
                        "Doctor profile not found"
                    )
                );
            }

            var role =
                profile is HeadDepartmentDoctor
                    ? ProfileRole.HeadDepartmentDoctor
                    : ProfileRole.Doctor;

            var result = new DoctorResult
            {
                Id = profile.Id,
                AccountId = profile.AccountId,
                Role = role,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Degrees = [.. profile.Degrees],
                AcademicTitle = profile.AcademicTitle,
                Position = profile.Position,
                Gender = profile.Gender,
                CitizenIdentificationNumber = profile.CitizenIdentificationNumber,
                DateOfBirth = profile.DateOfBirth,
                Address = profile.Address,
                MediaId = profile.MediaId,
                CreatedAt = profile.CreatedAt,
            };

            await EnrichAccountAsync(result, cancellationToken);

            return Result<DoctorResult>.Success(ApplicationStatus.Success, result);
        }

        private async Task EnrichAccountAsync(
            DoctorResult result,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var response = await bus.InvokeAsync<Result<List<AccountSummary>>>(
                    new GetAccountsByIdsQuery { AccountIds = [result.AccountId] },
                    cancellationToken
                );

                if (response.IsSuccess() && response.Data is not null)
                {
                    var account = response.Data.FirstOrDefault(x => x.Id == result.AccountId);
                    if (account is not null)
                    {
                        result.Phone = account.Phone;
                        result.Email = account.Email;
                    }
                }
                else
                {
                    logger.LogWarning(
                        "Account lookup for doctor account {AccountId} failed with status {StatusCode}",
                        result.AccountId,
                        response.StatusCode
                    );
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Account lookup for doctor account {AccountId} failed; returning profile without account data",
                    result.AccountId
                );
            }
        }
    }
}
