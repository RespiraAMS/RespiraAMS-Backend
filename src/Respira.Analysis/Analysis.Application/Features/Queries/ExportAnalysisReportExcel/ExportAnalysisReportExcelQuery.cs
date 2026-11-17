using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Analysis.Application.Features.Queries.ExportAnalysisReportExcel;

public sealed record ExportAnalysisReportExcelQuery : IQuery
{
    public Guid AnalysisId { get; init; }
    public Guid? RequiredDoctorId { get; init; }
}
