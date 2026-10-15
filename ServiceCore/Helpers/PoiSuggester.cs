using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace ServiceCore.Helpers
{
    /// <summary>
    /// Stelt voorzieningen in de buurt van een punt voor (scholen, winkels, openbaar vervoer, groen, sport) via OpenStreetMap (Overpass API,
    /// gratis, geen sleutel). Afstanden zijn hemelsbreed; de tijd is een schatting (te voet tot 1,5 km, daarna met de fiets).
    /// Enkel voorzieningen met een naam; per categorie de dichtstbijzijnde. Bedoeld als voorstel dat de beheerder nog aanpast.
    /// </summary>
    public static class PoiSuggester
    {
        public sealed record Poi(string Category, string Name, double Lat, double Lng, string Distance, string Time, double Meters);

        private static readonly HttpClient Http = CreateClient();
        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd("CoproApp/1.0 (projectpagina-ligging)");
            return c;
        }

        // categorie -> (maximale afstand in m, aantal voorstellen)
        private static readonly Dictionary<string, (int Radius, int Max)> Limits = new()
        {
            ["school"] = (3000, 3), ["shop"] = (2500, 3), ["ov"] = (3500, 3), ["groen"] = (2500, 2), ["sport"] = (3000, 2)
        };

        public static async Task<List<Poi>> SuggestAsync(double lat, double lng, CancellationToken ct = default)
        {
            var la = lat.ToString(CultureInfo.InvariantCulture);
            var lo = lng.ToString(CultureInfo.InvariantCulture);
            var query = "[out:json][timeout:25];(" +
                $"nwr(around:3000,{la},{lo})[\"amenity\"~\"^(school|kindergarten|college)$\"][\"name\"];" +
                $"nwr(around:2500,{la},{lo})[\"shop\"~\"^(supermarket|bakery|convenience|butcher|greengrocer)$\"][\"name\"];" +
                $"nwr(around:3500,{la},{lo})[\"railway\"~\"^(station|halt)$\"][\"name\"];" +
                $"nwr(around:1500,{la},{lo})[\"highway\"=\"bus_stop\"][\"name\"];" +
                $"nwr(around:2500,{la},{lo})[\"leisure\"~\"^(park|nature_reserve|playground)$\"][\"name\"];" +
                $"nwr(around:3000,{la},{lo})[\"leisure\"~\"^(sports_centre|stadium|swimming_pool|pitch)$\"][\"name\"];" +
                ");out center tags 500;";

            var all = new List<Poi>();
            JsonDocument doc;
            try
            {
                doc = await QueryOverpassAsync(query, ct);
            }
            catch (HttpRequestException)
            {
                // De publieke Overpass-servers zijn soms urenlang overbelast: val terug op de zoekfunctie van Nominatim (OpenStreetMap)
                return await SuggestViaNominatimAsync(lat, lng, ct);
            }
            using var _ = doc;
            foreach (var el in doc.RootElement.GetProperty("elements").EnumerateArray())
            {
                if (!el.TryGetProperty("tags", out var tags) || !tags.TryGetProperty("name", out var nameEl)) continue;
                var name = nameEl.GetString();
                if (string.IsNullOrWhiteSpace(name)) continue;
                double elLat, elLon;
                if (el.TryGetProperty("lat", out var latEl) && el.TryGetProperty("lon", out var lonEl)) { elLat = latEl.GetDouble(); elLon = lonEl.GetDouble(); }
                else if (el.TryGetProperty("center", out var c)) { elLat = c.GetProperty("lat").GetDouble(); elLon = c.GetProperty("lon").GetDouble(); }
                else continue;

                var category = Categorize(tags);
                if (category == null) continue;
                var meters = (double)GeocodingHelper.HaversineDistanceKm(lat, lng, elLat, elLon) * 1000.0;
                if (meters > Limits[category].Radius) continue;
                all.Add(new Poi(category, name!.Trim(), Math.Round(elLat, 6), Math.Round(elLon, 6), FormatDistance(meters), FormatTime(meters), meters));
            }

            return all
                .GroupBy(p => (p.Category, p.Name.ToLowerInvariant()))
                .Select(g => g.OrderBy(p => p.Meters).First())
                .GroupBy(p => p.Category)
                .SelectMany(g => g.OrderBy(p => p.Meters).Take(Limits[g.Key].Max))
                .OrderBy(p => Array.IndexOf(new[] { "school", "shop", "ov", "groen", "sport" }, p.Category)).ThenBy(p => p.Meters)
                .ToList();
        }

        // zoektermen per categorie voor de Nominatim-terugval
        private static readonly (string Query, string Category)[] NominatimQueries =
        {
            ("school", "school"), ("supermarket", "shop"), ("bakery", "shop"), ("railway station", "ov"), ("bus stop", "ov"), ("park", "groen"), ("sports centre", "sport")
        };

        private static async Task<List<Poi>> SuggestViaNominatimAsync(double lat, double lng, CancellationToken ct)
        {
            const double dLat = 0.035, dLon = 0.055; // ~ 3,9 km rond het punt
            var viewbox = string.Create(CultureInfo.InvariantCulture, $"{lng - dLon:0.######},{lat + dLat:0.######},{lng + dLon:0.######},{lat - dLat:0.######}");
            var all = new List<Poi>();
            var first = true;
            foreach (var (query, category) in NominatimQueries)
            {
                // Nominatim-gebruiksvoorwaarden: maximaal 1 aanvraag per seconde
                if (!first) await Task.Delay(1100, ct);
                first = false;
                try
                {
                    var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&viewbox={viewbox}&bounded=1&format=jsonv2&limit=15&accept-language=nl";
                    using var response = await Http.GetAsync(url, ct);
                    if (!response.IsSuccessStatusCode) continue;
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (!double.TryParse(el.GetProperty("lat").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var elLat)) continue;
                        if (!double.TryParse(el.GetProperty("lon").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var elLon)) continue;
                        var meters = (double)GeocodingHelper.HaversineDistanceKm(lat, lng, elLat, elLon) * 1000.0;
                        if (meters > Limits[category].Radius) continue;
                        all.Add(new Poi(category, name.Trim(), Math.Round(elLat, 6), Math.Round(elLon, 6), FormatDistance(meters), FormatTime(meters), meters));
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
                {
                    if (ct.IsCancellationRequested) throw;
                }
            }
            if (all.Count == 0) throw new HttpRequestException("Geen voorzieningen gevonden via Nominatim");
            return all
                .GroupBy(p => (p.Category, p.Name.ToLowerInvariant()))
                .Select(g => g.OrderBy(p => p.Meters).First())
                .GroupBy(p => p.Category)
                .SelectMany(g => g.OrderBy(p => p.Meters).Take(Limits[g.Key].Max))
                .OrderBy(p => Array.IndexOf(new[] { "school", "shop", "ov", "groen", "sport" }, p.Category)).ThenBy(p => p.Meters)
                .ToList();
        }

        // De publieke Overpass-servers zijn soms overbelast (502/500): probeer meerdere servers, telkens twee keer, met korte pauze.
        private static readonly string[] Endpoints =
        {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass.private.coffee/api/interpreter"
        };

        private static async Task<JsonDocument> QueryOverpassAsync(string query, CancellationToken ct)
        {
            Exception? last = null;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                foreach (var url in Endpoints)
                {
                    try
                    {
                        using var response = await Http.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = query }), ct);
                        if (response.IsSuccessStatusCode)
                            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                        last = new HttpRequestException($"{url}: {(int)response.StatusCode}");
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
                    {
                        if (ct.IsCancellationRequested) throw;
                        last = ex;
                    }
                }
                await Task.Delay(2500, ct);
            }
            throw new HttpRequestException("Overpass niet bereikbaar", last);
        }

        private static string? Categorize(JsonElement tags)
        {
            string? Get(string k) => tags.TryGetProperty(k, out var v) ? v.GetString() : null;
            var amenity = Get("amenity"); var shop = Get("shop"); var railway = Get("railway"); var highway = Get("highway"); var leisure = Get("leisure");
            if (amenity is "school" or "kindergarten" or "college") return "school";
            if (shop is "supermarket" or "bakery" or "convenience" or "butcher" or "greengrocer") return "shop";
            if (railway is "station" or "halt" || highway == "bus_stop") return "ov";
            if (leisure is "park" or "nature_reserve" or "playground") return "groen";
            if (leisure is "sports_centre" or "stadium" or "swimming_pool" or "pitch") return "sport";
            return null;
        }

        private static string FormatDistance(double meters) =>
            meters < 1000 ? $"{Math.Round(meters / 10.0) * 10:0} m" : $"{(meters / 1000.0).ToString("0.0", new CultureInfo("nl-BE"))} km";

        // te voet (5 km/h) tot 1,5 km, anders met de fiets (15 km/h); omweg-factor 1,3 op de hemelsbrede afstand
        private static string FormatTime(double meters)
        {
            var route = meters * 1.3;
            if (route <= 1500) return $"{Math.Max(1, (int)Math.Ceiling(route / 83.3))} min te voet";
            return $"{Math.Max(1, (int)Math.Ceiling(route / 250.0))} min fietsen";
        }
    }
}
