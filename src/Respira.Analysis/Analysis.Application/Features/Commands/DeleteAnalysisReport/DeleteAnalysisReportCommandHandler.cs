using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Features.Commands.DeleteAnalysisReport;

public sealed class DeleteAnalysisReportCommandHandler(
    IAnalysisDbContext context,
    ILogger<DeleteAnalysisReportCommandHandler> logger
) : ICommandHandler<DeleteAnalysisReportCommand, Result<DeleteAnalysisReportResult>>
{
    public async Task<Result<DeleteAnalysisReportResult>> HandleAsync(
        DeleteAnalysisReportCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (command.AnalysisId == Guid.Empty)
        {
            return Result<DeleteAnalysisReportResult>.Failure(
                new ApplicationError(ApplicationStatus.BadRequest, "Analysis ID is required")
            );
        }

        var analysis = await context
            .ClinicalAnalyses
            .FirstOrDefaultAsync(
                item => item.Id == command.AnalysisId && !item.IsDeleted,
                cancellationToken
            );

        if (analysis is null)
        {
            return Result<DeleteAnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.ResourceNotFound,
                    "Analysis report was not found"
                )
            );
        }

        // A doctor may only delete reports that belong to them; admins and head department doctors may delete any.
        var role = command.ActorRole?.Trim();
        var canManageAll =
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "HeadDepartmentDoctor", StringComparison.OrdinalIgnoreCase);
        var isOwner =
            command.ActorAccountId != Guid.Empty
            && analysis.DoctorId is { } doctorId
            && doctorId == command.ActorAccountId;

        if (!canManageAll && !isOwner)
        {
            return Result<DeleteAnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.Restricted,
                    "You can only delete your own analysis reports"
                )
            );
        }

        analysis.IsDeleted = true;
        analysis.DeletedAt = DateTimeOffset.UtcNow;
        analysis.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Analysis report {AnalysisId} soft-deleted by {ActorRole}/{ActorAccountId}",
            analysis.Id,
            command.ActorRole,
            command.ActorAccountId
        );

        return Result<DeleteAnalysisReportResult>.Success(
            ApplicationStatus.Success,
            new DeleteAnalysisReportResult(analysis.Id)
        );
    }
}
