using Authentication.Application.Constracts.Cache;
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
        ICacheService cacheService,
        ILogger<GetListAccountQueryHandler> logger
    ) : IQueryHandler<GetListAccountQuery, Result<Pagination<Account>>>
    {
        public async Task<Result<Pagination<Account>>> HandleAsync(
            GetListAccountQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var paginationParam = query.PaginationParam;
            var cacheKey = $"auth:accounts:list:page:{paginationParam.Page}:size:{paginationParam.Size}";
            var cachedResult = await cacheService.GetAsync<Pagination<Account>>(
                cacheKey,
                cancellationToken
            );

            if (cachedResult is not null)
            {
                logger.LogDebug("Authentication account list cache hit for page {Page}", paginationParam.Page);
                return Result<Pagination<Account>>.Success(ApplicationStatus.Success, cachedResult);
            }

            var accountsQuery = dbContext
                .Accounts.AsNoTracking()
                .Where(account => !account.IsDeleted)
                .OrderByDescending(account => account.CreatedAt);

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

            await cacheService.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(2),
                cancellationToken
            );

            return Result<Pagination<Account>>.Success(ApplicationStatus.Success, result);
        }
    }
}
