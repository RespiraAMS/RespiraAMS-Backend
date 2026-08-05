using Respira.ServiceDefaults.Dtos;
using X.PagedList;

namespace Application.Contracts.Mappers;

public interface IPaginationFactory
{
    Pagination<T> Create<T>(IPagedList<T> items) where T : class;
}