using MaleMove.Application.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[Authorize(Policy = "PlannerRead")]
public sealed class RoutesController(IRouteService routes) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Calculate([FromBody] RouteRequestDto request, CancellationToken cancellationToken) =>
        Success(await routes.CalculateAsync(request, cancellationToken));
}

