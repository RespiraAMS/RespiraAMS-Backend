using Application.Abstracts.Data;
using Application.Clients;
using Application.Features.Doctors.Get.Results;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.Doctors.Get.Queries
{
    public class DoctorQueryHandler(
        IDoctorDbContext dbContext,
        IAuthClient authClient,
        IMediaClient mediaClient
    ) : IQueryHandler<DoctorQuery, DoctorQueryResult>
    {
        public async Task<DoctorQueryResult> HandleAsync(
            DoctorQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var authDoctor = await authClient.GetDoctorAsync(query.Id, cancellationToken);

            var doctor =
                await dbContext
                    .Doctors.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
                ?? throw new NotFoundException("Doctor", query.Id);

            var result = new DoctorQueryResult
            {
                Email = authDoctor.Email,
                Phone = authDoctor.Phone,
                Role = authDoctor.Role,
                IsEmailConfirmed = authDoctor.IsEmailConfirmed,
                Status = authDoctor.Status,
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
                result.Url = await mediaClient.GetUrlAsync(doctor.MediaId.Value, cancellationToken);
            }

            return result;
        }
    }
}
