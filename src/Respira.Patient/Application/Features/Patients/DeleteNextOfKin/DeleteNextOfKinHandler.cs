using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Patient.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.DeleteNextOfKin
{
    public class DeleteNextOfKinHandler(IDbContext context, ILogger<DeleteNextOfKinHandler> logger)
        : ICommandHandler<DeleteNextOfKinCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteNextOfKinCommand command, CancellationToken cancellationToken = default)
        {
            // Get next of kin by ID
            var nextOfKin = await context.NextOfKins.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (nextOfKin is null)
            {
                logger.LogDebug("Next of kin not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Next of kin not found"));
            }

            // Delete next of kin
            nextOfKin.IsDeleted = true;
            nextOfKin.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
