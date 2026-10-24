using MaleMove.Application.Map;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[Authorize(Policy = "PlannerRead")]
public sealed class MapController(IMapService map) : BaseApiController
{
    [HttpGet("roads")]
    public async Task<IActionResult> Roads([FromQuery] double west, [FromQuery] double south,
        [FromQuery] double east, [FromQuery] double north, CancellationToken cancellationToken) =>
        Success(await map.GetRoadsAsync(new MapBounds(west, south, east, north), cancellationToken));

    [HttpGet("roadworks")]
    public async Task<IActionResult> Roadworks([FromQuery] double west, [FromQuery] double south,
        [FromQuery] double east, [FromQuery] double north, [FromQuery] DateTimeOffset? at,
        CancellationToken cancellationToken) =>
        Success(await map.GetRoadworksAsync(new MapBounds(west, south, east, north), at ?? DateTimeOffset.UtcNow, cancellationToken));
}

