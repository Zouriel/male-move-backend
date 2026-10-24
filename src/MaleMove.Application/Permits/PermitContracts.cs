using MaleMove.Domain.Entities;

namespace MaleMove.Application.Permits;

public sealed record PermitSummaryDto(
    Guid Id,
    string PermitNumber,
    string LocationDescription,
    string WorkDescription,
    PermitImpactType ImpactType,
    PermitApprovalState ApprovalState,
    SiteVerificationState SiteVerificationState,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    int Revision,
    DateTimeOffset UpdatedAt);

public sealed record PermitListResult(IReadOnlyList<PermitSummaryDto> Items, int Page, int PageSize, int Total);

public sealed record UpdatePermitRequest(
    string PermitNumber,
    string LocationDescription,
    string WorkDescription,
    string? Contractor,
    PermitImpactType ImpactType,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    TimeOnly? DailyStart,
    TimeOnly? DailyEnd,
    int Revision);

public interface IPermitService
{
    Task<PermitListResult> ListAsync(int page, int pageSize, PermitApprovalState? state, CancellationToken cancellationToken);
    Task<PermitSummaryDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PermitSummaryDto> UpdateAsync(Guid id, UpdatePermitRequest request, Guid actorId, CancellationToken cancellationToken);
    Task<PermitSummaryDto> SubmitAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
    Task<PermitSummaryDto> ReviewAsync(Guid id, bool approve, string? comment, Guid actorId, CancellationToken cancellationToken);
}

