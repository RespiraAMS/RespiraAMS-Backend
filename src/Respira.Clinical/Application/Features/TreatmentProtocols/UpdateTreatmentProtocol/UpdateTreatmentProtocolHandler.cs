using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolHandler(
        IDbContext context,
        IDiagnoseService service,
        IUpdateMapper<TreatmentProtocol, UpdateTreatmentProtocolCommand> mapper,
        ILogger<UpdateTreatmentProtocolHandler> logger)
        : ICommandHandler<UpdateTreatmentProtocolCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateTreatmentProtocolCommand command, CancellationToken cancellationToken = default)
        {
            // Check if treatment exists in database
            var protocol = await context.TreatmentProtocols

                .Include(x => x.Pathogens)
                .Include(x => x.Criteria)
                .Include(x => x.Treatments)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (protocol is null)
            {
                logger.LogDebug("TreatmentProtocol with ID {id} does not exist in database", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "TreatmentProtocol not found"));
            }

            // Check if antibiotics, pathogens and criteria exist in database
            var antibioticIds = command.Medicines.SelectMany(x => x).Distinct().ToList();
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

            var pathogenIds = command.Pathogens.Distinct().ToList();
            var pathogenDbCount = await context.Pathogens.CountAsync(x => pathogenIds.Contains(x.Id), cancellationToken);
            if (pathogenDbCount != command.Pathogens.Count)
            {
                logger.LogDebug("Not all pathogen IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = command.Pathogens.Count,
                    TotalFoundInDb = pathogenDbCount,
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
            }

            var criteriaIds = command.Criteria.Distinct().ToList();
            var criteriaDbCount = await context.Criteria.CountAsync(x => criteriaIds.Contains(x.Id), cancellationToken);
            if (criteriaDbCount != command.Criteria.Count)
            {
                logger.LogDebug("Not all criteria IDs given exist in database: {detail}", new
                {
                    TotalIdsGiven = command.Criteria.Count,
                    TotalFoundInDb = criteriaDbCount,
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Map from command to entities
            var mapResult = mapper.MapModel(protocol, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {ApplicationError}", mapResult.Error);
                return Result.Failure(mapResult.Error!);
            }

            // Clean all the join tables data
            protocol.Treatments.Clear();
            protocol.Pathogens.Clear();
            protocol.Criteria.Clear();

            context.UpdateRelations(protocol.Pathogens, command.Pathogens);
            context.UpdateRelations(protocol.Criteria, command.Criteria);

            var clinicalContext = new ClinicalContext
            {
                AntibioticGroups = await context.AntibioticGroups.ToListAsync(cancellationToken),
                Antibiograms = [],
                Antibiotics = await context.Antibiotics.Include(x => x.AntibioticGroup).ToListAsync(cancellationToken),
                Metrics = [],
                Pathogens = [],
                SuspectedCauses = [],
                TreatmentProtocols = [],
                Variables = [],
            };

            // Adding treatments from a Treatment entity
            var treatments = command.Medicines.ConvertAll(combination =>
            {
                // Because we need a full entity treatment to use in the service,
                // we won't use the stub attach method, but building the list of
                // antibiotics manually using the clinical context
                var antibiotics = clinicalContext.Antibiotics
                    .Where(x => combination.Contains(x.Id))
                    .ToList();
                return new Treatment
                {
                    TreatmentProtocolId = protocol.Id,
                    Combination = antibiotics,
                };
            });

            foreach (var treatment in treatments)
            {
                var validateResult = service.IsMedicineCombinationValid(clinicalContext, treatment);
                if (validateResult.IsFailure())
                {
                    logger.LogDebug("Failed to validate treatment: {Error}", validateResult.Error);
                    return Result.Failure(validateResult.Error!);
                }

                if (!validateResult.Data)
                {
                    logger.LogDebug("Invalid treatment: all antibiotics in a treatment must belong to different group: {Detail}", new
                    {
                        AntibioticNames = treatment.Combination.Select(x => x.Name)
                    });
                    return Result.Failure(new ApplicationError(
                        ApplicationStatus.BusinessRuleViolation,
                        "Invalid treatment: all antibiotics in a treatment must belong to different group"));
                }
            }
            await context.Treatments.AddRangeAsync(treatments, cancellationToken);

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
