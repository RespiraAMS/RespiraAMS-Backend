using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Types of tokens issued to authenticated users.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TokenType
    {
        RefreshToken,
        AccessToken,
        EmailVerificationToken,
        PasswordResetToken,
    }
}
