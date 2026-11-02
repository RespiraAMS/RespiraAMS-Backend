using Respira.ServiceDefaults.Contracts.Pagination;
using X.PagedList;

namespace Respira.Doctor.Application.Constracts.Mappers
{
    public interface IPaginationFactory
    {
        Pagination<T> Create<T>(IPagedList<T> items) where T : class;
    }
}
