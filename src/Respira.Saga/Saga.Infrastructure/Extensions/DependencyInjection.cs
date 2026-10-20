using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respira.Saga.Application.Constracts.Media;
using Respira.Saga.Application.Constracts.Sagas;
using Respira.Saga.Infrastructure.Media;
using Respira.Saga.Infrastructure.Sagas;

namespace Respira.Saga.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the HTTP client used by the saga to upload the avatar to the Media
        /// microservice and the reader the status queries use to observe in-flight sagas.
        /// The base address comes from the <c>Media:BaseUrl</c> section, which the AppHost
        /// injects through the <c>Media__BaseUrl</c> environment variable.
        /// </summary>
        public static IServiceCollection AddSagaInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var baseUrl =
                configuration["Media:BaseUrl"]
                ?? throw new InvalidOperationException("Configuration 'Media:BaseUrl' is not configured");

            services.AddHttpClient<IMediaClient, MediaClient>(client =>
            {
                // Trailing slash so relative paths such as "api/v1/..." are appended
                // instead of replacing the last path segment.
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            });

            services.AddSingleton<ISagaStateReader, SagaStateReader>();

            return services;
        }
    }
}
