using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Application.Contracts.Enrichment;
using Respira.Doctor.Application.Contracts.Mappers;
using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Utils.FullTextSearch;
using X.PagedList.EF;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetSubordinates
{
    public class GetSubordinateDoctorsHandler(
        IDoctorDbContext dbContext,
        IPaginationFactory factory,
        IDoctorProfileEnricher enricher
    ) : IQueryHandler<GetSubordinateDoctorsQuery, Result<Pagination<PagedDoctorItem>>>
    {
        public async Task<Result<Pagination<PagedDoctorItem>>> HandleAsync(
            GetSubordinateDoctorsQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var headDoctor = await dbContext
                .HeadDepartmentDoctors.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.AccountId == query.HeadDoctorAccountId,
                    cancellationToken
                );

            if (headDoctor is null)
            {
                return Result<Pagination<PagedDoctorItem>>.Failure(
                    new ApplicationError(
                        ApplicationStatus.ResourceNotFound,
                        "Head department doctor profile not found"
                    )
                );
            }

            var doctors = dbContext
                .Doctors.AsNoTracking()
                .Where(x => x.HeadDepartmentDoctorId == headDoctor.Id);

            var filter = query.Filter;
            if (filter is not null)
            {
                if (filter.Gender.HasValue)
                {
                    doctors = doctors.Where(x => x.Gender == filter.Gender.Value);
                }

                if (filter.AcademicTitle.HasValue)
                {
                    doctors = doctors.Where(x => x.AcademicTitle == filter.AcademicTitle.Value);
                }

                if (filter.Position.HasValue)
                {
                    doctors = doctors.Where(x => x.Position == filter.Position.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var tsQuery = FullTextSearchUtil.BuildPrefixQuery(filter.SearchTerm);
                    if (tsQuery.Length > 0)
                    {
                        doctors = doctors.Where(x =>
                            x.SearchVector.Matches(
                                EF.Functions.ToTsQuery(
                                    FullTextSearchUtil.ConfigurationName,
                                    tsQuery
                                )
                            )
                        );
                    }
                }
            }

            var queryable = doctors.Select(x => new PagedDoctorItem
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Role = ProfileRole.Doctor,
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
            });

            var profiles = await queryable
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToPagedListAsync(
                    query.Param.Page,
                    query.Param.Size,
                    cancellationToken: cancellationToken
                );

            await enricher.EnrichAsync(profiles, cancellationToken);

            return Result<Pagination<PagedDoctorItem>>.Success(
                ApplicationStatus.Success,
                factory.Create(profiles)
            );
        }
    }
}
