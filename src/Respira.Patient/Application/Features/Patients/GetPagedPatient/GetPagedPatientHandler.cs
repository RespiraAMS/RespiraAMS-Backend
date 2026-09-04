using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.Patients.GetPagedPatient;

public class GetPagedPatientHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedPatientQuery, Result<Pagination<PagedPatientItem>>>
{
    public async Task<Result<Pagination<PagedPatientItem>>> HandleAsync(GetPagedPatientQuery query, CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.Patients.AsQueryable();
        if (query.Filter is not null)
        {
            if (query.Filter.FullName is not null)
            {
                queryable = queryable.Where(x => EF.Functions.ILike(x.FullName, $"%{query.Filter.FullName}%"));
            }

            if (query.Filter.MedicalRecordCode is not null)
            {
                queryable = queryable.Where(x => EF.Functions.ILike(x.MedicalRecordCode, $"%{query.Filter.MedicalRecordCode}%"));
            }
        }

        // Get paged patients
        var patients = await queryable
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PagedPatientItem
            {
                Id = x.Id,
                FullName = x.FullName,
                Age = x.Age(),
                IsMale = x.IsMale,
                MedicalRecordCode = x.MedicalRecordCode,
                Status = x.Status,
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);

        return Result<Pagination<PagedPatientItem>>.Success(Status.Success, factory.Create(patients));
    }
}
