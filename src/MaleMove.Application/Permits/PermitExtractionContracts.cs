namespace MaleMove.Application.Permits;

/// <summary>
/// Fields read from an uploaded permit document. Every value is a suggestion for a person to check
/// before saving; anything the document doesn't state is null.
/// </summary>
public sealed record PermitExtractionDto(
    string? PermitNumber,
    string? LocationDescription,
    string? WorkDescription,
    string? Contractor,
    string? ImpactType,
    string? StartDate,
    string? StartTime,
    string? EndDate,
    string? EndTime,
    string? DailyStart,
    string? DailyEnd,
    IReadOnlyList<string> StreetNames);

public interface IPermitExtractionService
{
    Task<PermitExtractionDto> ExtractAsync(Guid permitId, CancellationToken cancellationToken);
}
