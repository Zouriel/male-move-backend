using MaleMove.Application.Permits;
using MaleMove.Application.Routes;
using MaleMove.Application.Map;
using MaleMove.Application.Population;
using MaleMove.Infrastructure.Persistence;
using MaleMove.Infrastructure.Services;
using MaleMove.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaleMove.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMaleMoveInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));
        services.AddScoped<IPermitService, PermitService>();
        services.AddScoped<IPermitUploadService, PermitUploadService>();
        services.AddScoped<IPopulationService, PopulationService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IMapService, MapService>();
        services.AddSingleton<IPermitDocumentStorage, LocalPermitDocumentStorage>();
        services.AddSingleton<IFileMalwareScanner, ClamAvMalwareScanner>();
        return services;
    }
}
