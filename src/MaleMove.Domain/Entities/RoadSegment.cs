using NetTopologySuite.Geometries;

namespace MaleMove.Domain.Entities;

public sealed class RoadSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long OsmWayId { get; set; }
    public long FromNodeId { get; set; }
    public long ToNodeId { get; set; }
    public string? Name { get; set; }
    public string RoadClass { get; set; } = "unclassified";
    public string AccessProfile { get; set; } = "road";
    public bool IsOneWay { get; set; }
    public double LengthMeters { get; set; }
    public double? AssumedBaseSpeedKmh { get; set; }
    public LineString Geometry { get; set; } = null!;
    public string NetworkVersion { get; set; } = string.Empty;
}

