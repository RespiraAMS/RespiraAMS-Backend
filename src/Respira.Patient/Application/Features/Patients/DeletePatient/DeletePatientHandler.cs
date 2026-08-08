using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Patients.DeletePatient;

public class DeletePatientHandler(IDbContext context, ILogger<DeletePatientHandler> logger)
    : ICommandHandler<DeletePatientCommand>
{
    public async Task HandleAsync(DeletePatientCommand command, CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var patient = await context.Patients
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (patient is null)
        {
            logger.LogDebug("Patient with this ID not found: {Id}", command.Id);
            throw new NotFoundException(nameof(Patient), command.Id);
        }

        // Check if patient has any treatment
        if (!await context.Treatments.AnyAsync(x => x.PatientId == patient.Id, cancellationToken))
        {
            logger.LogDebug("Patient {Id} has no treatments, cannot discharged", command.Id);
            throw new BadRequestException("Cannot discharge patient that hasn't received any treatment");
        }

        // Delete patient
        patient.IsDeleted = true;
        patient.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete patient");
            throw new ServerException();
        }
    }
}