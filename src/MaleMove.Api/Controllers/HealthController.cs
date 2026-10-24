using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MaleMove.Infrastructure.Persistence;

namespace MaleMove.Api.Controllers;

[AllowAnonymous]
[Route("api/v1/health")]
public sealed class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? Ok(new { status = "ready" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "not_ready" });
}

