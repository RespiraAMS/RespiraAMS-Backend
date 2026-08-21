using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Token type for the AuthDoctor
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TokenType
    {
        /// <summary>
        /// Refresh token
        /// </summary>
        RefreshToken,

        /// <summary>
        /// Access token
        /// </summary>
        AccessToken,

        /// <summary>
        /// Email verification token
        /// </summary>
        EmailVerificationToken,

        /// <summary>
        /// Password reset token
        /// </summary>
        PasswordResetToken,
    }
}
