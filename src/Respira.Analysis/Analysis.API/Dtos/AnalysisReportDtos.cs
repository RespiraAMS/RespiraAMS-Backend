using Respira.Analysis.Application.Features.Queries.GetPagedAnalysisReport;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Analysis.API.Dtos;

public sealed record GetPagedAnalysisReportRequestDto
{
    /// <summary>Page index, starting at 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size, between 1 and 100.</summary>
    public int Size { get; init; } = 10;

    /// <summary>Only return reports created by this doctor.</summary>
    public Guid? DoctorId { get; init; }

    /// <summary>Free text matched against the report id and the stored report content.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Lower bound (inclusive) for the report creation time.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Upper bound (exclusive) for the report creation time.</summary>
    public DateTimeOffset? CreatedTo { get; init; }

    public GetPagedAnalysisReportQuery ToQuery(Guid? doctorId) =>
        new()
        {
            Param = new PaginationParam { Page = Page, Size = Size },
            Filter = new AnalysisReportFilter
            {
                DoctorId = doctorId,
                SearchTerm = SearchTerm,
                CreatedFrom = CreatedFrom,
                CreatedTo = CreatedTo,
            },
        };
}
