using MaleMove.Application.Common;
using MaleMove.Application.Permits;
using MaleMove.Domain.Entities;
using MaleMove.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaleMove.Infrastructure.Services;

public sealed class PermitService(AppDbContext db) : IPermitService
{
    public async Task<PermitListResult> ListAsync(int page, int pageSize, PermitApprovalState? state, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new AppException("Page must be positive and page size must be between 1 and 100.", 400, "invalid_pagination");

        var query = db.Permits.AsNoTracking();
        if (state is not null) query = query.Where(x => x.ApprovalState == state);
        var total = await query.CountAsync(cancellationToken);
        var permits = await query.OrderByDescending(x => x.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = permits.Select(ToDto).ToArray();
        return new PermitListResult(items, page, pageSize, total);
    }

    public async Task<PermitSummaryDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var permit = await db.Permits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return permit is null ? null : ToDto(permit);
    }

    public async Task<PermitSummaryDto> UpdateAsync(Guid id, UpdatePermitRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        var permit = await db.Permits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Permit was not found.", 404, "permit_not_found");
        if (permit.Revision != request.Revision)
            throw new AppException("This permit changed since it was loaded. Refresh and review the current version.", 409, "revision_conflict");
        if (permit.ApprovalState is not (PermitApprovalState.Draft or PermitApprovalState.NeedsReview or PermitApprovalState.Rejected))
            throw new AppException("Only draft, rejected, or returned permits can be edited. Submitted and approved permits are locked.", 409, "permit_revision_locked");
        if (string.IsNullOrWhiteSpace(request.PermitNumber) || string.IsNullOrWhiteSpace(request.LocationDescription)
            || string.IsNullOrWhiteSpace(request.WorkDescription))
            throw new AppException("Permit number, location and work description are required.", 422, "required_field_missing");
        if (request.PermitNumber.Length > 120 || request.LocationDescription.Length > 1500
            || request.WorkDescription.Length > 4000 || request.Contractor?.Length > 300)
            throw new AppException("One or more permit fields exceed their allowed length.", 422, "field_too_long");
        if ((request.DailyStart is null) != (request.DailyEnd is null))
            throw new AppException("Recurring daily restrictions require both a start and an end time.", 422, "daily_schedule_incomplete");
        if (request.StartsAt is not null && request.EndsAt is not null && request.EndsAt <= request.StartsAt)
            throw new AppException("The permit end time must be after its start time.", 422, "invalid_schedule");

        permit.PermitNumber = request.PermitNumber.Trim();
        var previousState = permit.ApprovalState.ToString();
        permit.LocationDescription = request.LocationDescription.Trim();
        permit.WorkDescription = request.WorkDescription.Trim();
        permit.Contractor = string.IsNullOrWhiteSpace(request.Contractor) ? null : request.Contractor.Trim();
        permit.ImpactType = request.ImpactType;
        permit.StartsAt = request.StartsAt;
        permit.EndsAt = request.EndsAt;
        permit.DailyStart = request.DailyStart;
        permit.DailyEnd = request.DailyEnd;
        permit.ApprovalState = PermitApprovalState.NeedsReview;
        permit.Revision++;
        permit.UpdatedAt = DateTimeOffset.UtcNow;
        db.PermitAuditEvents.Add(Audit(permit.Id, actorId, "permit.updated", previousState, permit.ApprovalState.ToString()));
        await MatchRoadsAsync(permit, request.StreetNames, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(permit);
    }

    /// <summary>
    /// Links the permit to the loaded road segments whose street name appears in its location (or in the street names
    /// read from the document), limited to the island it names. A reviewer still confirms the match before approval.
    /// </summary>
    private async Task MatchRoadsAsync(Permit permit, IReadOnlyList<string>? streetNames, CancellationToken cancellationToken)
    {
        var text = string.Join(" | ", new[] { permit.LocationDescription }.Concat(streetNames ?? [])).ToLowerInvariant();
        var names = await db.RoadSegments.AsNoTracking().Where(x => x.Name != null).Select(x => x.Name!).Distinct().ToListAsync(cancellationToken);
        var matched = names.Where(n => n.Length >= 4 && text.Contains(n.ToLowerInvariant(), StringComparison.Ordinal)).ToList();
        // Island bands within Greater Malé, by latitude: Malé is south of the airport, Hulhumalé north of it.
        (double Min, double Max)? band = text.Contains("hulhumal") ? (4.200, 4.260) : text.Contains("male") || text.Contains("malé") ? (4.160, 4.185) : null;
        var segments = matched.Count == 0 ? [] : await db.RoadSegments.AsNoTracking()
            .Where(x => x.Name != null && matched.Contains(x.Name))
            .Where(x => band == null || (x.Geometry.Centroid.Y >= band.Value.Min && x.Geometry.Centroid.Y <= band.Value.Max))
            .Select(x => x.Id).ToListAsync(cancellationToken);

        await db.PermitRoadSegments.Where(x => x.PermitId == permit.Id).ExecuteDeleteAsync(cancellationToken);
        var restriction = permit.ImpactType == PermitImpactType.FullClosure ? "full_closure" : permit.ImpactType.ToString().ToLowerInvariant();
        db.PermitRoadSegments.AddRange(segments.Select(id => new PermitRoadSegment
        {
            PermitId = permit.Id, RoadSegmentId = id, RestrictionType = restriction,
            DelaySeconds = permit.ImpactType == PermitImpactType.FullClosure ? null : 120, MatchConfidence = 0.6,
            MatchEvidence = $"Street name matched: {string.Join(", ", matched)}",
        }));
    }

    public async Task<PermitSummaryDto> SubmitAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var permit = await db.Permits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Permit was not found.", 404, "permit_not_found");
        if (permit.ApprovalState == PermitApprovalState.Submitted)
            return ToDto(permit);
        if (permit.ApprovalState is not (PermitApprovalState.Draft or PermitApprovalState.NeedsReview))
            throw new AppException("Only draft or corrected permits can be submitted.", 409, "invalid_permit_transition");
        if (permit.StartsAt is null || permit.EndsAt is null || permit.ImpactType == PermitImpactType.Unknown)
            throw new AppException("A complete schedule and restriction type are required before review.", 422, "permit_incomplete");
        var previousState = permit.ApprovalState.ToString();
        permit.ApprovalState = PermitApprovalState.Submitted;
        permit.Revision++;
        permit.UpdatedAt = DateTimeOffset.UtcNow;
        db.PermitAuditEvents.Add(Audit(permit.Id, actorId, "permit.submitted", previousState, permit.ApprovalState.ToString()));
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(permit);
    }

