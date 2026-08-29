using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Types of tokens issued to authenticated users.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TokenType
    {
        /// <summary>Long-lived token used to obtain new access tokens.</summary>
        RefreshToken,

        /// <summary>Short-lived token authorizing API access.</summary>
        AccessToken,

        /// <summary>Token used to confirm a doctor's email address.</summary>
        EmailVerificationToken,

        /// <summary>Token used to authorize a password reset.</summary>
        PasswordResetToken,
    }
}
