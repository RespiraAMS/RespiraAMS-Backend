using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Contracts.Export;
using Respira.Analysis.Application.Features.Shared;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Features.Queries.ExportAnalysisReportExcel;

public sealed class ExportAnalysisReportExcelQueryHandler(
    IAnalysisDbContext context,
    IAnalysisReportExcelWriter writer
) : IQueryHandler<ExportAnalysisReportExcelQuery, Result<AnalysisReportExcelFile>>
{
    public const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<AnalysisReportExcelFile>> HandleAsync(
        ExportAnalysisReportExcelQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var reportResult = await AnalysisReportReader.ReadAsync(
            context,
            query.AnalysisId,
            cancellationToken,
            query.RequiredDoctorId
        );

        if (reportResult.IsFailure() || reportResult.Data is null)
        {
            return Result<AnalysisReportExcelFile>.Failure(reportResult.Error!);
        }

        try
        {
            var report = reportResult.Data;
            var content = writer.Write(report);
            var fileName = BuildFileName(report.AnalysisId);

            return Result<AnalysisReportExcelFile>.Success(
                ApplicationStatus.Success,
                new AnalysisReportExcelFile(
                    report.AnalysisId,
                    report.DoctorId,
                    fileName,
                    ExcelContentType,
                    content
                )
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<AnalysisReportExcelFile>.Failure(
                new ApplicationError(
                    ApplicationStatus.ServerError,
                    "Analysis report could not be exported"
                )
            );
        }
    }

    private static string BuildFileName(Guid analysisId) =>
        $"analysis-report-{analysisId:N}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx";
}
