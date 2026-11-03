using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.TreatmentPlans
{
    public class UpsertTreatmentPlanHandler(ILogger<UpsertTreatmentPlanHandler> logger)
        : ICommandHandler<UpsertTreatmentPlanCommand, Result<UpsertTreatmentPlanResult>>
    {
        public async Task<Result<UpsertTreatmentPlanResult>> HandleAsync(UpsertTreatmentPlanCommand command, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Receive");
            return Result<UpsertTreatmentPlanResult>.Success(ApplicationStatus.Success, new UpsertTreatmentPlanResult(Guid.CreateVersion7()));
        }
    }
}
