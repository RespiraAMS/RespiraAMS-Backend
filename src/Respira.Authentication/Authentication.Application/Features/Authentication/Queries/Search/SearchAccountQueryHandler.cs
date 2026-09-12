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
        ILogger<SearchAccountQueryHandler> logger
    ) : IQueryHandler<SearchAccountQuery, Result<IEnumerable<AccountResult>>>
    {
        public async Task<Result<IEnumerable<AccountResult>>> HandleAsync(
            SearchAccountQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var accountList = await dbContext
                .Accounts.Where(a =>
                    a.Email.Contains(query.Query)
                    || a.Phone.Contains(query.Query)
                    || a.Id.ToString().Equals(query.Query)
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

            return Result<IEnumerable<AccountResult>>.Success(
                ApplicationStatus.Success,
                accountList
            );
        }
    }
}
