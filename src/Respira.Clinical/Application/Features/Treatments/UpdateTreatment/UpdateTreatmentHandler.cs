using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Microsoft.EntityFrameworkCore;

namespace Respira.Clinical.Application.Features.Treatments.UpdateTreatment
{
    public class UpdateTreatmentHandler(IDbContext context, ILogger<UpdateTreatmentHandler> logger)
        : ICommandHandler<UpdateTreatmentCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateTreatmentCommand command, CancellationToken cancellationToken = default)
        {
            // Check if treatment exists in database
            var treatment = await context.Treatments
                .Include(x => x.Pathogens)
                .Include(x => x.Criteria)
                .Include(x => x.Medicines)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (treatment is null)
            {
                logger.LogDebug("Treatment with ID {id} does not exist in database", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Treatment not found"));
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
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Not all antibiotic IDs given exist"));
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
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Not all pathogen IDs given exist"));
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
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Not all criteria IDs given exist"));
            }

            // Map from command to entities
            treatment.Severity = command.Severity;
            treatment.TreatmentSite = command.TreatmentSite;

            // Clean all the join tables data
            treatment.Medicines.Clear();
            treatment.Pathogens.Clear();
            treatment.Criteria.Clear();

            context.UpdateRelations(treatment.Pathogens, command.Pathogens);
            context.UpdateRelations(treatment.Criteria, command.Criteria);

            // Adding medicine compositions from a MedicalComposition entity
            var compositions = command.Medicines.ConvertAll(m =>
            {
                var composition = new MedicineComposition { TreatmentId = treatment.Id };
                context.UpdateRelations(composition.Antibiotics, m);
                return composition;
            });
            await context.MedicineCompositions.AddRangeAsync(compositions);

            treatment.UpdatedAt = DateTimeOffset.UtcNow;

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
