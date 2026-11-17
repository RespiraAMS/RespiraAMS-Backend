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

namespace Respira.Clinical.Application.Features.Pathogens.SearchPathogen
{
    public class SearchPathogenHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<SearchPathogenQuery, Result<Pagination<SearchPathogenItem>>>
    {
        public async Task<Result<Pagination<SearchPathogenItem>>> HandleAsync(SearchPathogenQuery query, CancellationToken cancellationToken = default)
        {
            var tsQuery = FullTextSearchUtil.BuildPrefixQuery(query.SearchTerm);

            // Nothing usable to search for (empty or symbol only term)
            if (tsQuery.Length == 0)
            {
                var empty = new StaticPagedList<SearchPathogenItem>([], 1, 10, 0);
                return Result<Pagination<SearchPathogenItem>>.Success(ApplicationStatus.Success, factory.Create(empty));
            }

            var patients = await context.Pathogens
                .Where(x =>
                    EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Description).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Matches(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery)))
                .Select(x => new SearchPathogenItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsAtypical = x.IsAtypical,
                    Rank = EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Description).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Rank(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery))
                })
                .OrderByDescending(x => x.Rank)
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);
            return Result<Pagination<SearchPathogenItem>>.Success(ApplicationStatus.Success, factory.Create(patients));
        }
    }
}
