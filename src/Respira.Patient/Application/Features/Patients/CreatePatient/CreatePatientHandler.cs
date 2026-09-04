using Application.Contracts.Data;

namespace Application.Features.Patients.CreatePatient;

public class CreatePatientHandler(IDbContext context, ICreateMapper<Patient, CreatePatientCommand> mapper)
    : ICommandHandler<CreatePatientCommand, Result<CreatePatientResult>>
{
    public async Task<Result<CreatePatientResult>> HandleAsync(CreatePatientCommand command, CancellationToken cancellationToken = default)
    {
        // Map command to model
        var patient = mapper.ToModel(command);

        // Save changes to database
        await context.Patients.AddAsync(patient, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreatePatientResult>.Success(Status.Created, new CreatePatientResult(patient.Id));
    }
}
