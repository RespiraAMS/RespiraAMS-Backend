using Authentication.Application.Constracts.Data;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence
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

            SeedAdminAccount(modelBuilder);
        }

        /// <summary>
        /// Seeds a bootstrap administrator account so the manager/admin flows (e.g. the
        /// Create/Update/Delete saga endpoints) can be exercised without first registering an
        /// account out-of-band. Idempotent: the row is keyed by a fixed Id, so re-running the
        /// migration only ever touches this one record.
        /// </summary>
        /// <param name="modelBuilder">Model builder.</param>
        private void SeedAdminAccount(ModelBuilder modelBuilder)
        {
            const string adminEmail = "admin@respira.ams";
            var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            modelBuilder
                .Entity<Account>()
                .HasData(
                    new Account
                    {
                        Id = adminId,
                        Email = adminEmail,
                        // BCrypt hash of "Admin@123" (work factor 12), generated with BCrypt.Net-Next.
                        HashPassword =
                            "$2a$12$RYGvxowi6VHTYi6qMXQ7ROTagbu9XS58dlqtQdSjp1AMWQ1T6dR4C",
                        Phone = "0000000000",
                        Role = RoleType.Admin,
                        IsEmailConfirmed = true,
                        Status = StatusType.Active,
                        CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UpdatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        IsDeleted = false,
                        DeletedAt = null,
                    }
                );
        }
    }
}
