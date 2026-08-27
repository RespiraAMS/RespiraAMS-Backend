using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;

public class UpdateEmpiricTreatmentProtocolHandler(
    IDbContext context,
    IUpdateMapper<EmpiricTreatmentProtocol, UpdateEmpiricTreatmentProtocolCommand> mapper,
    ILogger<UpdateEmpiricTreatmentProtocolHandler> logger)
    : ICommandHandler<UpdateEmpiricTreatmentProtocolCommand>
{
    public async Task HandleAsync(UpdateEmpiricTreatmentProtocolCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if pathogen ID exists
        if (command.SpecialInfectionId is not null && await context.Pathogens
                .FirstOrDefaultAsync(x => x.Id == command.SpecialInfectionId, cancellationToken) is null)
        {
            logger.LogDebug("Pathogen ID not found: {Id}", command.SpecialInfectionId);
            throw new BadRequestException("Pathogen ID (SpecialInfectionId) not exists");
        }

        // Check if all criteria IDs exist
        if (await context.Criteria.CountAsync(x => command.OtherCriteriaIds.Contains(x.Id), cancellationToken) !=
            command.OtherCriteriaIds.Count)
        {
            logger.LogDebug("Not all criterion ids exists");
            throw new BadRequestException("Not all criterion IDs exists");
        }

        // Check if all antibiotic IDs exist
        if (await context.Antibiotics.CountAsync(x => command.MedicineIds.Contains(x.Id), cancellationToken) !=
            command.MedicineIds.Count)
        {
            logger.LogWarning("Not all antibiotic ids exists");
            throw new BadRequestException("Not all medicine ids exists");
        }

        // Get entity by ID
        var protocol = await context.EmpiricTreatmentProtocols
            .Include(x => x.Medicines)
            .Include(x => x.OtherCriteria)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (protocol is null)
        {
            logger.LogDebug("Empiric treatment protocol not found: {Id}", command.Id);
            throw new NotFoundException(nameof(EmpiricTreatmentProtocol), command.Id);
        }

        // Map from command to entity
        mapper.MapModel(protocol, command);

        // Add stub to the IDs list
        context.UpdateRelations(protocol.Medicines, command.MedicineIds);
        context.UpdateRelations(protocol.OtherCriteria, command.OtherCriteriaIds);

        // Save changes to database
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save empiric treatment protocol");
            throw new ServerException();
        }
    }
}
