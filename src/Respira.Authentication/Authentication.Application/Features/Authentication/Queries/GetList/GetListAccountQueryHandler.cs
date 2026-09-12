using Authentication.Application.Constracts.Data;
using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Authentication.Application.Features.Authentication.Queries.GetList
{
    public class GetListAccountQueryHandler(
        IAuthDbContext dbContext,
        ILogger<GetListAccountQueryHandler> logger
    ) : IQueryHandler<GetListAccountQuery, Result<Pagination<Account>>>
    {
        public async Task<Result<Pagination<Account>>> HandleAsync(
            GetListAccountQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var accountsQuery = dbContext
                .Accounts.AsNoTracking()
                .Where(account => !account.IsDeleted)
                .OrderByDescending(account => account.CreatedAt);

            var paginationParam = new PaginationParam();

            var pagedAccounts = await accountsQuery.ToPagedListAsync(
                paginationParam.Page,
                paginationParam.Size
            );

            var result = new Pagination<Account>(
                new PaginationMetadata
                {
                    CurrentPage = paginationParam.Page,
                    PageSize = paginationParam.Size,
                    TotalItemCount = pagedAccounts.TotalItemCount,
                    PageCount = pagedAccounts.PageCount,
                    HasPreviousPage = pagedAccounts.HasPreviousPage,
                    HasNextPage = pagedAccounts.HasNextPage,
                },
                pagedAccounts
            );

            logger.LogInformation(
                "Authentication account list returned {Count} of {TotalItemCount} account(s)",
                pagedAccounts.Count,
                pagedAccounts.TotalItemCount
            );

            return Result<Pagination<Account>>.Success(ApplicationStatus.Success, result);
        }
    }
}
