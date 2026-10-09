using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;
using X.PagedList.Extensions;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable
{
    public class GetPagedClinicalVariableHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedClinicalVariableQuery, Result<Pagination<PagedClinicalVariableItem>>>
    {
        public async Task<Result<Pagination<PagedClinicalVariableItem>>> HandleAsync(GetPagedClinicalVariableQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.ClinicalVariables.AsQueryable();
            if (query.Filter is not null)
            {
                if (query.Filter.Name is not null)
                {
                    queryable = queryable.Where(x => EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%"));
                }

                if (query.Filter.Code is not null)
                {
                    queryable = queryable.Where(x => x.Code.Equals(query.Filter.Code));
                }

                if (query.Filter.IsRequired is not null)
                {
                    queryable = queryable.Where(x => x.IsRequired == query.Filter.IsRequired);
                }

                // Because we use TPH with ValueType as discriminator and EF Core ignore ValueType,
                // we need this work around to filter by ValueType
                if (query.Filter.ValueType is { } valueType)
                {
                    queryable = valueType switch
                    {
                        ClinicalValueType.Boolean => queryable.Where(x => x is BooleanClinicalVariable),
                        ClinicalValueType.Numeric => queryable.Where(x => x is NumericClinicalVariable),
                        ClinicalValueType.Categorical => queryable.Where(x => x is CategoricalClinicalVariable),
                        // If the value type is not defined, then the filter shouldn't return
                        // anything (since this value type does not exist -> no data)
                        // The category simply use a direct comparison, so it doesn't need
                        // a work around like ValueType
                        _ => queryable.Where(_ => false),
                    };
                }

                if (query.Filter.Category is not null)
                {
                    queryable = queryable.Where(x => x.Category == query.Filter.Category);
                }
            }

            var variables = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedClinicalVariableItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    IsRequired = x.IsRequired,
                    CanonicalUnit = x.CanonicalUnit,
                    ValueType = x.ValueType,
                    Category = x.Category
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size);

            return Result<Pagination<PagedClinicalVariableItem>>.Success(ApplicationStatus.Success, factory.Create(variables));
        }
    }
}
