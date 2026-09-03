using Application.Contracts.Data;

namespace Application.Features.Pathogens.CreatePathogen;

public class CreatePathogenHandler(IDbContext context, ICreateMapper<Pathogen, CreatePathogenCommand> mapper)
    : ICommandHandler<CreatePathogenCommand, Result<CreatePathogenResult>>
{
    public async Task<Result<CreatePathogenResult>> HandleAsync(CreatePathogenCommand command,
        CancellationToken cancellationToken = default)
    {
        // Map command to model
        var pathogen = mapper.ToModel(command);

        // Save changes to database
        await context.Pathogens.AddAsync(pathogen, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);


        return Result<CreatePathogenResult>.Success(Status.Created, new CreatePathogenResult(pathogen.Id));
    }
}
