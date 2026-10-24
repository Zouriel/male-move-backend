using NetTopologySuite.Geometries;

namespace MaleMove.Domain.Entities;

public sealed class GeographicArea
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentAreaId { get; set; }
    public string OfficialCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AreaType { get; set; } = string.Empty;
    public MultiPolygon Geometry { get; set; } = null!;
    public string SourceVersion { get; set; } = string.Empty;
}

public sealed class DatasetSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Publisher { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SourceUri { get; set; }
    public int? SourceYear { get; set; }
    public string License { get; set; } = string.Empty;
    public string ApprovalState { get; set; } = "Pending";
    public string CoverageDescription { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PopulationSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GeographicAreaId { get; set; }
    public GeographicArea GeographicArea { get; set; } = null!;
    public int Year { get; set; }
    public string Indicator { get; set; } = "population";
    public string? Category { get; set; }
    public string? AgeGroup { get; set; }
    public string? Sex { get; set; }
    public long Value { get; set; }
    public string Unit { get; set; } = "persons";
    public bool IsProjection { get; set; }
    public Guid DatasetSourceId { get; set; }
    public DatasetSource DatasetSource { get; set; } = null!;
    public string SourceLocator { get; set; } = string.Empty;
}

public sealed class TrafficProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DayType { get; set; } = "AllDays";
    public string RoadClass { get; set; } = "*";
    public TimeOnly StartsAtLocal { get; set; }
    public TimeOnly EndsAtLocal { get; set; }
    public double SpeedMultiplier { get; set; } = 1d;
    public bool IsCalibrated { get; set; }
    public string AssumptionNote { get; set; } = string.Empty;
    public string Version { get; set; } = "1";
    public bool IsActive { get; set; }
}

