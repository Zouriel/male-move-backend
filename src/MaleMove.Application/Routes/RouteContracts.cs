namespace MaleMove.Application.Routes;

public sealed record CoordinateDto(double Latitude, double Longitude);

public sealed record RouteRequestDto(
    CoordinateDto Origin,
    CoordinateDto Destination,
    DateTimeOffset DepartureTime,
    Guid? TrafficProfileId = null,
    string? VehicleType = null);

/// <summary>Vehicle types a route can be estimated for; motorcycle is the default (the most common vehicle in Malé).</summary>
public static class VehicleTypes
{
    public const string Motorcycle = "motorcycle";
    public const string Car = "car";
    public const string Bicycle = "bicycle";
    public const string Walking = "walking";
    public static readonly string[] All = [Motorcycle, Car, Bicycle, Walking];
}

public sealed record RouteAlternativeDto(
    int Rank,
    IReadOnlyList<CoordinateDto> Geometry,
    double DistanceMeters,
    double EstimatedDurationSeconds,
    DateTimeOffset EstimatedArrival,
    IReadOnlyList<Guid> AffectedPermitIds);

public sealed record RouteResultDto(
    Guid Id,
    IReadOnlyList<RouteAlternativeDto> Alternatives,
    string TrafficLabel,
    string TrafficProfileVersion,
    string NetworkVersion,
    DateTimeOffset CalculatedAt,
    IReadOnlyList<string> Assumptions);

public interface IRouteService
{
    Task<RouteResultDto> CalculateAsync(RouteRequestDto request, CancellationToken cancellationToken);
}

