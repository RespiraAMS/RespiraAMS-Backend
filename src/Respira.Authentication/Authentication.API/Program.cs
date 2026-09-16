using Authentication.API.Extensions;
using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Constracts.Email;
using Authentication.Application.Features.Authentication.Commands.Login;
using Authentication.Infrastructure.Extensions;
using Authentication.Infrastructure.Persistence;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services
    .AddOptions<JwtOption>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<EmailOption>()
    .BindConfiguration(EmailOption.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.AddNpgsqlDbContext<AuthDbContext>("authdb");
builder.Services.AddScoped<IAuthDbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<AuthDbContext>()
);
builder.AddRedisClient("cache");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("cache");
    options.InstanceName = "respira-auth:";
});
builder.Services
    .AddFusionCache()
    .WithSystemTextJsonSerializer()
    .WithRegisteredDistributedCache()
    .WithDefaultEntryOptions(options =>
    {
        options.Duration = TimeSpan.FromMinutes(2);
        options.IsFailSafeEnabled = true;
    });
builder.Services.AddAuthenticationInfrastructure();

builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(LoginCommand).Assembly);
});

var app = builder.Build();

await app.ApplyMigrationsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
