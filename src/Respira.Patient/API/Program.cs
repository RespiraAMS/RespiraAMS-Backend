using Respira.Patient.DI;
using Respira.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn = builder.Configuration.GetConnectionString("patientDb") ?? throw new InvalidOperationException("No connection string found");

// Add error handling
builder.Services.AddCustomErrorHandling();

// Add service discovery
builder.AddServiceDefaults();

// DI registration
builder.AddDI();

var app = builder.Build();

app.ApplyMigrations(app.Environment.IsDevelopment());

app.Run();
