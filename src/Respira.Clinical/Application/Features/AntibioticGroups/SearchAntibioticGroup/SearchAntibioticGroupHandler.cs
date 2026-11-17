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

namespace Respira.Clinical.Application.Features.AntibioticGroups.SearchAntibioticGroup
{
    public class SearchAntibioticGroupHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<SearchAntibioticGroupQuery, Result<Pagination<SearchAntibioticGroupItem>>>
    {
        public async Task<Result<Pagination<SearchAntibioticGroupItem>>> HandleAsync(SearchAntibioticGroupQuery query, CancellationToken cancellationToken = default)
        {
            var tsQuery = FullTextSearchUtil.BuildPrefixQuery(query.SearchTerm);

            // Nothing usable to search for (empty or symbol only term)
            if (tsQuery.Length == 0)
            {
                var empty = new StaticPagedList<SearchAntibioticGroupItem>([], 1, 10, 0);
                return Result<Pagination<SearchAntibioticGroupItem>>.Success(ApplicationStatus.Success, factory.Create(empty));
            }

            var patients = await context.AntibioticGroups
                .Where(x =>
                    EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Description).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Matches(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery)))
                .Select(x => new SearchAntibioticGroupItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    ParentId = x.ParentId,
                    ParentName = x.Parent == null ? null : x.Parent.Name,
                    Description = x.Description,
                    Rank = EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Description).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Rank(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery))
                })
                .OrderByDescending(x => x.Rank)
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);
            return Result<Pagination<SearchAntibioticGroupItem>>.Success(ApplicationStatus.Success, factory.Create(patients));
        }
    }
}
