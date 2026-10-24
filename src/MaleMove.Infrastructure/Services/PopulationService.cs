using MaleMove.Application.Population;
using MaleMove.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaleMove.Infrastructure.Services;

public sealed class PopulationService(AppDbContext db) : IPopulationService
{
    public async Task<PopulationPage> GetAsync(int? fromYear, int? toYear, string? areaType, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.PopulationSeries.AsNoTracking().Where(x => x.DatasetSource.ApprovalState == "Approved");
        if (fromYear is not null) query = query.Where(x => x.Year >= fromYear);
        if (toYear is not null) query = query.Where(x => x.Year <= toYear);
        if (!string.IsNullOrWhiteSpace(areaType)) query = query.Where(x => x.GeographicArea.AreaType == areaType);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.GeographicArea.Name).ThenBy(x => x.Year)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PopulationRowDto(x.GeographicArea.OfficialCode, x.GeographicArea.Name, x.GeographicArea.AreaType,
                x.Year, x.Indicator, x.Category, x.AgeGroup, x.Sex, x.Value, x.Unit, x.IsProjection,
                x.DatasetSource.Title, x.SourceLocator)).ToListAsync(cancellationToken);
        return new PopulationPage(rows, total, page, pageSize);
    }
}

