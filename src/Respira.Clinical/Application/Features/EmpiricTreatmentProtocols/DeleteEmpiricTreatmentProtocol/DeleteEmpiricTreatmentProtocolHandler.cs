using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolHandler(
    IDbContext context,
    ILogger<DeleteEmpiricTreatmentProtocolHandler> logger)
    : ICommandHandler<DeleteEmpiricTreatmentProtocolCommand, Respira.ServiceDefaults.Contracts.Results.Result>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result> HandleAsync(DeleteEmpiricTreatmentProtocolCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var protocol = await context.EmpiricTreatmentProtocols
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (protocol is null)
        {
            logger.LogDebug("Empiric treatment protocol with id {id} not found", command.Id);
            return Respira.ServiceDefaults.Contracts.Results.Result.Failure(new Error(Status.BadRequest, "Empiric treatment protocol with id {id} not found"));
            // throw new NotFoundException(nameof(EmpiricTreatmentProtocol), command.Id);
        }

        // Delete protocol
        protocol.IsDeleted = true;
        protocol.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        await context.SaveChangesAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result.Success(Status.Deleted);
    }
}
