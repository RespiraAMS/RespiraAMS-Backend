using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database
{
    /// <summary>
    /// EF Core DbContext for the Auth database
    /// </summary>
    /// <param name="options">DbContext options (connection string, etc.)</param>
    public class AuthDbContext(DbContextOptions<AuthDbContext> options)
        : DbContext(options),
            IAuthDbContext
    {
        /// <summary>
        /// Tokens stored by the Auth service (refresh tokens, verification tokens, ...)
        /// </summary>
        public DbSet<Token> Tokens { get; set; }

        /// <summary>
        /// Revoked tokens (blacklist)
        /// </summary>
        public DbSet<BlacklistToken> BlacklistTokens { get; set; }

        /// <summary>
        /// Registered doctor accounts
        /// </summary>
        public DbSet<AuthDoctor> AuthDoctors { get; set; }

        /// <summary>
        /// Saves changes without a cancellation token (required by <see cref="IAuthDbContext"/>)
        /// </summary>
        /// <returns>Number of affected rows</returns>
        public Task<int> SaveChangesAsync() => base.SaveChangesAsync();

        /// <summary>
        /// Configures entity mappings: table names, indexes, soft-delete filters, enum conversions
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuthDoctor>().ToTable("auth_doctors");
            modelBuilder.Entity<AuthDoctor>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<AuthDoctor>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<AuthDoctor>().Property(x => x.Role).HasConversion<string>();
            modelBuilder.Entity<AuthDoctor>().Property(x => x.Status).HasConversion<string>();

            modelBuilder.Entity<Token>().ToTable("tokens");
            modelBuilder.Entity<Token>().HasIndex(x => x.HashToken).IsUnique();
            modelBuilder
                .Entity<Token>()
                .HasOne(x => x.AuthDoctor)
                .WithMany(x => x.Tokens)
                .HasForeignKey(x => x.AuthUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Token>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Token>().Property(x => x.TokenType).HasConversion<string>();
            modelBuilder.Entity<Token>().HasIndex(x => x.AuthUserId);
            modelBuilder.Entity<Token>().HasIndex(x => new { x.AuthUserId, x.TokenType });
            modelBuilder.Entity<Token>().HasIndex(x => x.ExpirationDate);

            modelBuilder.Entity<BlacklistToken>().ToTable("blacklist_tokens");
            modelBuilder.Entity<BlacklistToken>().HasIndex(x => x.HashToken).IsUnique();
            modelBuilder.Entity<BlacklistToken>().HasIndex(x => x.ExpirationDate);
        }
    }
}
