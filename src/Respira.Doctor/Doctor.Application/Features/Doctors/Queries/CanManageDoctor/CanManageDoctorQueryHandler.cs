using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Constracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Doctors;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Doctor.Application.Features.Doctors.Queries.CanManageDoctor
{
    public sealed class CanManageDoctorQueryHandler(IDoctorDbContext dbContext)
        : IQueryHandler<CanManageDoctorQuery, Result<DoctorManagementAuthorization>>
    {
        public async Task<Result<DoctorManagementAuthorization>> HandleAsync(
            CanManageDoctorQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var doctor = await dbContext.Doctors.FirstOrDefaultAsync(
                item => item.Id == query.DoctorId && item.AccountId == query.TargetAccountId,
                cancellationToken
            );

            if (doctor is not null)
            {
                var allowed = query.ActorRole switch
                {
                    "Admin" => true,
                    "Doctor" => doctor.AccountId == query.ActorAccountId,
                    "HeadDepartmentDoctor" => await IsManagedByHeadAsync(
                        doctor,
                        query.ActorAccountId,
                        cancellationToken
                    ),
                    _ => false,
                };

                return Result<DoctorManagementAuthorization>.Success(
                    ApplicationStatus.Success,
                    new DoctorManagementAuthorization { Allowed = allowed, MediaId = doctor.MediaId }
                );
            }

            var headDepartmentDoctor = await dbContext.HeadDepartmentDoctors.FirstOrDefaultAsync(
                item => item.Id == query.DoctorId && item.AccountId == query.TargetAccountId,
                cancellationToken
            );

            return Result<DoctorManagementAuthorization>.Success(
                ApplicationStatus.Success,
                new DoctorManagementAuthorization
                {
                    Allowed = headDepartmentDoctor is not null
                        && (query.ActorRole == "Admin"
                            || query.ActorRole == "HeadDepartmentDoctor"
                                && headDepartmentDoctor.AccountId == query.ActorAccountId),
                    MediaId = headDepartmentDoctor?.MediaId,
                }
            );
        }

        private async Task<bool> IsManagedByHeadAsync(
            Respira.Doctor.Domain.Entities.Doctor doctor,
            Guid actorAccountId,
            CancellationToken cancellationToken
        )
        {
            var headId = await dbContext.HeadDepartmentDoctors
                .Where(item => item.AccountId == actorAccountId)
                .Select(item => (Guid?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return headId.HasValue && doctor.HeadDepartmentDoctorId == headId.Value;
        }
    }
}
