using Microsoft.EntityFrameworkCore;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Application.Constracts.Enrichment;
using Respira.Doctor.Application.Constracts.Mappers;
using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor
{
    public class GetPagedDoctorHandler(
        IDoctorDbContext dbContext,
        IPaginationFactory factory,
        IDoctorProfileEnricher enricher
    ) : IQueryHandler<GetPagedDoctorQuery, Result<Pagination<PagedDoctorItem>>>
    {
        public async Task<Result<Pagination<PagedDoctorItem>>> HandleAsync(
            GetPagedDoctorQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var filter = query.Filter ?? new DoctorFilter();

            var doctors = ApplyFilter(dbContext.Doctors.AsNoTracking(), filter);
            var headDepartmentDoctors = ApplyFilter(
                dbContext.HeadDepartmentDoctors.AsNoTracking(),
                filter
            );

            IQueryable<PagedDoctorItem> queryable;

            if (filter.Role == ProfileRole.Doctor)
            {
                queryable = Project(doctors, ProfileRole.Doctor);
            }
            else if (filter.Role == ProfileRole.HeadDepartmentDoctor)
            {
                queryable = Project(headDepartmentDoctors, ProfileRole.HeadDepartmentDoctor);
            }
            else
            {
                queryable = Project(doctors, ProfileRole.Doctor)
                    .Concat(Project(headDepartmentDoctors, ProfileRole.HeadDepartmentDoctor));
            }

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

        private static IQueryable<T> ApplyFilter<T>(IQueryable<T> source, DoctorFilter filter)
            where T : BaseDoctor
        {
            if (filter.Gender is not null)
            {
                source = source.Where(x => x.Gender == filter.Gender);
            }

            if (filter.AcademicTitle is not null)
            {
                source = source.Where(x => x.AcademicTitle == filter.AcademicTitle);
            }

            if (filter.Position is not null)
            {
                source = source.Where(x => x.Position == filter.Position);
            }

            return source;
        }

        private static IQueryable<PagedDoctorItem> Project<T>(
            IQueryable<T> source,
            ProfileRole role
        )
            where T : BaseDoctor
        {
            return source.Select(x => new PagedDoctorItem
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
            });
        }
    }
}
