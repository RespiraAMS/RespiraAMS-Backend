using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Utils.FullTextSearch;
using X.PagedList;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric
{
    public class SearchClinicalMetricHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<SearchClinicalMetricQuery, Result<Pagination<SearchClinicalMetricItem>>>
    {
        public async Task<Result<Pagination<SearchClinicalMetricItem>>> HandleAsync(SearchClinicalMetricQuery query, CancellationToken cancellationToken = default)
        {
            var tsQuery = FullTextSearchUtil.BuildPrefixQuery(query.SearchTerm);

            // Nothing usable to search for (empty or symbol only term)
            if (tsQuery.Length == 0)
            {
                var empty = new StaticPagedList<SearchClinicalMetricItem>([], 1, 10, 0);
                return Result<Pagination<SearchClinicalMetricItem>>.Success(ApplicationStatus.Success, factory.Create(empty));
            }

            var patients = await context.ClinicalMetrics
                .Where(x =>
                    EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Code).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Description).SetWeight(NpgsqlTsVector.Lexeme.Weight.C))
                    .Matches(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery)))
                .Select(x => new SearchClinicalMetricItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Description = x.Description,
                    Version = x.Version,
                    Publisher = x.Publisher,
                    IsApplied = x.IsApplied,
                    Rank = EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Code).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Description).SetWeight(NpgsqlTsVector.Lexeme.Weight.C))
                    .Rank(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery))
                })
                .OrderByDescending(x => x.Rank)
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);
            return Result<Pagination<SearchClinicalMetricItem>>.Success(ApplicationStatus.Success, factory.Create(patients));
        }
    }
}
