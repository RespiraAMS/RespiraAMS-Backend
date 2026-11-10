using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Shared;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Features.Queries.GetAnalysisReport;

public sealed class GetAnalysisReportQueryHandler(IAnalysisDbContext context)
    : IQueryHandler<GetAnalysisReportQuery, Result<AnalysisReportResult>>
{
    public Task<Result<AnalysisReportResult>> HandleAsync(
        GetAnalysisReportQuery query,
        CancellationToken cancellationToken = default
    ) => AnalysisReportReader.ReadAsync(
        context,
        query.AnalysisId,
        cancellationToken,
        query.RequiredDoctorId
    );
}
