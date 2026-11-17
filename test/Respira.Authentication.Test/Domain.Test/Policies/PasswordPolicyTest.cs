using Respira.Authentication.Domain.Policies;

namespace Respira.Authentication.Domain.Test.Policies;

public sealed class PasswordPolicyTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetViolations_NullOrWhitespacePassword_ReturnsRequiredViolationOnly(
        string? password
    )
    {
        var violations = PasswordPolicy.GetViolations(password);

        var violation = Assert.Single(violations);
        Assert.Equal("A password is required", violation);
        Assert.False(PasswordPolicy.IsValid(password));
    }

    [Theory]
    [InlineData("a1")]
    [InlineData("abc1234")]
    public void GetViolations_PasswordShorterThanMinimumLength_ReturnsMinimumLengthViolation(
        string password
    )
    {
        var violations = PasswordPolicy.GetViolations(password);

        var violation = Assert.Single(violations);
        Assert.Equal(
            $"The password must be at least {PasswordPolicy.MinimumLength} characters long",
            violation
        );
        Assert.False(PasswordPolicy.IsValid(password));
    }

    [Fact]
    public void GetViolations_PasswordLongerThanMaximumLength_ReturnsMaximumLengthViolation()
    {
        var password = new string('a', PasswordPolicy.MaximumLength) + "1";

        var violations = PasswordPolicy.GetViolations(password);

        var violation = Assert.Single(violations);
        Assert.Equal(
            $"The password must not be longer than {PasswordPolicy.MaximumLength} characters",
            violation
        );
        Assert.False(PasswordPolicy.IsValid(password));
    }

    [Fact]
    public void GetViolations_PasswordWithoutLetter_ReturnsMissingLetterViolation()
    {
        var violations = PasswordPolicy.GetViolations("12345678");

        var violation = Assert.Single(violations);
        Assert.Equal("The password must contain at least one letter", violation);
        Assert.False(PasswordPolicy.IsValid("12345678"));
    }

    [Fact]
    public void GetViolations_PasswordWithoutDigit_ReturnsMissingDigitViolation()
    {
        var violations = PasswordPolicy.GetViolations("abcdefgh");

        var violation = Assert.Single(violations);
        Assert.Equal("The password must contain at least one digit", violation);
        Assert.False(PasswordPolicy.IsValid("abcdefgh"));
    }

    [Fact]
    public void GetViolations_ShortPasswordWithoutLetterOrDigit_ReturnsAllApplicableViolations()
    {
        var violations = PasswordPolicy.GetViolations("!!!");

        Assert.Equal(3, violations.Count);
        Assert.Contains(
            $"The password must be at least {PasswordPolicy.MinimumLength} characters long",
            violations
        );
        Assert.Contains("The password must contain at least one letter", violations);
        Assert.Contains("The password must contain at least one digit", violations);
    }

    [Theory]
    [InlineData("Admin@123")]
    [InlineData("abcd1234")]
    [InlineData("DoctorPass99!")]
    public void IsValid_CompliantPassword_ReturnsTrueAndEmptyViolations(string password)
    {
        Assert.True(PasswordPolicy.IsValid(password));
        Assert.Empty(PasswordPolicy.GetViolations(password));
    }

    [Fact]
    public void IsValid_PasswordAtExactMaximumLength_ReturnsTrue()
    {
        var password = new string('a', PasswordPolicy.MaximumLength - 1) + "1";

        Assert.Equal(PasswordPolicy.MaximumLength, password.Length);
        Assert.True(PasswordPolicy.IsValid(password));
        Assert.Empty(PasswordPolicy.GetViolations(password));
    }
}
