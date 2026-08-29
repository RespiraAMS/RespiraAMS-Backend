using Application.Abstracts.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email
{
    /// <summary>
    /// Resolves the Auth service base URL from the <c>VerifyEmail:BaseUrl</c> configuration
    /// (set this to the Auth service port when running outside Aspire), falling back to the
    /// Auth service Aspire service discovery endpoint, then the gateway.
    /// </summary>
    public sealed class VerifyEmailLinkBuilder(
        IConfiguration configuration,
        ILogger<VerifyEmailLinkBuilder> logger
    ) : IVerifyEmailLinkBuilder
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<VerifyEmailLinkBuilder> _logger = logger;

        public string Build(string token, string email)
        {
            var baseUrl =
                _configuration["VerifyEmail:BaseUrl"]
                ?? _configuration["services__auth-service__https__0"]
                ?? _configuration["services__auth-service__http__0"]
                ?? _configuration["services__gateway__https__0"]
                ?? _configuration["services__gateway__http__0"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogWarning(
                    "Auth service base URL not found in service discovery or configuration; verify-email links will be unusable"
                );
                baseUrl = "https://localhost:7050";
            }

            return $"{baseUrl.TrimEnd('/')}/api/1/auth/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        }
    }
}
