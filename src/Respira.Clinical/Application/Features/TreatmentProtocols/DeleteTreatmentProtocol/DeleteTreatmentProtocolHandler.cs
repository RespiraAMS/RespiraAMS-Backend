using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.DeleteTreatmentProtocol
{
    public class DeleteTreatmentProtocolHandler(
        IDbContext context,
        ILogger<DeleteTreatmentProtocolHandler> logger)
        : ICommandHandler<DeleteTreatmentProtocolCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteTreatmentProtocolCommand command, CancellationToken cancellationToken = default)
        {
            // Get protocol by ID
            var protocol = await context.TreatmentProtocols
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (protocol is null)
            {
                logger.LogDebug("Treatment protocol not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Treatment protocol not found"));
            }

            // Soft delete protocol and all its treatments
            await context.ExecuteInTransactionAsync(async () =>
            {
                // Soft delete protocol
                protocol.IsDeleted = true;
                protocol.DeletedAt = DateTimeOffset.UtcNow;

                // Soft delete all associated treatments
                var treatmentDeleted = await context.Treatments
                    .Where(x => x.TreatmentProtocolId == protocol.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(t => t.IsDeleted, true)
                        .SetProperty(t => t.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);
                logger.LogDebug("Cascade soft delete treatment protocol: {Detail}", new
                {
                    TreatmentProtocolId = protocol.Id,
                    TreatmentDeletedCount = treatmentDeleted,
                });
            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
