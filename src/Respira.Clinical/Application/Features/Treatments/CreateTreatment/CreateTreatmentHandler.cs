using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Application.Contracts.Mappers;

namespace Respira.Clinical.Application.Features.Treatments.CreateTreatment
{
    public class CreateTreatmentHandler(
        IDbContext context,
        ICreateMapper<CreateTreatmentCommand, Treatment> mapper,
        ILogger<CreateTreatmentHandler> logger)
        : ICommandHandler<CreateTreatmentCommand, Result<CreateTreatmentResult>>
    {
        public async Task<Result<CreateTreatmentResult>> HandleAsync(CreateTreatmentCommand command, CancellationToken cancellationToken = default)
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
                return Result<CreateTreatmentResult>.Failure(new Error(ApplicationStatus.BadRequest, "Not all antibiotic IDs given exist"));
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
                return Result<CreateTreatmentResult>.Failure(new Error(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
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
                return Result<CreateTreatmentResult>.Failure(new Error(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Map from command to entities
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to model: {Error}", mapResult.Error);
                return Result<CreateTreatmentResult>.Failure(mapResult.Error!);
            }
            var treatment = mapResult.Data!;
            context.UpdateRelations(treatment.Pathogens, command.Pathogens);
            context.UpdateRelations(treatment.Criteria, command.Criteria);

            // Adding medicine compositions from a MedicalComposition entity
            var compositions = command.Medicines.ConvertAll(m =>
            {
                var composition = new MedicineComposition { TreatmentId = treatment.Id };
                context.UpdateRelations(composition.Antibiotics, m);
                return composition;
            });

            // Save changes to database
            await context.Treatments.AddAsync(treatment);
            await context.MedicineCompositions.AddRangeAsync(compositions);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateTreatmentResult>.Success(ApplicationStatus.Success, new CreateTreatmentResult(treatment.Id));
        }
    }
}
