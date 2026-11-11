using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Application.Contracts;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Features.Shared;

/// <summary>Loads and validates the stored JSON of an analysis report.</summary>
internal static class AnalysisReportReader
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<Result<AnalysisReportResult>> ReadAsync(
        IAnalysisDbContext context,
        Guid analysisId,
        CancellationToken cancellationToken = default,
        Guid? requiredDoctorId = null
    )
    {
        if (analysisId == Guid.Empty)
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(ApplicationStatus.BadRequest, "Analysis ID is required")
            );
        }

        var row = await context
            .ClinicalAnalyses.AsNoTracking()
            .Where(analysis => analysis.Id == analysisId && !analysis.IsDeleted)
            .Select(analysis => new { analysis.DoctorId, analysis.ReportJson })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.ResourceNotFound,
                    "Analysis report was not found"
                )
            );
        }

        if (requiredDoctorId.HasValue && row.DoctorId.HasValue && row.DoctorId != requiredDoctorId.Value)
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.Restricted,
                    "You can only access your own analysis reports"
                )
            );
        }

        var report = Deserialize(row.ReportJson);
        if (report is null)
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(ApplicationStatus.ServerError, "Stored analysis report is invalid")
            );
        }

        if (requiredDoctorId.HasValue && report.DoctorId != requiredDoctorId.Value)
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.Restricted,
                    "You can only access your own analysis reports"
                )
            );
        }

        return Result<AnalysisReportResult>.Success(ApplicationStatus.Success, report);
    }

    internal static AnalysisReportResult? Deserialize(string reportJson)
    {
        try
        {
            return JsonSerializer.Deserialize<AnalysisReportResult>(reportJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
