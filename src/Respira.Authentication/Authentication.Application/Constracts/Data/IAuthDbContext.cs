using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Constracts.Data
{
    public interface IAuthDbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<BlacklistToken> BlacklistTokens { get; set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
