using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Analysis.Application.Features.Commands.DeleteAnalysisReport;

public sealed record DeleteAnalysisReportCommand : ICommand
{
    public Guid AnalysisId { get; init; }
    public Guid ActorAccountId { get; init; }
    public string ActorRole { get; init; } = string.Empty;
}

public sealed record DeleteAnalysisReportResult(Guid AnalysisId);
