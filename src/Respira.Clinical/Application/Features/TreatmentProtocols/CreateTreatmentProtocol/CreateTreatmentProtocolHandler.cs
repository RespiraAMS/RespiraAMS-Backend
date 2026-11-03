using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol
{
    public class CreateTreatmentProtocolHandler(
        IDbContext context,
        IDiagnoseService service,
        ICreateMapper<CreateTreatmentProtocolCommand, TreatmentProtocol> mapper,
        ILogger<CreateTreatmentProtocolHandler> logger)
        : ICommandHandler<CreateTreatmentProtocolCommand, Result<CreateTreatmentProtocolResult>>
    {
        public async Task<Result<CreateTreatmentProtocolResult>> HandleAsync(CreateTreatmentProtocolCommand command, CancellationToken cancellationToken = default)
        {
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
                return Result<CreateTreatmentProtocolResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all antibiotic IDs given exist"));
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
                return Result<CreateTreatmentProtocolResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
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
                return Result<CreateTreatmentProtocolResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Map from command to entities
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {ApplicationError}", mapResult.Error);
                return Result<CreateTreatmentProtocolResult>.Failure(mapResult.Error!);
            }
            var protocol = mapResult.Data!;
            context.UpdateRelations(protocol.Pathogens, command.Pathogens);
            context.UpdateRelations(protocol.Criteria, command.Criteria);

            var clinicalContext = new ClinicalContext
            {
                AntibioticGroups = await context.AntibioticGroups.Include(x => x.Parent).ToListAsync(cancellationToken),
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

            // Business rule: check if combination is valid
            foreach (var treatment in treatments)
            {
                var validateResult = service.IsMedicineCombinationValid(clinicalContext, treatment.Combination);
                if (validateResult.IsFailure())
                {
                    logger.LogDebug("Failed to validate treatment: {Error}", validateResult.Error);
                    return Result<CreateTreatmentProtocolResult>.Failure(validateResult.Error!);
                }

                if (!validateResult.Data)
                {
                    logger.LogDebug("Invalid treatment: all antibiotics in a treatment must belong to different group: {Detail}", new
                    {
                        AntibioticNames = treatment.Combination.Select(x => x.Name)
                    });
                    return Result<CreateTreatmentProtocolResult>.Failure(new ApplicationError(
                        ApplicationStatus.BusinessRuleViolation,
                        "Invalid treatment: all antibiotics in a treatment must belong to different group"));
                }
            }

            // Save changes to database
            await context.TreatmentProtocols.AddAsync(protocol, cancellationToken);
            await context.Treatments.AddRangeAsync(treatments, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateTreatmentProtocolResult>.Success(ApplicationStatus.Success, new CreateTreatmentProtocolResult(protocol.Id));
        }
    }
}
