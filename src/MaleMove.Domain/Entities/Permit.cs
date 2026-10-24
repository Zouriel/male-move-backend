namespace MaleMove.Domain.Entities;

public enum PermitApprovalState
{
    Draft,
    NeedsReview,
    Submitted,
    Approved,
    Rejected
}

public enum PermitImpactType
{
    FullClosure,
    PartialRestriction,
    LaneRestriction,
    Unknown
}

public enum SiteVerificationState
{
    Unverified,
    VerifiedOpen,
    VerifiedRestricted,
    VerifiedClosed,
    Disputed
}

public sealed class Permit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PermitNumber { get; set; } = string.Empty;
    public string LocationDescription { get; set; } = string.Empty;
    public string WorkDescription { get; set; } = string.Empty;
    public string? Contractor { get; set; }
    public PermitImpactType ImpactType { get; set; }
    public PermitApprovalState ApprovalState { get; set; } = PermitApprovalState.Draft;
    public SiteVerificationState SiteVerificationState { get; set; } = SiteVerificationState.Unverified;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public TimeOnly? DailyStart { get; set; }
    public TimeOnly? DailyEnd { get; set; }
    public string? ReviewComment { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public int Revision { get; set; } = 1;
    public Guid? PermitDocumentId { get; set; }
    public PermitDocument? PermitDocument { get; set; }
    public ICollection<PermitRoadSegment> RoadSegments { get; set; } = new List<PermitRoadSegment>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PermitDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string ScanStatus { get; set; } = "Pending";
    public string OcrStatus { get; set; } = "NotStarted";
    public Guid UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PermitRoadSegment
{
    public Guid PermitId { get; set; }
    public Permit Permit { get; set; } = null!;
    public Guid RoadSegmentId { get; set; }
    public RoadSegment RoadSegment { get; set; } = null!;
    public string RestrictionType { get; set; } = string.Empty;
    public double? DelaySeconds { get; set; }
    public double? SpeedMultiplier { get; set; }
    public double? MatchConfidence { get; set; }
    public string? MatchEvidence { get; set; }
}

