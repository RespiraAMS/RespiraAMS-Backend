using Microsoft.EntityFrameworkCore;
using Respira.Authentication.Domain.Entities;

namespace Respira.Authentication.Application.Constracts.Data
{
    public interface IAuthDbContext
    {
        public DbSet<Account> Accounts { get; }
        public DbSet<Token> Tokens { get; }
        public DbSet<BlacklistToken> BlacklistTokens { get; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