    public async Task<PermitSummaryDto> ReviewAsync(Guid id, bool approve, string? comment, Guid actorId, CancellationToken cancellationToken)
    {
        var permit = await db.Permits.Include(x => x.PermitDocument).Include(x => x.RoadSegments)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException("Permit was not found.", 404, "permit_not_found");
        if (permit.ApprovalState != PermitApprovalState.Submitted)
            throw new AppException("Only submitted permits can be reviewed.", 409, "invalid_permit_transition");
        if (approve && permit.RoadSegments.Count == 0)
            throw new AppException("Confirm at least one affected road segment before approval.", 422, "road_segment_required");
        if (approve && permit.PermitDocument is not null && permit.PermitDocument.UploadedBy == actorId)
            throw new AppException("A permit uploader cannot approve their own permit.", 403, "separation_of_duties_required");
        if (approve && (permit.PermitDocument is null || permit.PermitDocument.ScanStatus != "Clean"))
            throw new AppException("The permit document must pass malware scanning before approval.", 409, "document_scan_required");
        if (!approve && string.IsNullOrWhiteSpace(comment))
            throw new AppException("A rejection reason is required.", 422, "review_comment_required");
        if (comment?.Length > 2000)
            throw new AppException("Review comments may not exceed 2000 characters.", 422, "review_comment_too_long");

        var previousState = permit.ApprovalState.ToString();
        permit.ApprovalState = approve ? PermitApprovalState.Approved : PermitApprovalState.Rejected;
        permit.ReviewComment = comment?.Trim();
        permit.ReviewedBy = actorId;
        permit.ReviewedAt = DateTimeOffset.UtcNow;
        permit.Revision++;
        permit.UpdatedAt = DateTimeOffset.UtcNow;
        db.PermitAuditEvents.Add(Audit(permit.Id, actorId, approve ? "permit.approved" : "permit.rejected", previousState, permit.ApprovalState.ToString()));
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(permit);
    }

    private static PermitSummaryDto ToDto(Permit permit) => new(
        permit.Id, permit.PermitNumber, permit.LocationDescription, permit.WorkDescription, permit.ImpactType,
        permit.ApprovalState, permit.SiteVerificationState, permit.StartsAt, permit.EndsAt, permit.Revision, permit.UpdatedAt);

    private static PermitAuditEvent Audit(Guid permitId, Guid actorId, string action, string? previous, string? next) => new()
    {
        PermitId = permitId, ActorId = actorId, Action = action,
        PreviousState = previous, NewState = next, OccurredAt = DateTimeOffset.UtcNow
    };
}

