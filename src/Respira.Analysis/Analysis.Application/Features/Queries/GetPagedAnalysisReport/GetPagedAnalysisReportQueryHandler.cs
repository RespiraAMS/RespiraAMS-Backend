using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Shared;
using Respira.Analysis.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Analysis.Application.Features.Queries.GetPagedAnalysisReport;

public sealed class GetPagedAnalysisReportQueryHandler(IAnalysisDbContext context)
    : IQueryHandler<GetPagedAnalysisReportQuery, Result<Pagination<AnalysisReportListItem>>>
{
    public async Task<Result<Pagination<AnalysisReportListItem>>> HandleAsync(
        GetPagedAnalysisReportQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.Param is null || query.Param.Page <= 0 || query.Param.Size is <= 0 or > 100)
        {
            return Result<Pagination<AnalysisReportListItem>>.Failure(
                new ApplicationError(
                    ApplicationStatus.BadRequest,
                    "Page must be positive and size must be between 1 and 100"
                )
            );
        }

        var filter = query.Filter;
        if (
            filter?.CreatedFrom is { } createdFrom
            && filter.CreatedTo is { } createdTo
            && createdFrom >= createdTo
        )
        {
            return Result<Pagination<AnalysisReportListItem>>.Failure(
                new ApplicationError(
                    ApplicationStatus.BadRequest,
                    "Created-from must be before created-to"
                )
            );
        }

        var queryable = context
            .ClinicalAnalyses.AsNoTracking()
            .Where(analysis => !analysis.IsDeleted);

        if (filter?.DoctorId is { } doctorId && doctorId != Guid.Empty)
        {
            queryable = queryable.Where(analysis => analysis.DoctorId == doctorId);
        }

        if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLowerInvariant();
            var isGuid = Guid.TryParse(term, out var reportId);

            queryable = queryable.Where(analysis =>
                analysis.ReportJson.ToLower().Contains(term)
                || analysis.PatientIdsJson.ToLower().Contains(term)
                || analysis.TreatmentPlanIdsJson.ToLower().Contains(term)
                || (isGuid && analysis.Id == reportId)
            );
        }

        if (filter?.CreatedFrom is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            queryable = queryable.Where(analysis => analysis.CreatedAt >= fromUtc);
        }

        if (filter?.CreatedTo is { } to)
        {
            var toUtc = to.ToUniversalTime();
            queryable = queryable.Where(analysis => analysis.CreatedAt < toUtc);
        }

        var paged = await queryable
            .OrderByDescending(analysis => analysis.CreatedAt)
            .ThenByDescending(analysis => analysis.Id)
            .ToPagedListAsync(
                query.Param.Page,
                query.Param.Size,
                cancellationToken: cancellationToken
            );

        var items = paged.Select(ToListItem).ToList();

        var pagination = new Pagination<AnalysisReportListItem>(
            new PaginationMetadata
            {
                CurrentPage = paged.PageNumber,
                HasNextPage = paged.HasNextPage,
                HasPreviousPage = paged.HasPreviousPage,
                PageCount = paged.PageCount,
                PageSize = paged.PageSize,
                TotalItemCount = paged.TotalItemCount,
            },
            items
        );

        return Result<Pagination<AnalysisReportListItem>>.Success(
            ApplicationStatus.Success,
            pagination
        );
    }

    private static AnalysisReportListItem ToListItem(ClinicalAnalysis analysis)
    {
        var report = string.IsNullOrWhiteSpace(analysis.ReportJson)
            ? null
            : AnalysisReportReader.Deserialize(analysis.ReportJson);

        return new AnalysisReportListItem(
            analysis.Id,
            analysis.DoctorId,
            analysis.Start,
            analysis.End,
            report?.TotalTreatmentPlans ?? 0,
            report?.TotalPatients ?? 0,
            report?.TotalAdherentPlans ?? 0,
            report?.TotalNonAdherentPlans ?? 0,
            report?.TotalPendingReviewPlans ?? 0,
            report?.TotalDeceasedPatients ?? 0,
            report?.TotalRecoveredPatients ?? 0,
            report?.TotalOtherOutcomePatients ?? 0,
            analysis.CreatedAt
        );
    }
}
