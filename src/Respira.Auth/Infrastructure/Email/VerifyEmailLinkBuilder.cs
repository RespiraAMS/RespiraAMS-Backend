using Application.Abstracts.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email
{
    /// <summary>
    /// Resolves the public gateway base URL from the Aspire service discovery
    /// environment variables (injected by <c>WithReference(gateway)</c>), falling back to the
    /// <c>VerifyEmail:BaseUrl</c> configuration value when running outside Aspire
    /// </summary>
    public sealed class VerifyEmailLinkBuilder : IVerifyEmailLinkBuilder
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VerifyEmailLinkBuilder> _logger;

        public VerifyEmailLinkBuilder(IConfiguration configuration, ILogger<VerifyEmailLinkBuilder> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string Build(string token, string email)
        {
            var gateway = _configuration["services__gateway__http__0"]
                ?? _configuration["services__gateway__https__0"]
                ?? _configuration["VerifyEmail:BaseUrl"];

            if (string.IsNullOrWhiteSpace(gateway))
            {
                _logger.LogWarning(
                    "Gateway base URL not found in service discovery or configuration; verify-email links will be unusable");
                gateway = "http://gateway";
            }

            return $"{gateway.TrimEnd('/')}/api/v1/auth/verify-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        }
    }
}
