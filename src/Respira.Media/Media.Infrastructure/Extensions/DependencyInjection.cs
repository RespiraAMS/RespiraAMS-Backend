using Amazon.S3;
using Media.Application.Constracts.Data;
using Media.Application.Constracts.Storage;
using Media.Infrastructure.Persistence;
using Media.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Media.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers infrastructure implementations that do not depend on a hosting provider.
        /// DbContext registration lives in the API composition root.
        /// </summary>
        public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IMediaDbContext>(sp =>
                sp.GetRequiredService<MediaDbContext>()
            );
            services.AddScoped<IStorageService, StorageService>();
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<R2Options>>()
                    .Value;

                var config = new AmazonS3Config
                {
                    ServiceURL = options.Endpoint,
                    ForcePathStyle = true,
                    UseAccelerateEndpoint = false,
                };

                return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
            });

            return services;
        }
    }
}
