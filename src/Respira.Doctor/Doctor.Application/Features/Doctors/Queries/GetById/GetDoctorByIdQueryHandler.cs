using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetById
{
    public class GetDoctorByIdQueryHandler(
        IDoctorDbContext dbContext,
        ILogger<GetDoctorByIdQueryHandler> logger
    ) : IQueryHandler<GetDoctorByIdQuery, Result<DoctorResult>>
    {
        public async Task<Result<DoctorResult>> HandleAsync(
            GetDoctorByIdQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            BaseDoctor? profile = await dbContext
                .Doctors.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == query.DoctorId, cancellationToken);
            profile ??= await dbContext
                .Managers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == query.DoctorId, cancellationToken);

            if (profile is null)
            {
                logger.LogDebug("Doctor profile {DoctorId} not found", query.DoctorId);

                return Result<DoctorResult>.Failure(
                    new ApplicationError(ApplicationStatus.ResourceNotFound, "Doctor not found")
                );
            }

            var role = profile is Manager ? ProfileRole.Manager : ProfileRole.Doctor;

            var result = new DoctorResult
            {
                Id = profile.Id,
                AccountId = profile.AccountId,
                Role = role,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Degrees = profile.Degrees.ToList(),
                AcademicTitle = profile.AcademicTitle,
                Position = profile.Position,
                Gender = profile.Gender,
                CitizenIdentificationNumber = profile.CitizenIdentificationNumber,
                DateOfBirth = profile.DateOfBirth,
                Address = profile.Address,
                MediaId = profile.MediaId,
                CreatedAt = profile.CreatedAt,
            };

            return Result<DoctorResult>.Success(ApplicationStatus.Success, result);
        }
    }
}
