using Respira.Authentication.Infrastructure.Authentication;

namespace Respira.Authentication.Infrastructure.Test.Authentication;

public class HashServiceTest
{
    private readonly HashService _service = new();

    #region HashPassword & VerifyPassword

    [Fact]
    public void HashPassword_ValidPassword_ReturnsNonEmptyBcryptHash()
    {
        var hash = _service.HashPassword("StrongPass@123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("StrongPass@123", hash);
        Assert.StartsWith("$2", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_GeneratesSaltedDistinctHashes()
    {
        var first = _service.HashPassword("StrongPass@123");
        var second = _service.HashPassword("StrongPass@123");

        Assert.NotEqual(first, second);
        Assert.True(_service.VerifyPassword("StrongPass@123", first));
        Assert.True(_service.VerifyPassword("StrongPass@123", second));
    }

    [Fact]
    public void HashPassword_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.HashPassword(null!));
        Assert.Throws<ArgumentException>(() => _service.HashPassword(string.Empty));
    }

    [Fact]
    public void VerifyPassword_MatchingPassword_ReturnsTrue()
    {
        var hash = _service.HashPassword("DoctorSecret#1");

        Assert.True(_service.VerifyPassword("DoctorSecret#1", hash));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _service.HashPassword("DoctorSecret#1");

        Assert.False(_service.VerifyPassword("WrongPassword#1", hash));
    }

    [Fact]
    public void VerifyPassword_MalformedSaltHash_CatchesSaltParseExceptionAndReturnsFalse()
    {
        Assert.False(_service.VerifyPassword("DoctorSecret#1", "not-a-valid-bcrypt-hash"));
    }

    [Fact]
    public void VerifyPassword_NullOrEmptyArguments_ThrowsArgumentException()
    {
        var hash = _service.HashPassword("DoctorSecret#1");

        Assert.Throws<ArgumentNullException>(() => _service.VerifyPassword(null!, hash));
        Assert.Throws<ArgumentException>(() => _service.VerifyPassword(string.Empty, hash));
        Assert.Throws<ArgumentNullException>(() => _service.VerifyPassword("DoctorSecret#1", null!));
        Assert.Throws<ArgumentException>(() => _service.VerifyPassword("DoctorSecret#1", string.Empty));
    }

    #endregion

    #region HashToken & VerifyToken

    [Fact]
    public void HashToken_ValidToken_ReturnsDeterministic64CharHexSha256()
    {
        var first = _service.HashToken("sample-refresh-token-value");
        var second = _service.HashToken("sample-refresh-token-value");

        Assert.Equal(64, first.Length);
        Assert.Equal(first, second);
        Assert.All(first, ch => Assert.True(Uri.IsHexDigit(ch)));
    }

    [Fact]
    public void HashToken_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.HashToken(null!));
        Assert.Throws<ArgumentException>(() => _service.HashToken(string.Empty));
    }

    [Fact]
    public void VerifyToken_MatchingTokenAndHash_ReturnsTrue()
    {
        var hash = _service.HashToken("my-token-123");

        Assert.True(_service.VerifyToken("my-token-123", hash));
        Assert.True(_service.VerifyToken("my-token-123", hash.ToLowerInvariant()));
    }

    [Fact]
    public void VerifyToken_MismatchingToken_ReturnsFalse()
    {
        var hash = _service.HashToken("my-token-123");

        Assert.False(_service.VerifyToken("other-token-456", hash));
    }

    [Fact]
    public void VerifyToken_MalformedHexHash_CatchesFormatExceptionAndReturnsFalse()
    {
        Assert.False(_service.VerifyToken("my-token-123", "not-hex-characters-zzzz!"));
        Assert.False(_service.VerifyToken("my-token-123", "ABC")); // odd-length hex string
    }

    [Fact]
    public void VerifyToken_NullOrEmptyArguments_ThrowsArgumentException()
    {
        var hash = _service.HashToken("my-token-123");

        Assert.Throws<ArgumentNullException>(() => _service.VerifyToken(null!, hash));
        Assert.Throws<ArgumentException>(() => _service.VerifyToken(string.Empty, hash));
        Assert.Throws<ArgumentNullException>(() => _service.VerifyToken("my-token-123", null!));
        Assert.Throws<ArgumentException>(() => _service.VerifyToken("my-token-123", string.Empty));
    }

    #endregion
}
