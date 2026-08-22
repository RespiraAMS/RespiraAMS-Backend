using Application.Contracts.Data;
using Microsoft.Extensions.Logging;

namespace Application.Features.Patients.CreatePatient;

public class CreatePatientHandler(
    IDbContext context,
    ICreateMapper<Patient, CreatePatientCommand> mapper,
    ILogger<CreatePatientHandler> logger) : ICommandHandler<CreatePatientCommand, CreatePatientResult>
{
    public async Task<CreatePatientResult> HandleAsync(CreatePatientCommand command,
        CancellationToken cancellationToken = default)
    {
        // Map command to model
        var patient = mapper.ToModel(command);

        // Save changes to database
        await context.Patients.AddAsync(patient, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save patient into database");
            throw new ServerException();
        }

        return new CreatePatientResult(patient.Id);
    }
}