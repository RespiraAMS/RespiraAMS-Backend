using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiograms.DeleteAntibiogram
{
    public class DeleteAntibiogramHandler(IDbContext context, ILogger<DeleteAntibiogramHandler> logger)
        : ICommandHandler<DeleteAntibiogramCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteAntibiogramCommand command, CancellationToken cancellationToken = default)
        {
            // Get antibiogram by ID
            var antibiogram = await context.Antibiograms.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (antibiogram is null)
            {
                logger.LogDebug("Antibiogram not found: {Id}", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Antibiogram not found"));
            }

            // Delete antibiogram
            antibiogram.IsDeleted = true;
            antibiogram.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
