using Microsoft.EntityFrameworkCore;
using Respira.Authentication.Application.Contracts.Data;
using Respira.Authentication.Domain.Entities;

namespace Respira.Authentication.Infrastructure.Persistence
{
    public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
        : DbContext(options),
            IAuthDbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<BlacklistToken> BlacklistTokens { get; set; }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default
        )
        {
            NormalizeDateTimeOffsets();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void NormalizeDateTimeOffsets()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
                    {
                        property.CurrentValue = dto.ToUniversalTime();
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<Account>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Account>().Property(x => x.Role).HasConversion<string>();
            modelBuilder.Entity<Account>().Property(x => x.Status).HasConversion<string>();

            modelBuilder.Entity<Token>().HasIndex(x => x.HashToken).IsUnique();
            modelBuilder
                .Entity<Token>()
                .HasOne(x => x.Account)
                .WithMany(x => x.Tokens)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Token>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Token>().Property(x => x.TokenType).HasConversion<string>();
            modelBuilder.Entity<Token>().HasIndex(x => x.AccountId);
            modelBuilder.Entity<Token>().HasIndex(x => new { x.AccountId, x.TokenType });
            modelBuilder.Entity<Token>().HasIndex(x => x.ExpirationDate);

            modelBuilder.Entity<BlacklistToken>().HasIndex(x => x.HashToken).IsUnique();
            modelBuilder.Entity<BlacklistToken>().HasIndex(x => x.ExpirationDate);
        }
    }
}
