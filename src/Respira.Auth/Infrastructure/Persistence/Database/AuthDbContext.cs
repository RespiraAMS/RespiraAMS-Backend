using Application.Abstracts.Data;
using Domain.Entities;
using Domain.Enums;
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
        public Task<int> SaveChangesAsync() => SaveChangesAsync(default);

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
                .Entity<AuthDoctor>()
                .HasData(
                    new AuthDoctor
                    {
                        Id = adminId,
                        Email = adminEmail,
                        // BCrypt hash of "Admin@123" (work factor 12), generated with BCrypt.Net-Next.
                        HashPassword = "$2a$12$RYGvxowi6VHTYi6qMXQ7ROTagbu9XS58dlqtQdSjp1AMWQ1T6dR4C",
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
