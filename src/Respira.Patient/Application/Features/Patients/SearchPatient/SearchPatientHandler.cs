using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Utils.FullTextSearch;
using X.PagedList;
using X.PagedList.EF;

namespace Respira.Patient.Application.Features.Patients.SearchPatient
{
    public class SearchPatientHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<SearchPatientQuery, Result<Pagination<SearchPatientItem>>>
    {
        public async Task<Result<Pagination<SearchPatientItem>>> HandleAsync(SearchPatientQuery query, CancellationToken cancellationToken = default)
        {
            var tsQuery = FullTextSearchUtil.BuildPrefixQuery(query.SearchTerm);

            // Nothing usable to search for (empty or symbol only term)
            if (tsQuery.Length == 0)
            {
                var empty = new StaticPagedList<SearchPatientItem>([], 1, 10, 0);
                return Result<Pagination<SearchPatientItem>>.Success(ApplicationStatus.Success, factory.Create(empty));
            }

            var patients = await context.Patients
                .Where(x =>
                    EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.FirstName).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.LastName).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                    .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.MedicalRecordCode).SetWeight(NpgsqlTsVector.Lexeme.Weight.C))
                    .Matches(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery)))
                .Select(x => new SearchPatientItem
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    DateOfBirth = x.DateOfBirth,
                    Age = x.Age(),
                    Gender = x.Gender,
                    MedicalRecordCode = x.MedicalRecordCode,
                    TreatmentOutcome = x.TreatmentOutcome,
                    Rank = EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.FirstName).SetWeight(NpgsqlTsVector.Lexeme.Weight.A)
                        .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.LastName).SetWeight(NpgsqlTsVector.Lexeme.Weight.B))
                        .Concat(EF.Functions.ToTsVector(FullTextSearchUtil.ConfigurationName, x.MedicalRecordCode).SetWeight(NpgsqlTsVector.Lexeme.Weight.C))
                        .Rank(EF.Functions.ToTsQuery(FullTextSearchUtil.ConfigurationName, tsQuery))
                })
                .OrderByDescending(x => x.Rank)
                .ToPagedListAsync(query.Param.Page, query.Param.Size, cancellationToken: cancellationToken);
            return Result<Pagination<SearchPatientItem>>.Success(ApplicationStatus.Success, factory.Create(patients));
        }
    }
}
