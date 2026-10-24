using MaleMove.Application.Common;
using MaleMove.Application.Routes;
using MaleMove.Domain.Entities;
using MaleMove.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MaleMove.Infrastructure.Services;

public sealed class RouteService(AppDbContext db) : IRouteService
{
    private static readonly TimeZoneInfo MaldivesTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Indian/Maldives");
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public async Task<RouteResultDto> CalculateAsync(RouteRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCoordinate(request.Origin);
        ValidateCoordinate(request.Destination);
        var vehicle = (request.VehicleType ?? VehicleTypes.Motorcycle).ToLowerInvariant();
        if (!VehicleTypes.All.Contains(vehicle))
            throw new AppException($"Vehicle type must be one of: {string.Join(", ", VehicleTypes.All)}.", 400, "vehicle_type_invalid");

        var origin = GeometryFactory.CreatePoint(new Coordinate(request.Origin.Longitude, request.Origin.Latitude));
        var destination = GeometryFactory.CreatePoint(new Coordinate(request.Destination.Longitude, request.Destination.Latitude));
        var originSegment = await db.RoadSegments.AsNoTracking().OrderBy(x => x.Geometry.Distance(origin)).FirstOrDefaultAsync(cancellationToken);
        var destinationSegment = await db.RoadSegments.AsNoTracking().OrderBy(x => x.Geometry.Distance(destination)).FirstOrDefaultAsync(cancellationToken);
        if (originSegment is null || destinationSegment is null)
            throw new AppException("The road network has not been loaded for this area.", 503, "road_network_unavailable");

        var startNode = ClosestEndpoint(originSegment, request.Origin);
        var destinationNode = ClosestEndpoint(destinationSegment, request.Destination);
        var edges = await db.RoadSegments.AsNoTracking().ToListAsync(cancellationToken);
        // People on foot may walk a one-way street against its direction.
        if (vehicle == VehicleTypes.Walking)
            edges.AddRange(edges.Where(x => x.IsOneWay).Select(Reversed).ToList());
        var restrictions = await db.PermitRoadSegments.AsNoTracking()
            .Where(x => x.Permit.ApprovalState == PermitApprovalState.Approved)
            .Include(x => x.Permit)
            .ToListAsync(cancellationToken);
        var restrictionsByEdge = restrictions.GroupBy(x => x.RoadSegmentId).ToDictionary(x => x.Key, x => x.ToArray());
        var profile = request.TrafficProfileId is null
            ? null
            : await db.TrafficProfiles.AsNoTracking().Where(x => x.Id == request.TrafficProfileId && x.IsActive).ToListAsync(cancellationToken);

        // Prefer a route that avoids every road under an active roadwork; only pass through one when nothing else connects.
        var avoidRoadworks = true;
        var primary = FindPath(startNode, destinationNode, edges, restrictionsByEdge, profile, request.DepartureTime, null, vehicle, avoidRoadworks);
        if (primary is null)
        {
            avoidRoadworks = false;
            primary = FindPath(startNode, destinationNode, edges, restrictionsByEdge, profile, request.DepartureTime, null, vehicle, avoidRoadworks);
        }
        if (primary is null)
            throw new AppException("No valid route was found. Check the road network and active restrictions.", 422, "route_not_found");

        PathResult? alternative = null;
        if (primary.Steps.Count > 1)
        {
            var detourEdge = primary.Steps.OrderByDescending(x => x.Edge.LengthMeters).First().Edge;
            alternative = FindPath(startNode, destinationNode, edges, restrictionsByEdge, profile, request.DepartureTime, [detourEdge.Id], vehicle, avoidRoadworks);
            if (alternative is not null && alternative.Steps.Select(x => x.Edge.Id).SequenceEqual(primary.Steps.Select(x => x.Edge.Id)))
                alternative = null;
        }

        var profileVersion = profile is { Count: > 0 } ? string.Join(',', profile.Select(x => x.Version).Distinct()) : "default-uncalibrated";
        var networkVersions = edges.Select(x => x.NetworkVersion).Distinct().Take(2).ToArray();
        var networkVersion = networkVersions.Length == 1 ? networkVersions[0] : "mixed-network-version";
        var alternatives = new List<RouteAlternativeDto> { ToDto(1, primary) };
        if (alternative is not null) alternatives.Add(ToDto(2, alternative));

        return new RouteResultDto(Guid.NewGuid(), alternatives,
            "Estimated traffic scenario — no live traffic observations connected.", profileVersion,
            networkVersion, DateTimeOffset.UtcNow,
            [$"Estimated for a {vehicle}. Travel times use assumed base speeds and configured scenario multipliers.",
             avoidRoadworks
                 ? "Routes avoid roads under approved roadworks in effect at the time you reach them."
                 : "No route avoids every roadwork here, so this one passes through the affected roads with their expected delay.",
             "Endpoints are snapped to the nearest loaded road segment."]);
    }

