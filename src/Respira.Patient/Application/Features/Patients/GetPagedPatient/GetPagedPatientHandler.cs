using Microsoft.EntityFrameworkCore;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.Extensions;

namespace Respira.Patient.Application.Features.Patients.GetPagedPatient
{
    public class GetPagedPatientHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedPatientQuery, Result<Pagination<PagedPatientItem>>>
    {
        public async Task<Result<Pagination<PagedPatientItem>>> HandleAsync(GetPagedPatientQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.Patients.AsQueryable();
            if (query.Filter is not null)
            {
                if (query.Filter.Gender is not null)
                {
                    queryable = queryable.Where(x => x.Gender == query.Filter.Gender);
                }
            }

            var patients = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedPatientItem
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    DateOfBirth = x.DateOfBirth,
                    Age = x.Age(),
                    Gender = x.Gender,
                    MedicalRecordCode = x.MedicalRecordCode,
                    TreatmentOutcome = x.TreatmentOutcome
                })
                .ToListAsync(cancellationToken);

            // Because age is function, thus cannot be translated to SQL,
            // so we need to switch to client evaluation. Although it can
            // be slower than server evaluation, but it's better to repeat
            // the age calculation logic
            if (query.Filter is not null)
            {
                if (query.Filter.MinAge is not null)
                {
                    patients = [.. patients.Where(x => x.Age >= query.Filter.MinAge)];
                }

                if (query.Filter.MaxAge is not null)
                {
                    patients = [.. patients.Where(x => x.Age <= query.Filter.MaxAge)];
                }
            }

            var result = patients.ToPagedList(query.Param.Page, query.Param.Size);

            return Result<Pagination<PagedPatientItem>>.Success(ApplicationStatus.Success, factory.Create(result));
        }
    }
}
