using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GroupLN.MarketData.Persistence;

namespace GroupLN.MarketData.Infrastructure.Commands;

/// <summary>
/// Verwijdert alle geïmporteerde crawler/immo-data in FK-veilige volgorde binnen één transactie.
/// Referentie- en geo-data (CrawlerSource, GeoMunicipality, GeoMunicipalSection) blijven intact.
/// Gebruik altijd eerst zonder --confirm voor een droge preview.
/// </summary>
public class ResetMarketDataCommand
{
    private readonly MarketDataDbContext _db;
    private readonly ILogger<ResetMarketDataCommand> _logger;

    public ResetMarketDataCommand(MarketDataDbContext db, ILogger<ResetMarketDataCommand> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <param name="confirm">Voer echte deletes uit. False = droge preview.</param>
    /// <param name="includeGeocodingCache">Wis ook de geocoding-cache (optioneel).</param>
    public async Task<ResetMarketDataResult> ExecuteAsync(
        bool confirm,
        bool includeGeocodingCache = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[ResetMarketData] Modus: {Mode} | GeocodingCache: {Geo}",
            confirm ? "CONFIRM (echte delete)" : "DRY RUN (preview)",
            includeGeocodingCache ? "ja" : "nee");

        var counts = await CountAllTablesAsync(includeGeocodingCache, ct);
        LogPreview(counts, includeGeocodingCache);

        if (!confirm)
        {
            _logger.LogWarning(
                "[ResetMarketData] DRY RUN voltooid. Geen data verwijderd. " +
                "Voeg --confirm toe om de cleanup effectief uit te voeren.");
            return new ResetMarketDataResult(Confirmed: false, Counts: counts);
        }

        _logger.LogWarning("[ResetMarketData] BEVESTIGD — data wordt verwijderd...");

        await ExecuteInTransactionAsync(includeGeocodingCache, ct);

        _logger.LogInformation("[ResetMarketData] Cleanup voltooid.");
        return new ResetMarketDataResult(Confirmed: true, Counts: counts);
    }

    // ── Preview logging ───────────────────────────────────────────────────────

    private void LogPreview(TableRowCounts c, bool includeGeo)
    {
        _logger.LogInformation(
            "[ResetMarketData] Preview te verwijderen records:\n" +
            "  MarketAsset              : {MarketAsset,10}\n" +
            "  MarketListing            : {MarketListing,10}\n" +
            "  MarketListingSnapshot    : {Snapshot,10}\n" +
            "  MarketListingPriceHistory: {PriceHistory,10}\n" +
            "  MarketAssetMatchCandidate: {MatchCandidate,10}\n" +
            "  ProjectGroupSnapshot     : {GroupSnapshot,10}\n" +
            "  ProjectGroupKpi          : {Kpi,10}\n" +
            "  CanonicalProjectAsset    : {CanonAsset,10}\n" +
            "  CanonicalProject         : {Canon,10}\n" +
            "  MarketAreaStatistics     : {AreaStats,10}\n" +
            "  CrawlerRun               : {CrawlerRun,10}\n" +
            "  ProjectPhotoHash         : {PhotoHash,10}\n" +
            "  ProjectAiExtractionCache : {AiCache,10}\n" +
            "  GeocodingCache           : {GeoCache}",
            c.MarketAssets,
            c.MarketListings,
            c.MarketListingSnapshots,
            c.MarketListingPriceHistories,
            c.MarketAssetMatchCandidates,
            c.ProjectGroupSnapshots,
            c.ProjectGroupKpis,
            c.CanonicalProjectAssets,
            c.CanonicalProjects,
            c.MarketAreaStatistics,
            c.CrawlerRuns,
            c.ProjectPhotoHashes,
            c.ProjectAiExtractionCaches,
            includeGeo ? c.GeocodingCaches.ToString() : "overgeslagen");

        _logger.LogInformation(
            "[ResetMarketData] NIET verwijderd (referentiedata): " +
            "CrawlerSource, GeoMunicipality, GeoMunicipalSection, __EFMigrationsHistory");
    }