    private static PathResult? FindPath(
        long start,
        long target,
        IReadOnlyList<RoadSegment> edges,
        IReadOnlyDictionary<Guid, PermitRoadSegment[]> restrictions,
        IReadOnlyList<TrafficProfile>? profiles,
        DateTimeOffset departure,
        IReadOnlyCollection<Guid>? bannedEdges,
        string vehicle,
        bool avoidRoadworks)
    {
        if (start == target) return new PathResult([], departure, departure, 0);
        var adjacency = edges.GroupBy(x => x.FromNodeId).ToDictionary(x => x.Key, x => x.ToArray());
        var arrival = new Dictionary<long, DateTimeOffset> { [start] = departure };
        var previous = new Dictionary<long, PathStep>();
        var queue = new PriorityQueue<long, long>();
        queue.Enqueue(start, departure.UtcDateTime.Ticks);

        while (queue.TryDequeue(out var node, out _))
        {
            if (node == target) break;
            if (!adjacency.TryGetValue(node, out var outgoing)) continue;
            var enteredAt = arrival[node];
            foreach (var edge in outgoing)
            {
                if (bannedEdges?.Contains(edge.Id) == true) continue;
                var edgeRestrictions = restrictions.TryGetValue(edge.Id, out var matches) ? matches : [];
                var localEntry = TimeZoneInfo.ConvertTime(enteredAt, MaldivesTimeZone);
                var active = edgeRestrictions.Where(x => IsEffective(x.Permit, localEntry)).ToArray();
                if (active.Any(x => avoidRoadworks
                                    || x.Permit.ImpactType == PermitImpactType.FullClosure
                                    || x.RestrictionType.Equals("full_closure", StringComparison.OrdinalIgnoreCase))) continue;

                var baseSpeed = SpeedFor(vehicle, edge.AssumedBaseSpeedKmh.GetValueOrDefault(15d));
                var multiplier = GetMultiplier(profiles, edge.RoadClass, TimeOnly.FromDateTime(localEntry.DateTime));
                if (baseSpeed <= 0 || multiplier <= 0) continue;
                var delay = active.Sum(x => Math.Max(0, x.DelaySeconds ?? 0));
                var seconds = (edge.LengthMeters / (baseSpeed * 1000d / 3600d * multiplier)) + delay;
                var candidate = enteredAt.AddSeconds(seconds);
                if (arrival.TryGetValue(edge.ToNodeId, out var current) && candidate >= current) continue;
                arrival[edge.ToNodeId] = candidate;
                previous[edge.ToNodeId] = new PathStep(edge, enteredAt,
                    active.Select(x => x.PermitId).Distinct().ToArray());
                queue.Enqueue(edge.ToNodeId, candidate.UtcDateTime.Ticks);
            }
        }

        if (!arrival.ContainsKey(target)) return null;
        var path = new List<PathStep>();
        var cursor = target;
        while (cursor != start)
        {
            if (!previous.TryGetValue(cursor, out var edge)) return null;
            path.Add(edge);
            cursor = edge.Edge.FromNodeId;
        }
        path.Reverse();
        return new PathResult(path, departure, arrival[target], path.Sum(x => x.Edge.LengthMeters));
    }

    private static RouteAlternativeDto ToDto(int rank, PathResult path)
    {
        var coordinates = new List<CoordinateDto>();
        foreach (var step in path.Steps)
        {
            foreach (var coordinate in step.Edge.Geometry.Coordinates)
            {
                var point = new CoordinateDto(coordinate.Y, coordinate.X);
                if (coordinates.Count == 0 || coordinates[^1] != point) coordinates.Add(point);
            }
        }
        var permitIds = path.Steps.SelectMany(x => x.AffectedPermitIds).Distinct().ToArray();
        return new RouteAlternativeDto(rank, coordinates, path.DistanceMeters,
            Math.Max(0, (path.Arrival - path.Departure).TotalSeconds), path.Arrival, permitIds);
    }

