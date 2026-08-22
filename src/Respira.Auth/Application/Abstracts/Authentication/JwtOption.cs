namespace Application.Abstracts.Authentication
{
    /// <summary>
    /// JWT configuration bound from the "Jwt" configuration section.
    /// </summary>
    public class JwtOption
    {
        /// <summary>Signing secret (HMAC-SHA256 key)</summary>
        public required string Secret { get; set; }

        /// <summary>Expected token issuer</summary>
        public required string Issuer { get; set; }

        /// <summary>Expected token audience</summary>
        public required string Audience { get; set; }

        /// <summary>Access token lifetime in minutes</summary>
        public int AccessTokenExpired { get; set; }

        /// <summary>Refresh token lifetime in minutes</summary>
        public int RefreshTokenExpired { get; set; }
    }
}
