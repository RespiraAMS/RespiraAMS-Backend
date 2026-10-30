using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol
{
    public class CreateTreatmentProtocolHandler(
        IDbContext context,
        ICreateMapper<CreateTreatmentProtocolCommand, TreatmentProtocol> mapper,
        ILogger<CreateTreatmentProtocolHandler> logger)
        : ICommandHandler<CreateTreatmentProtocolCommand, Result<CreateTreatmentResult>>
    {
        public async Task<Result<CreateTreatmentResult>> HandleAsync(CreateTreatmentProtocolCommand protocolCommand, CancellationToken cancellationToken = default)
        {
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
                return Result<CreateTreatmentResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all antibiotic IDs given exist"));
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
                return Result<CreateTreatmentResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
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
                return Result<CreateTreatmentResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Map from protocolCommand to entities
            var mapResult = mapper.ToModel(protocolCommand);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map protocolCommand to model: {ApplicationError}", mapResult.Error);
                return Result<CreateTreatmentResult>.Failure(mapResult.Error!);
            }
            var treatment = mapResult.Data!;
            context.UpdateRelations(treatment.Pathogens, protocolCommand.Pathogens);
            context.UpdateRelations(treatment.Criteria, protocolCommand.Criteria);

            // Adding medicine compositions from a MedicalComposition entity
            var compositions = protocolCommand.Medicines.ConvertAll(m =>
            {
                var composition = new Treatment { TreatmentProtocolId = treatment.Id };
                context.UpdateRelations(composition.Combination, m);
                return composition;
            });

            // Save changes to database
            await context.TreatmentProtocols.AddAsync(treatment, cancellationToken);
            await context.MedicineCompositions.AddRangeAsync(compositions, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateTreatmentResult>.Success(ApplicationStatus.Success, new CreateTreatmentResult(treatment.Id));
        }
    }
}