    /// <summary>Assumed speeds: road base speeds are for cars; motorcycles filter through traffic, bicycles cap at 14 km/h, walking is 4.8 km/h.</summary>
    private static double SpeedFor(string vehicle, double roadBaseSpeed) => vehicle switch
    {
        VehicleTypes.Motorcycle => roadBaseSpeed * 1.15,
        VehicleTypes.Bicycle => Math.Min(roadBaseSpeed, 14d),
        VehicleTypes.Walking => 4.8,
        _ => roadBaseSpeed,
    };

    private static RoadSegment Reversed(RoadSegment edge) => new()
    {
        Id = edge.Id,
        OsmWayId = edge.OsmWayId,
        FromNodeId = edge.ToNodeId,
        ToNodeId = edge.FromNodeId,
        Name = edge.Name,
        RoadClass = edge.RoadClass,
        AccessProfile = edge.AccessProfile,
        IsOneWay = false,
        LengthMeters = edge.LengthMeters,
        AssumedBaseSpeedKmh = edge.AssumedBaseSpeedKmh,
        Geometry = (LineString)edge.Geometry.Reverse(),
        NetworkVersion = edge.NetworkVersion,
    };

    private static double GetMultiplier(IReadOnlyList<TrafficProfile>? profiles, string roadClass, TimeOnly localTime)
    {
        if (profiles is null || profiles.Count == 0) return 1d;
        var selected = profiles.FirstOrDefault(x => (x.RoadClass == "*" || x.RoadClass.Equals(roadClass, StringComparison.OrdinalIgnoreCase))
            && ContainsTime(x.StartsAtLocal, x.EndsAtLocal, localTime));
        return selected?.SpeedMultiplier ?? 1d;
    }

    private static bool IsEffective(Permit permit, DateTimeOffset localEntry)
    {
        if (permit.ApprovalState != PermitApprovalState.Approved || permit.StartsAt is null || permit.EndsAt is null) return false;
        if (localEntry < TimeZoneInfo.ConvertTime(permit.StartsAt.Value, MaldivesTimeZone)
            || localEntry >= TimeZoneInfo.ConvertTime(permit.EndsAt.Value, MaldivesTimeZone)) return false;
        if (permit.DailyStart is null || permit.DailyEnd is null) return true;
        return ContainsTime(permit.DailyStart.Value, permit.DailyEnd.Value, TimeOnly.FromDateTime(localEntry.DateTime));
    }

    private static bool ContainsTime(TimeOnly startsAt, TimeOnly endsAt, TimeOnly value) => startsAt <= endsAt
        ? value >= startsAt && value < endsAt
        : value >= startsAt || value < endsAt;

    private static long ClosestEndpoint(RoadSegment segment, CoordinateDto point)
    {
        var first = segment.Geometry.Coordinates[0];
        var last = segment.Geometry.Coordinates[^1];
        var firstDistance = SquaredDistance(first.Y, first.X, point.Latitude, point.Longitude);
        var lastDistance = SquaredDistance(last.Y, last.X, point.Latitude, point.Longitude);
        return firstDistance <= lastDistance ? segment.FromNodeId : segment.ToNodeId;
    }

    private static double SquaredDistance(double lat1, double lon1, double lat2, double lon2) =>
        Math.Pow(lat1 - lat2, 2) + Math.Pow(lon1 - lon2, 2);

    private static void ValidateCoordinate(CoordinateDto point)
    {
        if (!double.IsFinite(point.Latitude) || !double.IsFinite(point.Longitude)
            || point.Latitude is < -90 or > 90 || point.Longitude is < -180 or > 180)
            throw new AppException("Coordinates must be finite latitude/longitude values.", 422, "coordinate_invalid");
        if (point.Latitude is < 4.0 or > 4.5 || point.Longitude is < 73.2 or > 73.8)
            throw new AppException("Coordinates must be within the supported Greater Malé area.", 422, "coordinate_outside_supported_area");
    }

    private sealed record PathStep(RoadSegment Edge, DateTimeOffset EnteredAt, IReadOnlyList<Guid> AffectedPermitIds);
    private sealed record PathResult(IReadOnlyList<PathStep> Steps, DateTimeOffset Departure, DateTimeOffset Arrival, double DistanceMeters);
}

