using System.ComponentModel.DataAnnotations;

namespace Respira.Authentication.Application.Contracts.Authentication
{
    public class JwtOption
    {
        public const string SectionName = "Jwt";

        [Required]
        public required string Issuer { get; set; }

        [Required]
        public required string Audience { get; set; }

        /// <summary>
        /// Key the tokens are signed with. Every service that validates the tokens
        /// (gateway, analysis, saga) reads the same value, so it is handed over by
        /// the AppHost.
        /// </summary>
        [Required]
        public required string Secret { get; set; }

        /// <summary>Lifetime of an access token, in minutes.</summary>
        [Range(1, 1440)]
        public required long AccessTokenExpires { get; set; }

        /// <summary>Lifetime of a refresh token, in days.</summary>
        [Range(1, 365)]
        public required long RefreshTokenExpires { get; set; }
    }
}
