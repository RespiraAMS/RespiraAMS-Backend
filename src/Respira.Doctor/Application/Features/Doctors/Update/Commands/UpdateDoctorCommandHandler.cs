using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.Update.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.Update.Commands
{
    public class UpdateDoctorCommandHandler(
        ILogger<UpdateDoctorCommand> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<UpdateDoctorCommand>
    {
        private const string CacheKeyPrefix = "doctor:info";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

        public async Task HandleAsync(
            UpdateDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var doctor = await dbContext.Doctors.FirstOrDefaultAsync(
                    x => x.Id == command.DoctorId,
                    cancellationToken
                );
                if (doctor is null)
                {
                    logger.LogWarning($"Doctor {command.DoctorId} not found for update");
                    await bus.PublishAsync(
                        new UpdateDoctorFailureEvent
                        {
                            SagaId = command.SagaId,
                            DoctorId = command.DoctorId,
                        }
                    );
                    return;
                }

                doctor.FirstName = command.FirstName;
                doctor.LastName = command.LastName;
                doctor.Degrees = command.Degrees;
                doctor.AcademicTitle = command.AcademicTitle;
                doctor.Position = command.Position;
                doctor.Gender = command.Gender;
                doctor.CitizenIdentificationNumber = command.CitizenIdentificationNumber;
                doctor.DateOfBirth = command.DateOfBirth?.ToUniversalTime();
                doctor.Address = command.Address;
                doctor.MediaId = command.MediaId;

                if (command.DoctorCreatorId is not null)
                {
                    var doctorCreator = await dbContext.Doctors
                        .Include(x => x.Subordinates)
                        .FirstOrDefaultAsync(
                            x => x.Id == command.DoctorCreatorId,
                            cancellationToken
                        );
                    if (
                        doctorCreator is not null
                        && doctorCreator.Subordinates?.Any(s => s.Id == doctor.Id) == false
                    )
                    {
                        doctorCreator.Subordinates.Add(doctor);
                        await cacheService.RemoveAsync(CacheKeyPrefix + doctorCreator.Id);
                        await cacheService.SetAsync(
                            CacheKeyPrefix + doctorCreator.Id,
                            doctorCreator,
                            CacheTtl
                        );
                    }
                }

                await cacheService.RemoveAsync(CacheKeyPrefix + doctor.Id);
                await cacheService.SetAsync(CacheKeyPrefix + doctor.Id, doctor, CacheTtl);
                await dbContext.SaveChangesAsync();

                await bus.PublishAsync(
                    new UpdateDoctorSuccessEvent()
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                        AcademicTitle = doctor.AcademicTitle,
                        Position = doctor.Position,
                        Gender = doctor.Gender,
                        DateOfBirth = doctor.DateOfBirth,
                        Address = doctor.Address,
                        MediaId = doctor.MediaId,
                        CitizenIdentificationNumber = doctor.CitizenIdentificationNumber,
                        Degrees = (List<Domain.Enums.DegreeType>)doctor.Degrees,
                        LastName = doctor.LastName,
                        FirstName = doctor.FirstName,
                    }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update doctor");
                await bus.PublishAsync(
                    new UpdateDoctorFailureEvent
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
        }
    }
}
