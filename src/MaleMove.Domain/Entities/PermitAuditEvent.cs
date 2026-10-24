namespace MaleMove.Domain.Entities;

/// <summary>Append-only event record. Stored in the same transaction as the related permit change.</summary>
public sealed class PermitAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PermitId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousState { get; set; }
    public string? NewState { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
