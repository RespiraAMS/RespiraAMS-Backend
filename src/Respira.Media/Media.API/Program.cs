using Respira.Media.API.Extensions;
using Respira.Media.Application.Constracts.Storage;
using Respira.Media.Application.Features.MediaAssets.Create;
using Respira.Media.Infrastructure.Extensions;
using Respira.Media.Infrastructure.Persistence;
using Respira.ServiceDefaults.Extensions;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<R2Options>()
    .BindConfiguration(R2Options.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services.AddApiVersion();

builder.AddNpgsqlDbContext<MediaDbContext>("mediadb");
builder.Services.AddMediaInfrastructure();

// Discover command/query handlers in the Media.Application assembly (e.g. CreateMediaAssetCommandHandler)
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(CreateMediaAssetCommand).Assembly);
});

var app = builder.Build();

await app.ApplyMigrationsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();
