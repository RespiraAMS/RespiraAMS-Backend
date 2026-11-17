using System.Text.Json;
using Respira.Authentication.Domain.Entities;
using Respira.Authentication.Domain.Enums;

namespace Respira.Authentication.Infrastructure.Data;

/// <summary>
/// Reads the accounts described by the seed data file. The file is read while the
/// EF Core model is built, so the accounts end up in the model as seed data and are
/// applied by the migrations like any other schema change.
/// </summary>
public static class AccountSeedDataLoader
{
    /// <summary>Path of the seed data file, relative to the application base directory.</summary>
    public const string DefaultFilePath = "Data/seed-accounts.jsonc";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads the accounts described by the seed data file.
    /// </summary>
    /// <param name="filePath">Path of the seed data file, relative to the application base directory.</param>
    /// <returns>
    /// The accounts described by the file; empty when the file does not exist, so the
    /// service still starts when the seed data is not deployed.
    /// </returns>
    public static IReadOnlyList<Account> Load(string filePath)
    {
        var path = ResolvePath(filePath);

        if (path is null)
        {
            return [];
        }

        var json = File.ReadAllText(path);

        var seedData =
            JsonSerializer.Deserialize<AccountSeedData>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize the seed data file '{path}'."
            );

        return seedData.Accounts.Select(MapToDomain).ToList();
    }

    /// <summary>
    /// Looks for the seed data file next to the application binaries first, then in
    /// the working directory, so the file is found both at runtime and while the
    /// migrations are scaffolded.
    /// </summary>
    private static string? ResolvePath(string filePath)
    {
        var candidates = new List<string>();

        if (Path.IsPathRooted(filePath))
        {
            candidates.Add(filePath);
        }
        else
        {
            candidates.Add(Path.Combine(AppContext.BaseDirectory, filePath));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), filePath));
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Account MapToDomain(AccountSeedDataItem item)
    {
        if (!Guid.TryParse(item.Id, out var id))
        {
            throw new InvalidOperationException(
                $"Seed data: the account '{item.Email}' has an invalid id '{item.Id}'."
            );
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            throw new InvalidOperationException("Seed data: an account is missing its email address.");
        }

        if (string.IsNullOrWhiteSpace(item.PasswordHash))
        {
            throw new InvalidOperationException(
                $"Seed data: the account '{item.Email}' is missing its password hash."
            );
        }

        return new Account
        {
            Id = id,
            Email = item.Email.Trim().ToLowerInvariant(),
            HashPassword = item.PasswordHash,
            Phone = item.Phone,
            Role = ParseEnum(item.Role, RoleType.Doctor),
            Status = ParseEnum(item.Status, StatusType.Active),
            IsEmailConfirmed = item.IsEmailConfirmed,
            IsDeleted = item.IsDeleted,
            CreatedAt = item.CreatedAt.ToUniversalTime(),
            UpdatedAt = item.UpdatedAt.ToUniversalTime(),
            DeletedAt = null,
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }
}
