using MaleMove.Application.Permits;
using MaleMove.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaleMove.Api.Controllers;

[Authorize]
public sealed class PermitsController(IPermitService permits, IPermitUploadService uploads, IPermitExtractionService extraction) : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = "PlannerRead")]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] PermitApprovalState? state = null, CancellationToken cancellationToken = default) =>
        Success(await permits.ListAsync(page, pageSize, state, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PlannerRead")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var permit = await permits.GetAsync(id, cancellationToken);
        return permit is null ? NotFound() : Success(permit);
    }

    [HttpPost("upload")]
    [Authorize(Policy = "PermitOfficer")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 26 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest("A permit file is required.");
        await using var stream = file.OpenReadStream();
        var permit = await uploads.UploadAsync(new PermitUploadRequest(stream, file.FileName, file.ContentType, file.Length, CurrentUserId), cancellationToken);
        return Created(permit);
    }

    /// <summary>Reads the uploaded permit document and suggests its fields. Nothing is saved.</summary>
    [HttpPost("{id:guid}/extract")]
    [Authorize(Policy = "PermitOfficer")]
    public async Task<IActionResult> Extract(Guid id, CancellationToken cancellationToken) =>
        Success(await extraction.ExtractAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PermitOfficer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermitRequest request, CancellationToken cancellationToken) =>
        Success(await permits.UpdateAsync(id, request, CurrentUserId, cancellationToken));

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "PermitOfficer")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken) =>
        Success(await permits.SubmitAsync(id, CurrentUserId, cancellationToken));

    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = "PermitApprover")]
    public async Task<IActionResult> Review(Guid id, [FromBody] PermitReviewRequest request, CancellationToken cancellationToken) =>
        Success(await permits.ReviewAsync(id, request.Approve, request.Comment, CurrentUserId, cancellationToken));
}

public sealed record PermitReviewRequest(bool Approve, string? Comment);

