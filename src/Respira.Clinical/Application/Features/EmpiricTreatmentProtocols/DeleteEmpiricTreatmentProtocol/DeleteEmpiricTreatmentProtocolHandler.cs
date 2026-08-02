using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolHandler(
    IDbContext context,
    ILogger<DeleteEmpiricTreatmentProtocolHandler> logger)
    : ICommandHandler<DeleteEmpiricTreatmentProtocolCommand>
{
    public async Task HandleAsync(DeleteEmpiricTreatmentProtocolCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get entity by ID
        var protocol = await context.EmpiricTreatmentProtocols
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (protocol is null)
        {
            logger.LogDebug("Empiric treatment protocol with id {id} not found", command.Id);
            throw new NotFoundException(nameof(EmpiricTreatmentProtocol), command.Id);
        }

        // Delete protocol
        protocol.IsDeleted = true;
        protocol.DeletedAt = DateTimeOffset.UtcNow;

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to delete empiric treatment protocol");
            throw new ServerException();
        }
    }
}