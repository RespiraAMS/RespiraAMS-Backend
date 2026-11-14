using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Authentication.Application.Contracts.Cache;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Application.Features.Authentication.Queries.Common;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Authentication.Application.Features.Authentication.Queries.GetList
{
    /// <summary>
    /// Lists the accounts page by page, optionally filtered by role, status and email
    /// confirmation state. Only the data that is safe to expose is returned, never the
    /// password hash.
    /// </summary>
    public class GetListAccountQueryHandler(
        IAuthDbContext dbContext,
        ICacheService cacheService,
        ILogger<GetListAccountQueryHandler> logger
    ) : IQueryHandler<GetListAccountQuery, Result<Pagination<AccountResult>>>
    {
        public async Task<Result<Pagination<AccountResult>>> HandleAsync(
            GetListAccountQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var paginationParam = query.PaginationParam;
            var filter = query.Filter ?? new AccountFilter();

            if (query.CallerRole is not null)
            {
                if (
                    query.CallerRole
                    is not (RoleType.Admin or RoleType.HeadDepartmentDoctor)
                )
                {
                    return Result<Pagination<AccountResult>>.Failure(
                        new ApplicationError(
                            ApplicationStatus.Restricted,
                            "Only Admin and HeadDepartmentDoctor accounts can list accounts"
                        )
                    );
                }

                if (
                    query.CallerRole == RoleType.HeadDepartmentDoctor
                    && filter.Role is not null
                    && filter.Role != RoleType.Doctor
                )
                {
                    return Result<Pagination<AccountResult>>.Failure(
                        new ApplicationError(
                            ApplicationStatus.Restricted,
                            "A HeadDepartmentDoctor account can only view Doctor accounts"
                        )
                    );
                }
            }

            var effectiveRole =
                query.CallerRole == RoleType.HeadDepartmentDoctor
                    ? RoleType.Doctor
                    : filter.Role;

            var cacheKey =
                $"auth:accounts:list:role:{effectiveRole?.ToString() ?? "all"}:status:{filter.Status?.ToString() ?? "all"}:confirmed:{filter.IsEmailConfirmed?.ToString() ?? "all"}:page:{paginationParam.Page}:size:{paginationParam.Size}";
            var cachedResult = await cacheService.GetAsync<Pagination<AccountResult>>(
                cacheKey,
                cancellationToken
            );

            if (cachedResult is not null)
            {
                logger.LogDebug(
                    "Authentication account list cache hit for role {Role}, status {Status}, confirmed {IsEmailConfirmed} on page {Page}",
                    effectiveRole,
                    filter.Status,
                    filter.IsEmailConfirmed,
                    paginationParam.Page
                );
                return Result<Pagination<AccountResult>>.Success(
                    ApplicationStatus.Success,
                    cachedResult
                );
            }

            var accountsQuery = dbContext
                .Accounts.AsNoTracking()
                .Where(account => !account.IsDeleted);

            if (effectiveRole is not null)
            {
                accountsQuery = accountsQuery.Where(account => account.Role == effectiveRole);
            }

            if (filter.Status is not null)
            {
                accountsQuery = accountsQuery.Where(account => account.Status == filter.Status);
            }

            if (filter.IsEmailConfirmed is not null)
            {
                accountsQuery = accountsQuery.Where(
                    account => account.IsEmailConfirmed == filter.IsEmailConfirmed
                );
            }

            var pagedAccounts = await accountsQuery
                .OrderByDescending(account => account.CreatedAt)
                .ThenBy(account => account.Id)
                .Select(account => new AccountResult
                {
                    Id = account.Id,
                    Email = account.Email,
                    Phone = account.Phone,
                    Role = account.Role,
                    IsEmailConfirmed = account.IsEmailConfirmed,
                    Status = account.Status,
                })
                .ToPagedListAsync(
                    paginationParam.Page,
                    paginationParam.Size,
                    cancellationToken: cancellationToken
                );

            var result = new Pagination<AccountResult>(
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
                "Authentication account list for role {Role}, status {Status}, confirmed {IsEmailConfirmed} returned {Count} of {TotalItemCount} account(s)",
                effectiveRole,
                filter.Status,
                filter.IsEmailConfirmed,
                pagedAccounts.Count,
                pagedAccounts.TotalItemCount
            );

            await cacheService.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(2),
                cancellationToken
            );

            return Result<Pagination<AccountResult>>.Success(ApplicationStatus.Success, result);
        }
    }
}
