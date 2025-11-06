using System;
using System.Collections.Generic;
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
                _httpClient.BaseAddress = new Uri("https://directory.peppol.eu/public/directory/");
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
                var response = await _httpClient.GetAsync($"v3/participants?participant={Uri.EscapeDataString(normalized)}", ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Peppol directory returned {StatusCode} for {Participant}", response.StatusCode, normalized);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                var root = json.RootElement;

                if (root.TryGetProperty("participants", out var participants) && participants.ValueKind == JsonValueKind.Array)
                {
                    foreach (var participant in participants.EnumerateArray())
                    {
                        var id = participant.GetPropertyOrDefault("participantID")
                                 ?? participant.GetPropertyOrDefault("participantId");
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        var name = participant.GetPropertyOrDefault("name");
                        var country = participant.GetPropertyOrDefault("countryCode");
                        return new PeppolParticipant(id, name, country);
                    }
                }
                else if (root.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
                {
                    foreach (var match in matches.EnumerateArray())
                    {
                        var id = match.GetPropertyOrDefault("participantID")
                                 ?? match.GetPropertyOrDefault("participantId");
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        var name = match.GetPropertyOrDefault("name");
                        var country = match.GetPropertyOrDefault("countryCode");
                        return new PeppolParticipant(id, name, country);
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
                yield return $"0208:{upper}";
                yield return $"0088:{upper}";
            }
            else
            {
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