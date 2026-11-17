using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.TreatmentPlans.CloseTreatmentPlan
{
    public class CloseTreatmentPlanHandler(IDbContext context, ILogger<CloseTreatmentPlanHandler> logger)
        : ICommandHandler<CloseTreatmentPlanCommand, Result>
    {
        public async Task<Result> HandleAsync(CloseTreatmentPlanCommand command, CancellationToken cancellationToken = default)
        {
            var plan = await context.TreatmentPlans
                .FirstOrDefaultAsync(x => x.Id == command.TreatmentPlanId, cancellationToken);
            if (plan is null)
            {
                logger.LogDebug("Treatment plan not found: {TreatmentPlanId}", command.TreatmentPlanId);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Treatment plan not found"));
            }

            // Check if this treatment plan is already closed or not
            if (plan.Status == TreatmentPlanStatus.Closed)
            {
                logger.LogDebug("This treatment plan is already closed");
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Treatment plan is already closed"));
            }

            plan.Status = TreatmentPlanStatus.Closed;
            plan.CloseReason = command.Reason;
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
