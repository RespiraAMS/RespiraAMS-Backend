using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Constracts.Data
{
    public interface IAuthDbContext
    {
        public DbSet<Account> Accounts { get; }
        public DbSet<Token> Tokens { get; }
        public DbSet<BlacklistToken> BlacklistTokens { get; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
