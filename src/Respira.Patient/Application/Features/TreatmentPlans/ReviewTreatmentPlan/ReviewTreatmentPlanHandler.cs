using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
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

            // Review
            plan.AdherenceState = command.AdherenceState;
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
