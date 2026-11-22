using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Respira.Gateway.Identity;
using Respira.ServiceDefaults.Extensions;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// Get JWT configuration
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
// The gateway owns authentication: every proxied route requires a validated token
// unless its route configuration opts out with the "Anonymous" policy. Downstream
// services then only have to apply their role matrix.
builder
    .Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()
    )
    .AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));

// Get CORS configuration
var origins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed origins is not configured");
var methods = builder.Configuration.GetSection("CORS:AllowedMethods").Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed methods is not configured");
// Cannot use empty list for headers, since custom header like X-ID, X-Role, X-Email
// will be blocked, which result in auth failure
var headers = builder.Configuration.GetSection("CORS:AllowedHeaders").Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed headers is not configured");

builder.Services.AddCors(options =>
{
    options.AddPolicy("RespiraAMS-CORS-policy", policy =>
    {
        policy.WithOrigins(origins);
        policy.WithMethods(methods);
        policy.WithHeaders(headers);
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(
            clientIp,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
            }
        );
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder
    .Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .ConfigureHttpClient(
        (_, handler) =>
        {
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5);
            handler.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2);
        }
    )
    .AddTransforms(transformBuilderContext =>
    {
        // Discard any identity headers sent by the client and rebuild them from the
        // validated principal. The downstream services trust these headers, so the
        // gateway must be their only author.
        transformBuilderContext.AddRequestTransform(context =>
        {
            GatewayIdentityHeaders.Apply(context.HttpContext.User, context.ProxyRequest.Headers);
            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

// CORS has to run before authentication: browser preflights carry no credentials,
// so they must be answered even though every proxied route requires a token.
app.UseCors("RespiraAMS-CORS-policy");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapReverseProxy();
app.MapDefaultEndpoints();

await app.RunAsync();

/// <summary>Entry point type exposed so the integration tests can host the gateway.</summary>
public partial class Program;
