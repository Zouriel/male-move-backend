using MaleMove.Application.Population;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[Authorize(Policy = "PlannerRead")]
public sealed class DataSourcesController(IDataSourceService sources) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Success(await sources.ListAsync(cancellationToken));
}

