using Application.Abstracts.Data;
using Application.Contracts.Messages;
using Application.Features.Doctors.Get.Results;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;
using Wolverine;

namespace Application.Features.Doctors.Get.Queries
{
    public class DoctorQueryHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus
    ) : IQueryHandler<DoctorQuery, ApiResponse<DoctorQueryResult>>
    {
        public async Task<ApiResponse<DoctorQueryResult>> HandleAsync(
            DoctorQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var authDoctor = await bus.InvokeAsync<ApiResponse<GetAuthDoctorResult>>(
                new GetUserQuery { Id = query.Id },
                cancellationToken
            );
            if (!authDoctor.Success)
            {
                return ApiResponse<DoctorQueryResult>.Fail(
                    authDoctor.Message ?? "Auth service error",
                    authDoctor.StatusCode
                );
            }

            var doctor =
                await dbContext
                    .Doctors.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
                ?? throw new NotFoundException("Doctor", query.Id);

            var result = new DoctorQueryResult
            {
                Email = authDoctor.Data!.Email,
                Phone = authDoctor.Data.Phone,
                Role = authDoctor.Data.Role,
                IsEmailConfirmed = authDoctor.Data.IsEmailConfirmed,
                Status = authDoctor.Data.Status,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Degrees = [.. doctor.Degrees.Select(d => d.ToString())],
                AcademicTitle = doctor.AcademicTitle.ToString(),
                Position = doctor.Position,
                Gender = doctor.Gender.ToString(),
                CitizenIdentificationNumber = doctor.CitizenIdentificationNumber,
                DateOfBirth = doctor.DateOfBirth,
                Address = doctor.Address,
                MediaId = doctor.MediaId,
                Patients = doctor.Patients?.ToList(),
                Subordinates = doctor.Subordinates?.Select(s => s.Id).ToList(),
            };

            if (doctor.MediaId.HasValue)
            {
                var media = await bus.InvokeAsync<ApiResponse<GetMediaResult>>(
                    new GetMediaQuery { Id = doctor.MediaId.Value },
                    cancellationToken
                );
                if (!media.Success)
                {
                    return ApiResponse<DoctorQueryResult>.Fail(
                        media.Message ?? "Media service error",
                        media.StatusCode
                    );
                }

                result.Url = media.Data?.Url;
            }

            return ApiResponse<DoctorQueryResult>.Ok(result);
        }
    }
}
