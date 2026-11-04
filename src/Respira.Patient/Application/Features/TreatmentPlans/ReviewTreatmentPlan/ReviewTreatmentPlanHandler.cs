using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.TreatmentPlans.ReviewTreatmentPlan
{
    public class ReviewTreatmentPlanHandler(IDbContext context, ILogger<ReviewTreatmentPlanHandler> logger)
        : ICommandHandler<ReviewTreatmentPlanCommand, Result>
    {
        public async Task<Result> HandleAsync(ReviewTreatmentPlanCommand command, CancellationToken cancellationToken = default)
        {
            var plan = await context.TreatmentPlans
                .FirstOrDefaultAsync(x => x.Id == command.TreatmentPlanId, cancellationToken);
            if (plan is null)
            {
                logger.LogDebug("Treatment plan not found: {TreatmentPlanId}", command.TreatmentPlanId);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Treatment plan not found"));
            }

            // Business rule: a review can only applied if that treatment plan is NotAdhere
            // The adhere state is evaluated by the engine, so it shouldn't be overwrite by
            // doctor, and review is one time only (after review, cannot review them again)
            if (plan.AdherenceState != AdherenceState.NotAdhere)
            {
                logger.LogDebug("Try review a not NotAdhere treatment plan: {TreatmentPlanId}", plan.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Only NotAdhere treatment plan can be reviewed"));
            }

            // Review
            plan.AdherenceState = command.AdherenceState;
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
