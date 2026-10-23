using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services.Peppol
{
    public class PeppolDirectoryClient : IPeppolDirectoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PeppolDirectoryClient> _logger;

        public PeppolDirectoryClient(HttpClient httpClient, ILogger<PeppolDirectoryClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://directory.peppol.eu/");
            }
        }

        public async Task<PeppolParticipant?> FindParticipantAsync(string identifier, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            foreach (var candidate in BuildCandidates(identifier))
            {
                var participant = await QueryParticipantAsync(candidate, ct);
                if (participant != null)
                    return participant;
            }

            return null;
        }

        private async Task<PeppolParticipant?> QueryParticipantAsync(string candidate, CancellationToken ct)
        {
            var normalized = candidate.StartsWith("iso6523-actorid-upis::", StringComparison.OrdinalIgnoreCase)
                ? candidate
                : $"iso6523-actorid-upis::{candidate}";

            try
            {
                // De zoek-API van de Peppol Directory (JSON). Het vroegere pad public/directory/v3/participants bestaat niet en gaf een HTML-pagina terug,
                // waardoor élke klant als "niet geregistreerd" gold.
                var response = await _httpClient.GetAsync($"search/1.0/json?participant={Uri.EscapeDataString(normalized)}", ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Peppol directory returned {StatusCode} for {Participant}", response.StatusCode, normalized);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                var root = json.RootElement;

                if (root.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
                {
                    foreach (var match in matches.EnumerateArray())
                    {
                        // participantID is een object { scheme, value }; oudere antwoorden hadden een tekst.
                        string? id = null;
                        if (match.TryGetProperty("participantID", out var pid))
                        {
                            if (pid.ValueKind == JsonValueKind.Object && pid.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String)
                            {
                                var scheme = pid.TryGetProperty("scheme", out var sc) && sc.ValueKind == JsonValueKind.String ? sc.GetString() : null;
                                id = string.IsNullOrWhiteSpace(scheme) ? v.GetString() : $"{scheme}::{v.GetString()}";
                            }
                            else if (pid.ValueKind == JsonValueKind.String) id = pid.GetString();
                        }
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        string? name = null, country = null;
                        if (match.TryGetProperty("entities", out var ents) && ents.ValueKind == JsonValueKind.Array)
                            foreach (var ent in ents.EnumerateArray())
                            {
                                country ??= ent.GetPropertyOrDefault("countryCode");
                                if (name == null && ent.TryGetProperty("name", out var names) && names.ValueKind == JsonValueKind.Array)
                                    foreach (var n in names.EnumerateArray()) { name = n.GetPropertyOrDefault("name"); if (!string.IsNullOrWhiteSpace(name)) break; }
                            }
                        return new PeppolParticipant(id!, name, country);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Peppol lookup failed for {Participant}", normalized);
            }

            return null;
        }

        private static IEnumerable<string> BuildCandidates(string identifier)
        {
            var trimmed = identifier.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                yield break;

            if (trimmed.StartsWith("iso6523-actorid-upis::", StringComparison.OrdinalIgnoreCase))
            {
                yield return trimmed;
                yield break;
            }

            yield return trimmed;

            if (trimmed.Contains(":", StringComparison.Ordinal))
                yield break;

            if (trimmed.StartsWith("BE", StringComparison.OrdinalIgnoreCase))
            {
                var upper = trimmed.ToUpperInvariant();
                // Belgisch ondernemingsnummer in Peppol: 0208 + 10 cijfers (zonder "BE", met voorloopnul).
                var digits = new string(upper.Where(char.IsDigit).ToArray());
                if (digits.Length == 9) digits = "0" + digits;
                if (digits.Length == 10) yield return $"0208:{digits}";
                yield return $"0208:{upper}";
                yield return $"0088:{upper}";
            }
            else
            {
                var bare = new string(trimmed.Where(char.IsDigit).ToArray());
                if (bare.Length == 9) bare = "0" + bare;
                if (bare.Length == 10) yield return $"0208:{bare}";
                yield return $"0208:{trimmed}";
            }
        }
    }

    internal static class JsonElementExtensions
    {
        public static string? GetPropertyOrDefault(this JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.ToString(),
                    _ => null
                };
            }

            return null;
        }
    }
}