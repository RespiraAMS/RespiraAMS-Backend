using Application.Contracts.Mappers;
using Respira.ServiceDefaults.Dtos;
using X.PagedList;

namespace Infrastructure.Mappers;

public class PaginationFactory : IPaginationFactory
{
    public Pagination<T> Create<T>(IPagedList<T> items) where T : class
    {
        return new Pagination<T>(
            new PaginationMetadata
            {
                CurrentPage = items.PageNumber,
                HasNextPage = items.HasNextPage,
                HasPreviousPage = items.HasPreviousPage,
                PageCount = items.PageCount,
                PageSize = items.PageSize,
                TotalItemCount = items.TotalItemCount,
            },
            items
        );
    }
}