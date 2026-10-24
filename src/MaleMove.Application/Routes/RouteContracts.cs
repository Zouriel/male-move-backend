namespace MaleMove.Application.Routes;

public sealed record CoordinateDto(double Latitude, double Longitude);

public sealed record RouteRequestDto(
    CoordinateDto Origin,
    CoordinateDto Destination,
    DateTimeOffset DepartureTime,
    Guid? TrafficProfileId = null);

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

