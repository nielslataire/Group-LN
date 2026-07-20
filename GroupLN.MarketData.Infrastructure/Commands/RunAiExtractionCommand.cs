using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Commands;

public record RunAiExtractionResult(
    int Candidates,
    int FromCache,
    int NewExtractions,
    int NoProjectName,
    int Skipped,
    int Errors);

public class RunAiExtractionCommand
{
    private readonly MarketDataDbContext    _db;
    private readonly IAiProjectExtractionService _aiService;
    private readonly ILogger<RunAiExtractionCommand> _logger;

    public RunAiExtractionCommand(
        MarketDataDbContext db,
        IAiProjectExtractionService aiService,
        ILogger<RunAiExtractionCommand> logger)
    {
        _db        = db;
        _aiService = aiService;
        _logger    = logger;
    }

    /// <param name="sourceName">
    /// null = alle bronnen. Opgeven om te beperken tot één bron (bijv. "Immoweb").
    /// </param>
    /// <param name="skipCached">
    /// true (standaard) = sla assets over waarvoor al een cache-entry bestaat op SourceName+ExternalId.
    /// false = voer ook opnieuw uit voor gecachete assets (nuttig bij model-upgrade).
    /// </param>
    public async Task<RunAiExtractionResult> ExecuteAsync(
        string? sourceName = null,
        bool skipCached    = true,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[AI-Extractie] Gestart. Source={Source} | SkipCached={Skip}",
            sourceName ?? "alle", skipCached);

        // ── 1. Project group assets ophalen ────────────────────────────────────
        var assetsQuery = _db.MarketAssets
            .AsNoTracking()
            .Where(a => a.IsProjectGroup && a.IsActive);

        // Per-bron filter
        List<int>? sourceIds = null;
        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            sourceIds = await _db.CrawlerSources
                .Where(s => s.Name == sourceName && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync(ct);

            if (sourceIds.Count == 0)
            {
                _logger.LogWarning("[AI-Extractie] Geen actieve bron gevonden met naam '{Name}'.", sourceName);
                return new RunAiExtractionResult(0, 0, 0, 0, 0, 0);
            }
        }

        // ── 2. Kandidaat-assets laden (id + adresdata + developer) ───────────────
        var candidates = await assetsQuery
            .Select(a => new
            {
                a.Id,
                a.AssetKey,
                a.Street,
                a.HouseNumber,
                a.PostalCode,
                a.City,
                a.DeveloperName
            })
            .ToListAsync(ct);

        _logger.LogInformation("[AI-Extractie] {Count} project group assets gevonden.", candidates.Count);

        if (candidates.Count == 0)
            return new RunAiExtractionResult(0, 0, 0, 0, 0, 0);

        // ── 3. Latest active listing per asset (voor Title + Url + SourceId) ────
        var assetIds     = candidates.Select(c => c.Id).ToList();
        var latestListing = await _db.MarketListings
            .AsNoTracking()
            .Where(l => assetIds.Contains(l.MarketAssetId) && l.IsActive)
            .GroupBy(l => l.MarketAssetId)
            .Select(g => new
            {
                AssetId  = g.Key,
                SourceId = g.Max(l => l.SourceId),
                Url      = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Url).FirstOrDefault(),
                Title    = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.AssetId, ct);

        // ── 4. SourceName lookup ────────────────────────────────────────────────
        var allSourceIds = latestListing.Values
            .Select(l => l.SourceId)
            .Distinct()
            .ToList();

        var sourceNameMap = await _db.CrawlerSources
            .Where(s => allSourceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        // ── 5. Al gecachete ExternalIds (voor skip-logica) ───────────────────────
        HashSet<string>? cachedExternalIds = null;
        if (skipCached)
        {
            var relevantExternalIds = candidates.Select(c => c.AssetKey).ToList();
            var cachedIds = await _db.ProjectAiExtractionCaches
                .AsNoTracking()
                .Where(c => relevantExternalIds.Contains(c.ExternalId))
                .Select(c => c.ExternalId)
                .Distinct()
                .ToListAsync(ct);
            cachedExternalIds = [.. cachedIds];
            _logger.LogInformation(
                "[AI-Extractie] {Cached} van {Total} assets al in cache (worden overgeslagen).",
                cachedIds.Count, candidates.Count);
        }

        // ── 6. Per asset extractie uitvoeren ────────────────────────────────────
        int fromCache = 0, newExtractions = 0, noProjectName = 0, skipped = 0, errors = 0;

        foreach (var asset in candidates)
        {
            if (ct.IsCancellationRequested) break;

            // SkipCached: al gecachet op ExternalId → overslaan
            if (skipCached && cachedExternalIds!.Contains(asset.AssetKey))
            {
                skipped++;
                continue;
            }

            latestListing.TryGetValue(asset.Id, out var listing);
            var srcId      = listing?.SourceId;
            var srcName    = srcId.HasValue && sourceNameMap.TryGetValue(srcId.Value, out var sn) ? sn : "unknown";

            // Filter op bron-naam indien opgegeven
            if (sourceIds is not null && (srcId is null || !sourceIds.Contains(srcId.Value)))
            {
                skipped++;
                continue;
            }

            var address = string.Join(", ", new[]
            {
                asset.Street is { Length: > 0 } s
                    ? s + (asset.HouseNumber is { Length: > 0 } hn ? " " + hn : "")
                    : null,
                asset.PostalCode is { Length: > 0 } pc
                    ? pc + (asset.City is { Length: > 0 } c ? " " + c : "")
                    : null
            }.Where(p => p is not null));

            var input = new AiProjectExtractionInput
            {
                SourceName  = srcName,
                ExternalId  = asset.AssetKey,
                Url         = listing?.Url,
                RawTitle    = listing?.Title,
                Address     = address.Length > 0 ? address : null,
                Developer   = asset.DeveloperName
            };

            try
            {
                var result = await _aiService.ExtractAsync(input, ct);

                if (result is null)
                {
                    skipped++;
                    continue;
                }

                if (result.FromCache) fromCache++;
                else                  newExtractions++;

                if (string.IsNullOrEmpty(result.ProjectName))
                    noProjectName++;
                else
                    _logger.LogInformation(
                        "[AI-Extractie] {ExternalId} | '{Name}' ({Origin}) | confidence={Conf}%",
                        asset.AssetKey,
                        result.ProjectName,
                        result.FromCache ? "cache" : "API",
                        result.ProjectNameConfidence);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "[AI-Extractie] Fout bij {ExternalId}: {Err}",
                    asset.AssetKey, ex.Message);
                errors++;
            }
        }

        var summary = new RunAiExtractionResult(
            candidates.Count, fromCache, newExtractions, noProjectName, skipped, errors);

        _logger.LogInformation(
            "[AI-Extractie] Klaar. " +
            "Candidates={C} | FromCache={Cache} | NieuweExtracts={New} | " +
            "GeenProjectnaam={NoPrj} | Overgeslagen={Skip} | Fouten={Err}",
            summary.Candidates, summary.FromCache, summary.NewExtractions,
            summary.NoProjectName, summary.Skipped, summary.Errors);

        return summary;
    }
}
