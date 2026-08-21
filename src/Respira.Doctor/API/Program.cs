// Entry point for the Respira Doctor API: configures controllers, OpenAPI,
// EF Core persistence and the doctor infrastructure (caching + DB).
using Infrastructure;
using Respira.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add service discovery / telemetry defaults
builder.AddServiceDefaults();

// Add infrastructure (DB context, FusionCache)
builder.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.ApplyMigrations(app.Environment.IsDevelopment());

app.Run();
