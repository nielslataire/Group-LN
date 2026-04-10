using FacadeCore;

namespace ServiceCore;

/// <summary>
/// No-op implementatie van IRouteService voor contexten zonder HttpClient (bv. ServiceFactory).
/// Geeft altijd null terug zodat de aanroepende code graceful degradeert.
/// </summary>
public sealed class NullRouteService : IRouteService
{
    public static readonly IRouteService Instance = new NullRouteService();

    private NullRouteService() { }

    public Task<RouteResult?> ComputeRouteAsync(
        string originAddress, string destinationAddress, CancellationToken ct = default)
        => Task.FromResult<RouteResult?>(null);
}
