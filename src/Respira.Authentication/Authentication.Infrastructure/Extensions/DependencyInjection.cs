using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Email;
using Authentication.Infrastructure.Authentication;
using Authentication.Infrastructure.Cache;
using Authentication.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Infrastructure.Extensions
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
