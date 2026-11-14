namespace Respira.Authentication.Domain.Policies;

/// <summary>
/// Rules a password has to satisfy before the system accepts it. The rules are
/// applied whenever a new password is chosen (registration and password reset).
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Minimum number of characters a password must have.</summary>
    public const int MinimumLength = 8;

    /// <summary>Maximum number of characters a password may have.</summary>
    public const int MaximumLength = 128;

    /// <summary>
    /// Checks a password against the policy.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>The list of rules the password breaks; empty when the password is valid.</returns>
    public static IReadOnlyList<string> GetViolations(string? password)
    {
        var violations = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            violations.Add("A password is required");
            return violations;
        }

        if (password.Length < MinimumLength)
        {
            violations.Add($"The password must be at least {MinimumLength} characters long");
        }

        if (password.Length > MaximumLength)
        {
            violations.Add($"The password must not be longer than {MaximumLength} characters");
        }

        if (!password.Any(char.IsLetter))
        {
            violations.Add("The password must contain at least one letter");
        }

        if (!password.Any(char.IsDigit))
        {
            violations.Add("The password must contain at least one digit");
        }

        return violations;
    }

    /// <summary>
    /// Checks whether a password satisfies the policy.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>True when the password is valid, false otherwise.</returns>
    public static bool IsValid(string? password) => GetViolations(password).Count == 0;
}
