using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstracts.Data
{
    /// <summary>
    /// Persistence abstraction for the Auth database, exposing the entity sets
    /// and a save method without leaking EF Core specifics to the Application layer.
    /// </summary>
    public interface IAuthDbContext
    {
        /// <summary>Issued tokens (refresh, verification, reset)</summary>
        public DbSet<Token> Tokens { get; set; }

        /// <summary>Revoked tokens (blacklist)</summary>
        public DbSet<BlacklistToken> BlacklistTokens { get; set; }

        /// <summary>Registered doctor accounts</summary>
        public DbSet<AuthDoctor> AuthDoctors { get; set; }

        /// <summary>Persists pending changes to the database</summary>
        /// <returns>Number of affected rows</returns>
        public Task<int> SaveChangesAsync();
    }
}
