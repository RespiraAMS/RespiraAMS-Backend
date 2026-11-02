using Microsoft.Extensions.DependencyInjection;
using Respira.Authentication.Application.Constracts.Authentication;
using Respira.Authentication.Application.Constracts.Cache;
using Respira.Authentication.Application.Constracts.Email;
using Respira.Authentication.Infrastructure.Authentication;
using Respira.Authentication.Infrastructure.Cache;
using Respira.Authentication.Infrastructure.Email;

namespace Respira.Authentication.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers infrastructure implementations that do not depend on a hosting provider.
        /// DbContext and distributed cache registrations live in the API composition root.
        /// </summary>
        public static IServiceCollection AddAuthenticationInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IHashService, HashService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddSingleton<ICacheService, CacheService>();
            return services;
        }
    }
}
