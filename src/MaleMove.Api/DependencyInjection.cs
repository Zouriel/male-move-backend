using System.Threading.RateLimiting;
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
        if (!environment.IsDevelopment() && (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience)))
            throw new InvalidOperationException("Authentication:Authority and Authentication:Audience are required outside Development.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.MapInboundClaims = false;
            options.TokenValidationParameters.RoleClaimType = "role";
            options.TokenValidationParameters.NameClaimType = "name";
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

