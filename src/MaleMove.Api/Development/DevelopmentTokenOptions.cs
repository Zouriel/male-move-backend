using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MaleMove.Api.Development;

/// <summary>
/// Local token issuer settings. Only created in the Development or Staging environment when
/// Authentication:DevelopmentSigningKey is set; it never replaces OIDC in Production.
/// </summary>
public sealed class DevelopmentTokenOptions
{
    public const string Issuer = "male-move-development";
    public static readonly string[] Roles = ["Administrator", "DataSteward", "PermitOfficer", "PermitApprover", "Planner"];

    public required SymmetricSecurityKey SigningKey { get; init; }
    public required string Audience { get; init; }

    public static DevelopmentTokenOptions? From(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var key = configuration["Authentication:DevelopmentSigningKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (!environment.IsDevelopment() && !environment.IsStaging())
            throw new InvalidOperationException("Authentication:DevelopmentSigningKey is only allowed in the Development and Staging environments.");
        var bytes = Encoding.UTF8.GetBytes(key);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Authentication:DevelopmentSigningKey must be at least 32 bytes.");
        return new DevelopmentTokenOptions
        {
            SigningKey = new SymmetricSecurityKey(bytes),
            Audience = configuration["Authentication:Audience"] ?? "male-move-api",
        };
    }
}
