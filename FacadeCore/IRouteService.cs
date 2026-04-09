namespace FacadeCore;

/// <summary>
/// Resultaat van een routeberekening via de Google Maps Routes API.
/// </summary>
/// <param name="DistanceKm">Gereden afstand in kilometer (afgerond op 2 decimalen).</param>
/// <param name="DurationSeconds">Gereden reistijd in seconden.</param>
public record RouteResult(decimal DistanceKm, int DurationSeconds);

/// <summary>
/// Service die een rijroute berekent via de Google Maps Routes API (computeRoutes).
/// </summary>
public interface IRouteService
{
    /// <summary>
    /// Berekent de rijafstand en reistijd tussen twee adressen.
    /// </summary>
    /// <param name="originAddress">Vertrekadres (vrije tekst).</param>
    /// <param name="destinationAddress">Bestemmingsadres (vrije tekst).</param>
    /// <returns>RouteResult met afstand en tijdsduur, of null bij een fout.</returns>
    Task<RouteResult?> ComputeRouteAsync(string originAddress, string destinationAddress, CancellationToken ct = default);
}
