using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Respira.Doctor.Application.Constracts.Enrichment;
using Respira.Doctor.Application.Constracts.Mappers;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Utils.FullTextSearch;
using X.PagedList;
using X.PagedList.EF;

namespace Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor
{
    public class SearchDoctorHandler(
        IDoctorDbContext dbContext,
        IPaginationFactory factory,
        IDoctorProfileEnricher enricher
    ) : IQueryHandler<SearchDoctorQuery, Result<Pagination<SearchDoctorItem>>>
    {
        public async Task<Result<Pagination<SearchDoctorItem>>> HandleAsync(
            SearchDoctorQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var tsQuery = FullTextSearchUtil.BuildPrefixQuery(query.SearchTerm);

            // Nothing usable to search for (empty or symbol only term).
            if (tsQuery.Length == 0)
            {
                var empty = new StaticPagedList<SearchDoctorItem>(
                    [],
                    query.Param.Page,
                    query.Param.Size,
                    0
                );
                return Result<Pagination<SearchDoctorItem>>.Success(
                    ApplicationStatus.Success,
                    factory.Create(empty)
                );
            }

            var doctors = ProjectSearch(
                dbContext
                    .Doctors.AsNoTracking()
                    .Where(x =>
                        x.SearchVector.Matches(
                            EF.Functions.ToTsQuery(
                                FullTextSearchUtil.ConfigurationName,
                                tsQuery
                            )
                        )
                    ),
                ProfileRole.Doctor,
                tsQuery
            );

            var headDepartmentDoctors = ProjectSearch(
                dbContext
                    .HeadDepartmentDoctors.AsNoTracking()
                    .Where(x =>
                        x.SearchVector.Matches(
                            EF.Functions.ToTsQuery(
                                FullTextSearchUtil.ConfigurationName,
                                tsQuery
                            )
                        )
                    ),
                ProfileRole.HeadDepartmentDoctor,
                tsQuery
            );

            var profiles = await doctors
                .Concat(headDepartmentDoctors)
                .OrderByDescending(x => x.Rank)
                .ThenBy(x => x.Id)
                .ToPagedListAsync(
                    query.Param.Page,
                    query.Param.Size,
                    cancellationToken: cancellationToken
                );

            await enricher.EnrichAsync(profiles, cancellationToken);

            return Result<Pagination<SearchDoctorItem>>.Success(
                ApplicationStatus.Success,
                factory.Create(profiles)
            );
        }

        private static IQueryable<SearchDoctorItem> ProjectSearch<T>(
            IQueryable<T> source,
            ProfileRole role,
            string tsQuery
        )
            where T : BaseDoctor
        {
            return source.Select(x => new SearchDoctorItem
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Role = role,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Degrees = x.Degrees,
                AcademicTitle = x.AcademicTitle,
                Position = x.Position,
                Gender = x.Gender,
                CitizenIdentificationNumber = x.CitizenIdentificationNumber,
                DateOfBirth = x.DateOfBirth,
                Address = x.Address,
                MediaId = x.MediaId,
                CreatedAt = x.CreatedAt,
                Rank = EF.Functions
                    .ToTsVector(FullTextSearchUtil.ConfigurationName, x.FirstName)
                    .SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(
                        EF.Functions
                            .ToTsVector(FullTextSearchUtil.ConfigurationName, x.LastName)
                            .SetWeight(NpgsqlTsVector.Lexeme.Weight.B)
                    )
                    .Concat(
                        EF.Functions
                            .ToTsVector(
                                FullTextSearchUtil.ConfigurationName,
                                x.CitizenIdentificationNumber
                            )
                            .SetWeight(NpgsqlTsVector.Lexeme.Weight.C)
                    )
                    .Rank(
                        EF.Functions.ToTsQuery(
                            FullTextSearchUtil.ConfigurationName,
                            tsQuery
                        )
                    ),
            });
        }
    }
}
