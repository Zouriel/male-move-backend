using MaleMove.Application.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[Authorize(Policy = "PlannerRead")]
public sealed class RoutesController(IRouteService routes) : BaseApiController
{
    /// <summary>Public: anyone can check a route. Rate limited per IP like every API route.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Calculate([FromBody] RouteRequestDto request, CancellationToken cancellationToken) =>
        Success(await routes.CalculateAsync(request, cancellationToken));
}