    // ── Transactie met FK-veilige volgorde ────────────────────────────────────

    private async Task ExecuteInTransactionAsync(bool includeGeocodingCache, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // ── Stap 1: tabellen zonder FK naar crawler-data ──────────────
                await DeleteAndLog("ProjectAiExtractionCache",
                    () => _db.ProjectAiExtractionCaches.ExecuteDeleteAsync(ct));

                if (includeGeocodingCache)
                    await DeleteAndLog("GeocodingCache",
                        () => _db.GeocodingCaches.ExecuteDeleteAsync(ct));

                await DeleteAndLog("MarketAreaStatistics",
                    () => _db.MarketAreaStatistics.ExecuteDeleteAsync(ct));

                // ── Stap 2: kinderen van MarketListing (Cascade, maar expliciete volgorde) ──
                await DeleteAndLog("MarketListingPriceHistory",
                    () => _db.MarketListingPriceHistories.ExecuteDeleteAsync(ct));

                await DeleteAndLog("MarketListingSnapshot",
                    () => _db.MarketListingSnapshots.ExecuteDeleteAsync(ct));

                // ── Stap 3: MarketListing (FK → MarketAsset Restrict, FK → CrawlerSource Restrict) ──
                await DeleteAndLog("MarketListing",
                    () => _db.MarketListings.ExecuteDeleteAsync(ct));

                // ── Stap 4: tabellen met FK → MarketAsset (Restrict/Cascade) ──
                await DeleteAndLog("ProjectPhotoHash",
                    () => _db.ProjectPhotoHashes.ExecuteDeleteAsync(ct));

                await DeleteAndLog("MarketAssetMatchCandidate",
                    () => _db.MarketAssetMatchCandidates.ExecuteDeleteAsync(ct));

                await DeleteAndLog("ProjectGroupSnapshot",
                    () => _db.ProjectGroupSnapshots.ExecuteDeleteAsync(ct));

                await DeleteAndLog("ProjectGroupKpi",
                    () => _db.ProjectGroupKpis.ExecuteDeleteAsync(ct));

                // ── Stap 5: CanonicalProjectAsset vóór CanonicalProject ───────
                await DeleteAndLog("CanonicalProjectAsset",
                    () => _db.CanonicalProjectAssets.ExecuteDeleteAsync(ct));

                await DeleteAndLog("CanonicalProject",
                    () => _db.CanonicalProjects.ExecuteDeleteAsync(ct));

                // ── Stap 6: MarketAsset — eerst child-units, dan parents ──────
                await DeleteAndLog("MarketAsset (units)",
                    () => _db.MarketAssets
                        .Where(a => a.ParentMarketAssetId != null)
                        .ExecuteDeleteAsync(ct));

                await DeleteAndLog("MarketAsset (projects/loose)",
                    () => _db.MarketAssets.ExecuteDeleteAsync(ct));

                // ── Stap 7: CrawlerRun (FK → CrawlerSource Restrict, source bewaard) ──
                await DeleteAndLog("CrawlerRun",
                    () => _db.CrawlerRuns.ExecuteDeleteAsync(ct));

                // ── Stap 8: Identity reseed ────────────────────────────────────
                await ReseedIdentitiesAsync(includeGeocodingCache, ct);

                await tx.CommitAsync(ct);
                _logger.LogInformation("[ResetMarketData] Transactie gecommit.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ResetMarketData] Fout tijdens cleanup — transactie teruggerold.");
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task DeleteAndLog(string tableName, Func<Task<int>> deleteFunc)
    {
        var deleted = await deleteFunc();
        _logger.LogInformation("[ResetMarketData] {Table}: {Count} rijen verwijderd.", tableName, deleted);
    }

    // ── IDENTITY reseed ───────────────────────────────────────────────────────

    private async Task ReseedIdentitiesAsync(bool includeGeocodingCache, CancellationToken ct)
    {
        var tables = new List<string>
        {
            "MarketAsset",
            "MarketListing",
            "MarketListingSnapshot",
            "MarketListingPriceHistory",
            "MarketAssetMatchCandidate",
            "ProjectGroupSnapshot",
            "ProjectGroupKpi",
            "CanonicalProject",
            "CanonicalProjectAsset",
            "CrawlerRun",
            "MarketAreaStatistics",
            "ProjectPhotoHash",
            "ProjectAiExtractionCache",
        };

        if (includeGeocodingCache) tables.Add("GeocodingCache");

        foreach (var table in tables)
        {
            try
            {
                // Tabelnamen zijn hardcoded in deze methode — geen SQL-injection risico.
#pragma warning disable EF1002
                await _db.Database.ExecuteSqlRawAsync(
                    $"DBCC CHECKIDENT ('{table}', RESEED, 0)", ct);
#pragma warning restore EF1002
                _logger.LogDebug("[ResetMarketData] Identity reseed: {Table}", table);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[ResetMarketData] Identity reseed mislukt voor {Table} — overgeslagen.", table);
            }
        }

        _logger.LogInformation("[ResetMarketData] Identity reseed voltooid voor {Count} tabellen.", tables.Count);
    }

    // ── Tellen ────────────────────────────────────────────────────────────────

    private async Task<TableRowCounts> CountAllTablesAsync(bool includeGeocodingCache, CancellationToken ct)
    {
        return new TableRowCounts
        {
            MarketAssets                = await _db.MarketAssets.CountAsync(ct),
            MarketListings              = await _db.MarketListings.CountAsync(ct),
            MarketListingSnapshots      = await _db.MarketListingSnapshots.CountAsync(ct),
            MarketListingPriceHistories = await _db.MarketListingPriceHistories.CountAsync(ct),
            MarketAssetMatchCandidates  = await _db.MarketAssetMatchCandidates.CountAsync(ct),
            ProjectGroupSnapshots       = await _db.ProjectGroupSnapshots.CountAsync(ct),
            ProjectGroupKpis            = await _db.ProjectGroupKpis.CountAsync(ct),
            CanonicalProjectAssets      = await _db.CanonicalProjectAssets.CountAsync(ct),
            CanonicalProjects           = await _db.CanonicalProjects.CountAsync(ct),
            MarketAreaStatistics        = await _db.MarketAreaStatistics.CountAsync(ct),
            CrawlerRuns                 = await _db.CrawlerRuns.CountAsync(ct),
            ProjectPhotoHashes          = await _db.ProjectPhotoHashes.CountAsync(ct),
            ProjectAiExtractionCaches   = await _db.ProjectAiExtractionCaches.CountAsync(ct),
            GeocodingCaches             = includeGeocodingCache
                                            ? await _db.GeocodingCaches.CountAsync(ct)
                                            : 0,
        };
    }
}

// ── Result types ──────────────────────────────────────────────────────────────

public record ResetMarketDataResult(bool Confirmed, TableRowCounts Counts);

public record TableRowCounts
{
    public int MarketAssets                { get; init; }
    public int MarketListings              { get; init; }
    public int MarketListingSnapshots      { get; init; }
    public int MarketListingPriceHistories { get; init; }
    public int MarketAssetMatchCandidates  { get; init; }
    public int ProjectGroupSnapshots       { get; init; }
    public int ProjectGroupKpis            { get; init; }
    public int CanonicalProjectAssets      { get; init; }
    public int CanonicalProjects           { get; init; }
    public int MarketAreaStatistics        { get; init; }
    public int CrawlerRuns                 { get; init; }
    public int ProjectPhotoHashes          { get; init; }
    public int ProjectAiExtractionCaches   { get; init; }
    public int GeocodingCaches             { get; init; }
}
