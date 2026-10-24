using System.Security.Claims;
using MaleMove.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult Success<T>(T data, string? message = null) => Ok(ApiResponse<T>.Ok(data, message));
    protected IActionResult Created<T>(T data, string? message = null) => StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message));
    protected Guid CurrentUserId => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new AppException("The authenticated identity has no valid subject identifier.", 403, "identity_subject_invalid");
}

