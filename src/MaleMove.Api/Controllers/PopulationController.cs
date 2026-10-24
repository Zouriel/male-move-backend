using MaleMove.Application.Population;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[Authorize(Policy = "PlannerRead")]
public sealed class PopulationController(IPopulationService population) : BaseApiController
{
    [HttpGet("series")]
    public async Task<IActionResult> Get([FromQuery] int? fromYear, [FromQuery] int? toYear,
        [FromQuery] string? areaType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (fromYear > toYear) return BadRequest("fromYear must be less than or equal to toYear.");
        return Success(await population.GetAsync(fromYear, toYear, areaType, page, pageSize, cancellationToken));
    }
}

