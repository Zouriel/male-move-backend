namespace MaleMove.Application.Population;

public sealed record DataSourceDto(
    Guid Id,
    string Publisher,
    string Title,
    string? SourceUri,
    int? SourceYear,
    string License,
    string ApprovalState,
    string CoverageDescription,
    string Limitations,
    DateTimeOffset? LastCheckedAt);

public interface IDataSourceService
{
    Task<IReadOnlyList<DataSourceDto>> ListAsync(CancellationToken cancellationToken);
}
