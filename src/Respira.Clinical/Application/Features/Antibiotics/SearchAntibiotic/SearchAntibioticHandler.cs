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

namespace Respira.Clinical.Application.Features.Antibiotics.SearchAntibiotic
{
    public class SearchAntibioticHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<SearchAntibioticQuery, Result<Pagination<SearchAntibioticItem>>>
    {
        public async Task<Result<Pagination<SearchAntibioticItem>>> HandleAsync(SearchAntibioticQuery query, CancellationToken cancellationToken = default)
        {
            var tsQuery = FullTextSearchUtil.BuildPrefixQuery(query.SearchTerm);

            // Nothing usable to search for (empty or symbol only term)
            if (tsQuery.Length == 0)
            {
                var empty = new StaticPagedList<SearchAntibioticItem>([], 1, 10, 0);
                return Result<Pagination<SearchAntibioticItem>>.Success(ApplicationStatus.Success, factory.Create(empty));
            }

            var patients = await context.Antibiotics
                .Where(x =>
                    EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Matches(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery)))
                .Select(x => new SearchAntibioticItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    AntibioticGroup = new AntibioticGroupResult()
                    {
                        Id = x.AntibioticGroupId,
                        Name = x.AntibioticGroup.Name,
                    },
                    Classification = x.Classification,
                    Rank = EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.Name).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Rank(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery))
                })
                .OrderByDescending(x => x.Rank)
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);
            return Result<Pagination<SearchAntibioticItem>>.Success(ApplicationStatus.Success, factory.Create(patients));
        }
    }
}
