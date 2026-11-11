using Respira.Analysis.Application.Contracts;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Analysis.Application.Features.Commands.Create;

public sealed record CreateAnalysisReportCommand : ICommand
{
    public Guid? DoctorId { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}
