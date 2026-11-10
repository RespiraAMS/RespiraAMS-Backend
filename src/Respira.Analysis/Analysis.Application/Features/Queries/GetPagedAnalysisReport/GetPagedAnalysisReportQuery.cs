using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Analysis.Application.Features.Queries.GetPagedAnalysisReport;

public sealed record AnalysisReportFilter
{
    /// <summary>Only return reports created by this doctor.</summary>
    public Guid? DoctorId { get; init; }

    /// <summary>Free text matched against the report id and the stored report content.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Lower bound (inclusive) for the report creation time.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Upper bound (exclusive) for the report creation time.</summary>
    public DateTimeOffset? CreatedTo { get; init; }
}

public sealed record GetPagedAnalysisReportQuery : IQuery
{
    public required PaginationParam Param { get; init; }

    public AnalysisReportFilter? Filter { get; init; } = new();
}

public sealed record AnalysisReportListItem(
    Guid AnalysisId,
    Guid? DoctorId,
    DateTimeOffset Start,
    DateTimeOffset End,
    int TotalTreatmentPlans,
    int TotalPatients,
    int TotalAdherentPlans,
    int TotalNonAdherentPlans,
    int TotalPendingReviewPlans,
    int TotalDeceasedPatients,
    int TotalRecoveredPatients,
    int TotalOtherOutcomePatients,
    DateTimeOffset CreatedAt
);
