using Microsoft.EntityFrameworkCore;
using Respira.Authentication.Application.Constracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Accounts;
using Respira.ServiceDefaults.Contracts.Results;

namespace Authentication.Application.Features.Accounts.Queries.GetByIds
{
    /// <summary>
    /// Answers account lookups sent by other microservices over the message broker.
    /// </summary>
    public class GetAccountsByIdsQueryHandler(IAuthDbContext dbContext)
        : IQueryHandler<GetAccountsByIdsQuery, Result<List<AccountSummary>>>
    {
        public async Task<Result<List<AccountSummary>>> HandleAsync(
            GetAccountsByIdsQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            if (query.AccountIds.Length == 0)
            {
                return Result<List<AccountSummary>>.Success(ApplicationStatus.Success, []);
            }

            // Project the enum columns first, then convert them to strings in memory:
            // Enum.ToString() is not translatable to SQL.
            var accounts = await dbContext
                .Accounts.AsNoTracking()
                .Where(x => query.AccountIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Email,
                    x.Phone,
                    x.Role,
                    x.Status,
                })
                .ToListAsync(cancellationToken);

            var result = accounts.ConvertAll(x => new AccountSummary
            {
                Id = x.Id,
                Email = x.Email,
                Phone = x.Phone,
                Role = x.Role.ToString(),
                Status = x.Status.ToString(),
            });

            return Result<List<AccountSummary>>.Success(ApplicationStatus.Success, result);
        }
    }
}
