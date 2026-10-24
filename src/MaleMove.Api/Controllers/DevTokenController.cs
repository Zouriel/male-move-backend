using MaleMove.Api.Development;
using MaleMove.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MaleMove.Api.Controllers;

/// <summary>Issues short-lived local tokens. Returns 404 unless the development issuer is configured.</summary>
[AllowAnonymous]
[Route("api/v1/dev/token")]
public sealed class DevTokenController(IServiceProvider services) : BaseApiController
{
    private static readonly Guid DevelopmentUserId = Guid.Parse("00000000-0000-4000-8000-000000000001");

    [HttpPost]
    public IActionResult Issue([FromQuery] string role = "Administrator")
    {
        var options = services.GetService<DevelopmentTokenOptions>();
        if (options is null) return NotFound();
        if (!DevelopmentTokenOptions.Roles.Contains(role, StringComparer.Ordinal))
            throw new AppException($"Role must be one of: {string.Join(", ", DevelopmentTokenOptions.Roles)}.", 400, "role_invalid");

        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = DevelopmentTokenOptions.Issuer,
            Audience = options.Audience,
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = DevelopmentUserId.ToString(),
                ["name"] = $"Local {role}",
                ["role"] = role,
            },
            SigningCredentials = new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256),
        });
        return Success(new { accessToken = token, expiresAt = expires, role });
    }
}
