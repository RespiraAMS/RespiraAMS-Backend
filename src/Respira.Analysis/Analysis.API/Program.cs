using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Commands.Create;
using Respira.Analysis.Infrastructure.Data;
using Respira.Analysis.Infrastructure.Extensions;
using Respira.Analysis.API.Auditing;
using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Messages.Patients;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddApiVersion();
builder.Services.AddCustomErrorHandling();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Respira";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Respira";
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

builder.AddNpgsqlDbContext<AnalysisDbContext>("analysisdb");
builder.Services.AddScoped<IAnalysisDbContext>(services =>
    services.GetRequiredService<AnalysisDbContext>()
);
builder.Services.AddAnalysisExcelExport();
builder.Services.AddAuditLogging();
builder.Services.AddScoped<IAuditEventPublisher, AnalysisAuditEventPublisher>();
builder.Services.AddHostedService<AuditOutboxForwarder<AnalysisDbContext>>();

var analysisConnection = builder.Configuration.GetConnectionString("analysisdb")
    ?? throw new InvalidOperationException("Connection string 'analysisdb' is not configured");
var rabbitMqConnection = builder.Configuration.GetConnectionString("rabbitmq")
    ?? throw new InvalidOperationException("Connection string 'rabbitmq' is not configured");

builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(CreateAnalysisReportCommand).Assembly);
    options.CodeGeneration.AlwaysUseServiceLocationFor<IAnalysisDbContext>();
    options.PersistMessagesWithPostgresql(analysisConnection);
    options.UseRabbitMq(rabbitMqConnection).AutoProvision();
    options.PublishMessage<GetPatientsByDoctorQuery>()
        .ToRabbitQueue("respira.patient.analysis.queries");
    options.PublishMessage<AuditEvent>().ToRabbitQueue(AuditQueues.Events);
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
}

app.UseCustomErrorHandling();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseClaimsPropagation();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
