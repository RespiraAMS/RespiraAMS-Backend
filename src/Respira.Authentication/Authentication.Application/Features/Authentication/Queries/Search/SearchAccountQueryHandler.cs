using System.Security.Cryptography;
using System.Text;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Authentication.Queries.Search.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Authentication.Application.Features.Authentication.Queries.Search
{
    public class SearchAccountQueryHandler(
        IAuthDbContext dbContext,
        ICacheService cacheService,
        ILogger<SearchAccountQueryHandler> logger
    ) : IQueryHandler<SearchAccountQuery, Result<IEnumerable<AccountResult>>>
    {
        public async Task<Result<IEnumerable<AccountResult>>> HandleAsync(
            SearchAccountQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var normalizedQuery = query.Query.Trim();
            var queryHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(normalizedQuery))
            );
            var cacheKey = $"auth:accounts:search:{queryHash}";
            var cachedAccounts = await cacheService.GetAsync<List<AccountResult>>(
                cacheKey,
                cancellationToken
            );

            if (cachedAccounts is not null)
            {
                logger.LogDebug("Authentication account search cache hit for {Query}", normalizedQuery);
                return Result<IEnumerable<AccountResult>>.Success(
                    ApplicationStatus.Success,
                    cachedAccounts
                );
            }

            var accountList = await dbContext
                .Accounts.Where(a =>
                    a.Email.Contains(normalizedQuery)
                    || a.Phone.Contains(normalizedQuery)
                    || a.Id.ToString().Equals(normalizedQuery)
                )
                .AsNoTracking()
                .Select(a => new AccountResult
                {
                    Id = a.Id,
                    Email = a.Email,
                    Phone = a.Phone,
                    Role = a.Role,
                    IsEmailConfirmed = a.IsEmailConfirmed,
                    Status = a.Status,
                })
                .ToListAsync(cancellationToken);

            logger.LogInformation(
                "Authentication account search for {Query} returned {Count} result(s)",
                query.Query,
                accountList.Count
            );

            await cacheService.SetAsync(
                cacheKey,
                accountList,
                TimeSpan.FromMinutes(2),
                cancellationToken
            );

            return Result<IEnumerable<AccountResult>>.Success(
                ApplicationStatus.Success,
                accountList
            );
        }
    }
}
