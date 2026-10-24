namespace MaleMove.Application.Population;

public sealed record PopulationRowDto(
    string AreaCode,
    string AreaName,
    string AreaType,
    int Year,
    string Indicator,
    string? Category,
    string? AgeGroup,
    string? Sex,
    long Value,
    string Unit,
    bool IsProjection,
    string SourceTitle,
    string SourceLocator);

public sealed record PopulationPage(IReadOnlyList<PopulationRowDto> Items, int Total, int Page, int PageSize);

public interface IPopulationService
{
    Task<PopulationPage> GetAsync(int? fromYear, int? toYear, string? areaType, int page, int pageSize, CancellationToken cancellationToken);
}

