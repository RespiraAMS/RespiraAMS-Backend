using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolHandler(
        IDbContext context,
        IUpdateMapper<TreatmentProtocol, UpdateTreatmentProtocolCommand> mapper,
        ILogger<UpdateTreatmentProtocolHandler> logger)
        : ICommandHandler<UpdateTreatmentProtocolCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateTreatmentProtocolCommand protocolCommand, CancellationToken cancellationToken = default)
        {
            // Check if treatment exists in database
            var treatment = await context.TreatmentProtocols
                .Include(x => x.Pathogens)
                .Include(x => x.Criteria)
                .Include(x => x.Treatments)
                .FirstOrDefaultAsync(x => x.Id == protocolCommand.Id, cancellationToken);
            if (treatment is null)
            {
                logger.LogDebug("TreatmentProtocol with ID {id} does not exist in database", protocolCommand.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "TreatmentProtocol not found"));
            }

            // Check if antibiotics, pathogens and criteria exist in database
            var antibioticIds = protocolCommand.Medicines.SelectMany(x => x).Distinct().ToList();
            var antibioticDbCount = await context.Antibiotics.CountAsync(x => antibioticIds.Contains(x.Id), cancellationToken);
            if (antibioticDbCount != antibioticIds.Count)
            {
                logger.LogDebug("Not all antibiotic IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = antibioticIds.Count,
                    TotalFoundInDb = antibioticDbCount,
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all antibiotic IDs given exist"));
            }

            var pathogenIds = protocolCommand.Pathogens.Distinct().ToList();
            var pathogenDbCount = await context.Pathogens.CountAsync(x => pathogenIds.Contains(x.Id), cancellationToken);
            if (pathogenDbCount != protocolCommand.Pathogens.Count)
            {
                logger.LogDebug("Not all pathogen IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = protocolCommand.Pathogens.Count,
                    TotalFoundInDb = pathogenDbCount,
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
            }

            var criteriaIds = protocolCommand.Criteria.Distinct().ToList();
            var criteriaDbCount = await context.Criteria.CountAsync(x => criteriaIds.Contains(x.Id), cancellationToken);
            if (criteriaDbCount != protocolCommand.Criteria.Count)
            {
                logger.LogDebug("Not all criteria IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = protocolCommand.Criteria.Count,
                    TotalFoundInDb = criteriaDbCount,
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Map from protocolCommand to entities
            var mapResult = mapper.MapModel(treatment, protocolCommand);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map protocolCommand to model: {ApplicationError}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }

            // Clean all the join tables data
            treatment.Treatments.Clear();
            treatment.Pathogens.Clear();
            treatment.Criteria.Clear();

            context.UpdateRelations(treatment.Pathogens, protocolCommand.Pathogens);
            context.UpdateRelations(treatment.Criteria, protocolCommand.Criteria);

            // Adding medicine compositions from a MedicalComposition entity
            var compositions = protocolCommand.Medicines.ConvertAll(m =>
            {
                var composition = new Treatment { TreatmentProtocolId = treatment.Id };
                context.UpdateRelations(composition.Combination, m);
                return composition;
            });
            await context.Treatments.AddRangeAsync(compositions, cancellationToken);

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
