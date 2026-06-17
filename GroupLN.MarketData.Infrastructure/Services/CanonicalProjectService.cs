using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Infrastructure.Deduplication;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class CanonicalProjectService : ICanonicalProjectService
{
    private readonly MarketDataDbContext _db;
    private readonly ILogger<CanonicalProjectService> _logger;

    public CanonicalProjectService(MarketDataDbContext db, ILogger<CanonicalProjectService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Rebuild ───────────────────────────────────────────────────────────────

    public async Task<RebuildCanonicalResult> RebuildCanonicalProjectsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[CanonicalRebuild] Starten...");

        // 1. Laad alle actieve projectgroepen
        var projectGroups = await _db.MarketAssets
            .Where(a => a.IsProjectGroup && a.ParentMarketAssetId == null && a.IsActive)
            .Select(a => new ProjectGroupData(
                a.Id,
                a.PostalCode,
                a.Street,
                a.HouseNumber,
                a.Latitude,
                a.Longitude,
                a.DeveloperName,
                a.GeoMunicipalityId,
                a.GeoMunicipalSectionId,
                a.LastSeenAt))
            .AsNoTracking()
            .ToListAsync(ct);

        _logger.LogInformation("[CanonicalRebuild] {Count} actieve projectgroepen geladen.", projectGroups.Count);

        if (projectGroups.Count == 0)
            return new RebuildCanonicalResult(0, 0, 0, 0, 0);

        var allIds = projectGroups.Select(p => p.Id).ToHashSet();

        // 2. Meest recente listing-titel per asset
        var titleByAsset = await _db.MarketListings
            .Where(l => allIds.Contains(l.MarketAssetId) && l.Title != null)
            .GroupBy(l => l.MarketAssetId)
            .Select(g => new
            {
                AssetId = g.Key,
                Title   = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).First()
            })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.AssetId, x => x.Title ?? "", ct);

        // 3. Aantal child units per project
        var unitCountByProject = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && allIds.Contains(a.ParentMarketAssetId.Value))
            .GroupBy(a => a.ParentMarketAssetId!.Value)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, ct);

        // 4. Kwalificerende match-candidates laden (Exact + Probable ≥ 0.80)
        var qualifyingMatches = await _db.MarketAssetMatchCandidates
            .Where(c => c.MatchType == "Project"
                     && !c.IsRejected
                     && c.CandidateMarketAssetId.HasValue
                     && allIds.Contains(c.ExistingMarketAssetId)
                     && allIds.Contains(c.CandidateMarketAssetId!.Value)
                     && (c.MatchLevel == "Exact"
                         || (c.MatchLevel == "Probable" && c.MatchScore >= 0.80m)))
            .Select(c => new MatchData(
                c.ExistingMarketAssetId,
                c.CandidateMarketAssetId!.Value,
                c.MatchLevel!,
                c.MatchScore,
                c.MatchReason))
            .AsNoTracking()
            .ToListAsync(ct);

        // 5. Possible matches enkel tellen voor logging
        var possibleCount = await _db.MarketAssetMatchCandidates
            .Where(c => c.MatchType == "Project"
                     && !c.IsRejected
                     && c.CandidateMarketAssetId.HasValue
                     && allIds.Contains(c.ExistingMarketAssetId)
                     && c.MatchLevel == "Possible")
            .CountAsync(ct);

        _logger.LogInformation(
            "[CanonicalRebuild] {Matches} Exact/Probable matches geladen, {Possible} Possible overgeslagen.",
            qualifyingMatches.Count, possibleCount);

        // 6. Union-Find: groepeer assets die samenvallen
        var parent = projectGroups.ToDictionary(p => p.Id, p => p.Id);

        long Find(long id)
        {
            while (parent[id] != id)
            {
                parent[id] = parent[parent[id]]; // path compression
                id = parent[id];
            }
            return id;
        }

        void Union(long a, long b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb) parent[ra] = rb;
        }

        foreach (var m in qualifyingMatches)
            Union(m.ExistingId, m.CandidateId);

        // 7. Bouw canonical groups (connected components)
        var groups = projectGroups
            .GroupBy(p => Find(p.Id))
            .ToList();

        // 8. Bestaande canonical records ophalen voor upsert
        var existingCpas = await _db.CanonicalProjectAssets
            .Where(a => allIds.Contains(a.MarketAssetId))
            .Select(a => new { a.MarketAssetId, a.CanonicalProjectId })
            .AsNoTracking()
            .ToListAsync(ct);

        var existingCpIdByAsset = existingCpas
            .ToDictionary(x => x.MarketAssetId, x => x.CanonicalProjectId);

        // Verwijder bestaande canonical assets voor de scope (full rebuild voor actieve groepen)
        await _db.CanonicalProjectAssets
            .Where(a => allIds.Contains(a.MarketAssetId))
            .ExecuteDeleteAsync(ct);

        int created = 0, updated = 0, linked = 0;
        var now = DateTime.UtcNow;

        // 9. Per canonical group: upsert CanonicalProject + CanonicalProjectAssets
        foreach (var group in groups)
        {
            var groupList    = group.ToList();
            var groupAssetIds = groupList.Select(p => p.Id).ToList();

            // Primary selecteren
            var primaryId = SelectPrimaryAsset(groupList, titleByAsset, unitCountByProject);
            var primary   = groupList.First(p => p.Id == primaryId);

            // Canonical naam kiezen
            var canonicalName   = ChooseCanonicalName(primaryId, groupAssetIds, titleByAsset, primary);
            var normalizedName  = ProjectNameNormalizer.Normalize(canonicalName);

            // Canonical project ophalen of aanmaken
            CanonicalProject? cp = null;
            foreach (var id in groupAssetIds)
            {
                if (!existingCpIdByAsset.TryGetValue(id, out var cpId)) continue;
                cp = await _db.CanonicalProjects.FindAsync([cpId], ct);
                if (cp != null) break;
            }

            if (cp is null)
            {
                cp = new CanonicalProject { CreatedAt = now };
                _db.CanonicalProjects.Add(cp);
                created++;
            }
            else
            {
                updated++;
            }

            cp.CanonicalName         = canonicalName;
            cp.NormalizedName        = normalizedName;
            cp.GeoMunicipalityId     = primary.GeoMunicipalityId;
            cp.GeoMunicipalSectionId = primary.GeoMunicipalSectionId;
            cp.PostalCode            = primary.PostalCode;
            cp.Street                = primary.Street;
            cp.HouseNumber           = primary.HouseNumber;
            cp.Latitude              = primary.Latitude;
            cp.Longitude             = primary.Longitude;
            cp.DeveloperName         = primary.DeveloperName;
            cp.UpdatedAt             = now;
            cp.IsActive              = true;

            await _db.SaveChangesAsync(ct);

            // CanonicalProjectAssets aanmaken
            foreach (var assetId in groupAssetIds)
            {
                var matchInfo = qualifyingMatches.FirstOrDefault(m =>
                    (m.ExistingId == assetId && groupAssetIds.Contains(m.CandidateId)) ||
                    (m.CandidateId == assetId && groupAssetIds.Contains(m.ExistingId)));

                var isSolo   = groupAssetIds.Count == 1;
                var matchLvl = isSolo ? "Primary" : (matchInfo?.MatchLevel ?? "Probable");
                var score    = isSolo ? 1.0m       : (matchInfo?.MatchScore ?? 0.80m);

                _db.CanonicalProjectAssets.Add(new CanonicalProjectAsset
                {
                    CanonicalProjectId = cp.Id,
                    MarketAssetId      = assetId,
                    IsPrimary          = assetId == primaryId,
                    MatchLevel         = matchLvl,
                    MatchScore         = score,
                    MatchReason        = matchInfo?.MatchReason,
                    Source             = isSolo ? "Primary" : "DeduplicationMatch",
                    CreatedAt          = now,
                });
                linked++;
            }

            await _db.SaveChangesAsync(ct);
        }

        // 10. Deactiveer canonical projects zonder assets meer (orphans na vorige runs)
        var orphanCount = await _db.CanonicalProjects
            .Where(cp => cp.IsActive && !_db.CanonicalProjectAssets.Any(a => a.CanonicalProjectId == cp.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(cp => cp.IsActive, false)
                .SetProperty(cp => cp.UpdatedAt, now), ct);

        if (orphanCount > 0)
            _logger.LogInformation("[CanonicalRebuild] {Count} orphan canonical projects gedeactiveerd.", orphanCount);

        _logger.LogInformation(
            "[CanonicalRebuild] Klaar. Groepen={Groups} | Aangemaakt={Created} | Bijgewerkt={Updated} | Assets={Linked} | Possible overgeslagen={Possible}",
            groups.Count, created, updated, linked, possibleCount);

        return new RebuildCanonicalResult(projectGroups.Count, created, updated, linked, possibleCount);
    }

    // ── Lookup methodes ───────────────────────────────────────────────────────

    public async Task<CanonicalProject?> GetCanonicalProjectForAssetAsync(long marketAssetId, CancellationToken ct = default)
    {
        var link = await _db.CanonicalProjectAssets
            .Where(a => a.MarketAssetId == marketAssetId)
            .Select(a => a.CanonicalProject)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return link?.IsActive == true ? link : null;
    }

    public async Task<CanonicalProjectStatisticsDto?> GetCanonicalProjectStatisticsAsync(long canonicalProjectId, CancellationToken ct = default)
    {
        var cp = await _db.CanonicalProjects
            .Where(c => c.Id == canonicalProjectId && c.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (cp is null) return null;

        var linkedAssetIds = await _db.CanonicalProjectAssets
            .Where(a => a.CanonicalProjectId == canonicalProjectId)
            .Select(a => a.MarketAssetId)
            .ToListAsync(ct);

        var units = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && linkedAssetIds.Contains(a.ParentMarketAssetId.Value))
            .Select(a => new { a.SaleStatus, a.LifecycleStatus })
            .AsNoTracking()
            .ToListAsync(ct);

        var soldCount      = units.Count(u => u.SaleStatus == SaleStatus.Sold);
        var availableCount = units.Count(u => u.SaleStatus == SaleStatus.Available);
        var reservedCount  = units.Count(u => u.SaleStatus == SaleStatus.Reserved || u.SaleStatus == SaleStatus.Option);

        // Prijzen via actieve listings
        var activeListingIds = await _db.MarketListings
            .Where(l => linkedAssetIds.Contains(l.MarketAssetId) && l.IsActive)
            .Select(l => l.Id)
            .ToListAsync(ct);

        decimal? gemPrijs = null, gemPpSqm = null;
        if (activeListingIds.Count > 0)
        {
            var snapshots = await _db.MarketListingSnapshots
                .Where(s => activeListingIds.Contains(s.MarketListingId))
                .GroupBy(s => s.MarketListingId)
                .Select(g => new { g.OrderByDescending(s => s.SnapshotDate).First().AskingPrice, g.OrderByDescending(s => s.SnapshotDate).First().PricePerSqm })
                .AsNoTracking()
                .ToListAsync(ct);

            var prices  = snapshots.Where(s => s.AskingPrice.HasValue).Select(s => s.AskingPrice!.Value).ToList();
            var ppSqms  = snapshots.Where(s => s.PricePerSqm.HasValue).Select(s => s.PricePerSqm!.Value).ToList();
            if (prices.Count  > 0) gemPrijs  = Math.Round(prices.Average(), 0);
            if (ppSqms.Count  > 0) gemPpSqm  = Math.Round(ppSqms.Average(), 0);
        }

        return new CanonicalProjectStatisticsDto
        {
            CanonicalProjectId = canonicalProjectId,
            CanonicalName      = cp.CanonicalName,
            LinkedAssetCount   = linkedAssetIds.Count,
            TotaalUnits        = units.Count,
            VerkochteUnits     = soldCount,
            BeschikbareUnits   = availableCount,
            GereserveerdeUnits = reservedCount,
            Verkoopgraad       = units.Count > 0 ? Math.Round((decimal)soldCount / units.Count * 100, 1) : 0m,
            GemiddeldePrijs    = gemPrijs,
            GemiddeldePrijsPerM2 = gemPpSqm,
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static long SelectPrimaryAsset(
        List<ProjectGroupData> group,
        Dictionary<long, string> titleByAsset,
        Dictionary<long, int> unitCountByProject)
    {
        return group
            .OrderByDescending(p => !IsTechnicalTitle(titleByAsset.GetValueOrDefault(p.Id)) ? 1 : 0)
            .ThenByDescending(p => unitCountByProject.GetValueOrDefault(p.Id))
            .ThenByDescending(p => p.LastSeenAt)
            .First().Id;
    }

    private static string ChooseCanonicalName(
        long primaryId,
        List<long> allIds,
        Dictionary<long, string> titleByAsset,
        ProjectGroupData primary)
    {
        // Voorkeur: niet-technische titel van de primary
        if (titleByAsset.TryGetValue(primaryId, out var primaryTitle) && !IsTechnicalTitle(primaryTitle))
            return primaryTitle;

        // Fallback: eerste niet-technische titel uit de groep
        foreach (var id in allIds)
        {
            if (titleByAsset.TryGetValue(id, out var t) && !IsTechnicalTitle(t))
                return t;
        }

        // Last resort: adres of technische titel van primary
        if (!string.IsNullOrEmpty(primary.Street))
        {
            var adres = primary.Street.Trim();
            if (!string.IsNullOrEmpty(primary.HouseNumber)) adres += $" {primary.HouseNumber.Trim()}";
            return adres;
        }

        return titleByAsset.TryGetValue(primaryId, out var tech) ? tech : $"Project {primaryId}";
    }

    private static bool IsTechnicalTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return true;
        return System.Text.RegularExpressions.Regex.IsMatch(
            title.Trim(),
            @"^(?:Project|Listing)\s+\d+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // ── Records ───────────────────────────────────────────────────────────────

    private sealed record ProjectGroupData(
        long Id,
        string? PostalCode,
        string? Street,
        string? HouseNumber,
        decimal? Latitude,
        decimal? Longitude,
        string? DeveloperName,
        int? GeoMunicipalityId,
        int? GeoMunicipalSectionId,
        DateTime LastSeenAt);

    private sealed record MatchData(
        long ExistingId,
        long CandidateId,
        string MatchLevel,
        decimal MatchScore,
        string? MatchReason);
}
