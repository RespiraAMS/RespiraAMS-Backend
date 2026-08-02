using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;

public class CreateEmpiricTreatmentProtocolHandler(
    IDbContext context,
    ICreateMapper<EmpiricTreatmentProtocol, CreateEmpiricTreatmentProtocolCommand> mapper,
    ILogger<CreateEmpiricTreatmentProtocolHandler> logger)
    : ICommandHandler<CreateEmpiricTreatmentProtocolCommand, CreateEmpiricTreatmentProtocolResult>
{
    public async Task<CreateEmpiricTreatmentProtocolResult> HandleAsync(CreateEmpiricTreatmentProtocolCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if disease ID exists
        var disease = await context.Diseases
            .FirstOrDefaultAsync(x => x.Id == command.DiseaseId, cancellationToken);
        if (disease is null)
        {
            logger.LogDebug("Disease ID not found: {Id}", command.DiseaseId);
            throw new BadRequestException("Disease ID not exists");
        }

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

        // Map from command to entity
        var protocol = mapper.ToModel(command);

        // Add stub to the IDs list
        context.UpdateRelations(protocol.Medicines, command.MedicineIds);
        context.UpdateRelations(protocol.OtherCriteria, command.OtherCriteriaIds);

        // Save changes to database
        await context.EmpiricTreatmentProtocols.AddAsync(protocol, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) <= 0)
        {
            logger.LogError("Failed to save empiric treatment protocol");
            throw new ServerException();
        }

        return new CreateEmpiricTreatmentProtocolResult(protocol.Id);
    }
}