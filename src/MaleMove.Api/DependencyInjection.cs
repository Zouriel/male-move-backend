using System.Threading.RateLimiting;
using MaleMove.Api.Development;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MaleMove.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMaleMoveApi(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddHttpContextAccessor();

        var authority = configuration["Authentication:Authority"];
        var audience = configuration["Authentication:Audience"];
        var developmentTokens = DevelopmentTokenOptions.From(configuration, environment);
        if (developmentTokens is null && !environment.IsDevelopment() && (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience)))
            throw new InvalidOperationException("Authentication:Authority and Authentication:Audience are required outside Development.");
        if (developmentTokens is not null) services.AddSingleton(developmentTokens);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters.RoleClaimType = "role";
            options.TokenValidationParameters.NameClaimType = "name";
            if (developmentTokens is not null)
            {
                // Local development only: tokens come from DevTokenController instead of an OIDC issuer.
                options.TokenValidationParameters.ValidIssuer = DevelopmentTokenOptions.Issuer;
                options.TokenValidationParameters.ValidAudience = developmentTokens.Audience;
                options.TokenValidationParameters.IssuerSigningKey = developmentTokens.SigningKey;
                return;
            }
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build();
            options.AddPolicy("PlannerRead", policy => policy.RequireAuthenticatedUser().RequireRole("Administrator", "DataSteward", "PermitOfficer", "PermitApprover", "Planner"));
            options.AddPolicy("PermitOfficer", policy => policy.RequireAuthenticatedUser().RequireRole("Administrator", "PermitOfficer"));
            options.AddPolicy("PermitApprover", policy => policy.RequireAuthenticatedUser().RequireRole("Administrator", "PermitApprover"));
            options.AddPolicy("Administrator", policy => policy.RequireAuthenticatedUser().RequireRole("Administrator"));
        });

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (!environment.IsDevelopment() && allowedOrigins.Length == 0)
            throw new InvalidOperationException("At least one Cors:AllowedOrigins value is required outside Development.");
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length == 0 && environment.IsDevelopment())
                policy.WithOrigins("http://localhost:4200");
            else
                policy.WithOrigins(allowedOrigins);
            policy.AllowAnyHeader().AllowAnyMethod();
        }));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "authenticated"
                    : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        return services;
    }
}

