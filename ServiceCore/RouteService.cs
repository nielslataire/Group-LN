using System.Net.Http.Json;
using System.Text.Json;
using FacadeCore;
using Microsoft.Extensions.Configuration;

namespace ServiceCore;

/// <summary>
/// Berekent rijroutes via de Google Maps Routes API v2 (computeRoutes).
/// </summary>
public class RouteService : IRouteService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public RouteService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["GoogleMaps:ApiKey"] ?? string.Empty;
    }

    public async Task<RouteResult?> ComputeRouteAsync(
        string originAddress, string destinationAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://routes.googleapis.com/directions/v2:computeRoutes");

        request.Headers.Add("X-Goog-Api-Key", _apiKey);
        request.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters,routes.duration");
        request.Content = JsonContent.Create(new
        {
            origin            = new { address = originAddress },
            destination       = new { address = destinationAddress },
            travelMode        = "DRIVE",
            routingPreference = "TRAFFIC_UNAWARE"
        });

        try
        {
            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("routes", out var routesEl)) return null;
            if (routesEl.GetArrayLength() == 0) return null;

            var route = routesEl[0];
            if (!route.TryGetProperty("distanceMeters", out var distEl)) return null;
            if (!route.TryGetProperty("duration", out var durEl)) return null;

            var meters  = distEl.GetInt32();
            var durStr  = durEl.GetString() ?? "0s";            // Google geeft "1234s" terug
            var seconds = int.Parse(durStr.TrimEnd('s'));

            return new RouteResult(Math.Round(meters / 1000m, 2), seconds);
        }
        catch
        {
            return null;
        }
    }
}
