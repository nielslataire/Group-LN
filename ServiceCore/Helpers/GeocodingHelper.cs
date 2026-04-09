using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ServiceCore.Helpers
{
    /// <summary>
    /// Geocodeert adressen via OpenStreetMap Nominatim (gratis, geen API-key nodig)
    /// en berekent de hemelsbreedse afstand (haversine) in kilometer.
    /// </summary>
    internal static class GeocodingHelper
    {
        private static readonly HttpClient _http;

        static GeocodingHelper()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CoproApp/1.0 (coordinatieproject-afstand)");
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Geocodeer een adresstring via Nominatim.
        /// Geeft (null, null) terug als het adres niet gevonden wordt.
        /// </summary>
        internal static async Task<(double? Lat, double? Lon)> GeocodeAsync(string address)
        {
            try
            {
                var encoded = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1";
                var results = await _http.GetFromJsonAsync<NominatimResult[]>(url);

                if (results == null || results.Length == 0) return (null, null);
                if (!double.TryParse(results[0].Lat, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat)) return (null, null);
                if (!double.TryParse(results[0].Lon, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon)) return (null, null);

                return (lat, lon);
            }
            catch
            {
                return (null, null);
            }
        }

        /// <summary>
        /// Berekent de hemelsbreedse afstand in km (haversine-formule).
        /// </summary>
        internal static decimal HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // straal aarde in km

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (decimal)Math.Round(R * c, 2);
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        private class NominatimResult
        {
            [JsonPropertyName("lat")]
            public string Lat { get; set; }

            [JsonPropertyName("lon")]
            public string Lon { get; set; }
        }
    }
}
