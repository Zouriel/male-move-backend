using MaleMove.Application.Population;
using MaleMove.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaleMove.Infrastructure.Services;

public sealed class DataSourceService(AppDbContext db) : IDataSourceService
{
    public async Task<IReadOnlyList<DataSourceDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.DatasetSources.AsNoTracking().OrderBy(x => x.Title)
            .Select(x => new DataSourceDto(x.Id, x.Publisher, x.Title, x.SourceUri, x.SourceYear, x.License,
                x.ApprovalState, x.CoverageDescription, x.Limitations, x.LastCheckedAt))
            .ToListAsync(cancellationToken);
}
