using Application.Contracts.Services;
using Infrastructure.Services.Email;
using Infrastructure.Services.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.Services.AddScoped<IExportService, ExportService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
        }

    }
}
