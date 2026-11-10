using Respira.Analysis.Application.Contracts;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Analysis.Application.Features.Queries.GetAnalysisReport;

public sealed record GetAnalysisReportQuery : IQuery
{
    public Guid AnalysisId { get; init; }
    public Guid? RequiredDoctorId { get; init; }
}
