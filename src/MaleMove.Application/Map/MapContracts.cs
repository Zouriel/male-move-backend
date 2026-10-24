namespace MaleMove.Application.Map;

public sealed record MapBounds(double West, double South, double East, double North);
public sealed record MapCoordinate(double Latitude, double Longitude);
public sealed record RoadFeatureDto(Guid Id, string? Name, string RoadClass, IReadOnlyList<MapCoordinate> Coordinates);
public sealed record RoadworkFeatureDto(Guid PermitId, string PermitNumber, string LocationDescription, string ImpactType,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<MapCoordinate> Coordinates, string ApprovalState);
public sealed record MapFeaturePage<T>(IReadOnlyList<T> Features, bool Truncated);

public interface IMapService
{
    Task<MapFeaturePage<RoadFeatureDto>> GetRoadsAsync(MapBounds bounds, CancellationToken cancellationToken);
    Task<MapFeaturePage<RoadworkFeatureDto>> GetRoadworksAsync(MapBounds bounds, DateTimeOffset at, CancellationToken cancellationToken);
}

