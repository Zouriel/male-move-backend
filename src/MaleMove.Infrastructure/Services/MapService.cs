using MaleMove.Application.Common;
using MaleMove.Application.Map;
using MaleMove.Domain.Entities;
using MaleMove.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MaleMove.Infrastructure.Services;

public sealed class MapService(AppDbContext db) : IMapService
{
    private static readonly GeometryFactory Factory = new(new PrecisionModel(), 4326);
    private const int MaximumFeatures = 2000;

    public async Task<MapFeaturePage<RoadFeatureDto>> GetRoadsAsync(MapBounds bounds, CancellationToken cancellationToken)
    {
        var area = CreateBounds(bounds);
        var rows = await db.RoadSegments.AsNoTracking().Where(x => x.Geometry.Intersects(area))
            .OrderBy(x => x.RoadClass).Take(MaximumFeatures + 1).ToListAsync(cancellationToken);
        var truncated = rows.Count > MaximumFeatures;
        var features = rows.Take(MaximumFeatures).Select(x => new RoadFeatureDto(x.Id, x.Name, x.RoadClass,
            x.Geometry.Coordinates.Select(ToCoordinate).ToArray())).ToArray();
        return new MapFeaturePage<RoadFeatureDto>(features, truncated);
    }

    public async Task<MapFeaturePage<RoadworkFeatureDto>> GetRoadworksAsync(MapBounds bounds, DateTimeOffset at, bool includePending, CancellationToken cancellationToken)
    {
        var area = CreateBounds(bounds);
        var rows = await db.Permits.AsNoTracking()
            // Approved works in effect now, plus permits awaiting review so a new submission shows at once (marked pending).
            .Where(x => (x.ApprovalState == PermitApprovalState.Approved && x.StartsAt <= at && x.EndsAt > at)
                        || (includePending && (x.ApprovalState == PermitApprovalState.Submitted || x.ApprovalState == PermitApprovalState.NeedsReview) && x.EndsAt > at))
            .SelectMany(x => x.RoadSegments.Where(segment => segment.RoadSegment.Geometry.Intersects(area)).Select(segment => new
            {
                x.Id, x.PermitNumber, x.LocationDescription, x.ImpactType, x.StartsAt, x.EndsAt, x.ApprovalState,
                Geometry = segment.RoadSegment.Geometry
            }))
            .Take(MaximumFeatures + 1).ToListAsync(cancellationToken);
        var truncated = rows.Count > MaximumFeatures;
        var features = rows.Take(MaximumFeatures).Select(x => new RoadworkFeatureDto(x.Id, x.PermitNumber,
            x.LocationDescription, x.ImpactType.ToString(), x.StartsAt!.Value, x.EndsAt!.Value,
            x.Geometry.Coordinates.Select(ToCoordinate).ToArray(), x.ApprovalState.ToString())).ToArray();
        return new MapFeaturePage<RoadworkFeatureDto>(features, truncated);
    }

    private static Polygon CreateBounds(MapBounds bounds)
    {
        if (!double.IsFinite(bounds.West) || !double.IsFinite(bounds.East) || !double.IsFinite(bounds.South) || !double.IsFinite(bounds.North)
            || bounds.West < 72 || bounds.East > 75 || bounds.South < -1 || bounds.North > 8
            || bounds.West >= bounds.East || bounds.South >= bounds.North
            || bounds.East - bounds.West > 1 || bounds.North - bounds.South > 1)
            throw new AppException("Map bounds are invalid or exceed the supported query area.", 422, "map_bounds_invalid");
        return Factory.ToGeometry(new Envelope(bounds.West, bounds.East, bounds.South, bounds.North)) as Polygon
            ?? throw new InvalidOperationException("Could not create map query bounds.");
    }

    private static MapCoordinate ToCoordinate(Coordinate coordinate) => new(coordinate.Y, coordinate.X);
}

