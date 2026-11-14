namespace Respira.Authentication.Infrastructure.Data;

/// <summary>
/// Shape of the accounts section of the seed data file.
/// </summary>
public sealed class AccountSeedData
{
    public ICollection<AccountSeedDataItem> Accounts { get; init; } = [];
}

/// <summary>
/// An account described by the seed data file. The file is the single source of truth
/// for the accounts EF Core seeds through the migrations, so the values have to be
/// stable: a change to the file requires a new migration.
/// </summary>
public sealed class AccountSeedDataItem
{
    /// <summary>Identifier of the account.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Login address of the account.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// BCrypt hash of the password. The clear text password never reaches the file,
    /// so it cannot be read back from the repository.
    /// </summary>
    public string PasswordHash { get; init; } = string.Empty;

    /// <summary>Contact phone number.</summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>Role of the account (Doctor, HeadDepartmentDoctor, MedicalRegulationReviewer, Admin).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Status of the account (Active / Inactive).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Whether the email address is already confirmed.</summary>
    public bool IsEmailConfirmed { get; init; }

    /// <summary>Whether the account is soft deleted.</summary>
    public bool IsDeleted { get; init; }

    /// <summary>Creation timestamp of the account.</summary>
    public DateTimeOffset CreatedAt { get; init; } = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Last update timestamp of the account.</summary>
    public DateTimeOffset UpdatedAt { get; init; } = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
