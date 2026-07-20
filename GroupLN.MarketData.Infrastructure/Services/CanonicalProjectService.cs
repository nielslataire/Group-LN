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
        _logger.LogInformation(
            "[CanonicalRebuild] Starten | NonDestructive=true | OriginalsPreserved=true | " +
            "Opmerking=Geen MarketAssets/MarketListings/Snapshots/PriceHistory worden verwijderd. " +
            "CanonicalProject is enkel een virtuele groepering bovenop de bestaande brondata.");

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

        // 3b. AI-extractie: heeft het project een AI-projectnaam?
        var aiProjectNameByAsset = await _db.ProjectAiExtractionCaches
            .Where(a => !string.IsNullOrEmpty(a.ExtractedProjectName))
            .Select(a => new { a.ExternalId, a.ExtractedProjectName, a.ProjectNameConfidence, a.ExtractedDeveloper })
            .AsNoTracking()
            .ToListAsync(ct);

        // ExternalId → asset Id mapping via listings
        var listingsByAsset = await _db.MarketListings
            .Where(l => allIds.Contains(l.MarketAssetId))
            .Select(l => new { l.MarketAssetId, l.ExternalId })
            .AsNoTracking()
            .ToListAsync(ct);

        var externalIdToAsset = listingsByAsset
            .GroupBy(l => l.ExternalId)
            .ToDictionary(g => g.Key, g => g.First().MarketAssetId);

        var aiScoreByAsset = new Dictionary<long, int>();
        foreach (var ai in aiProjectNameByAsset)
        {
            if (externalIdToAsset.TryGetValue(ai.ExternalId, out var assetId))
            {
                var score = 0;
                if (!string.IsNullOrEmpty(ai.ExtractedProjectName)) score += 3;
                if (ai.ProjectNameConfidence >= 80) score += 2;
                if (!string.IsNullOrEmpty(ai.ExtractedDeveloper)) score += 1;
                aiScoreByAsset[assetId] = score;
            }
        }

        // 3c. Foto's per project
        var photoCountByAsset = await _db.ProjectPhotoHashes
            .Where(p => allIds.Contains(p.MarketAssetId))
            .GroupBy(p => p.MarketAssetId)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.AssetId, x => x.Count, ct);

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

            // Primary selecteren o.b.v. gewogen score
            var (primaryId, repReason) = SelectPrimaryAsset(
                groupList, titleByAsset, unitCountByProject, aiScoreByAsset, photoCountByAsset);
            var primary   = groupList.First(p => p.Id == primaryId);

            _logger.LogInformation(
                "[CanonicalRebuild] RepresentativeSelected | AssetId={Id} | Reason={Reason} | GroupSize={Size}",
                primaryId, repReason, groupAssetIds.Count);

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
            "[CanonicalRebuild] Klaar | NonDestructive=true | OriginalsPreserved=true | " +
            "GroupsCreated={Created} | GroupsUpdated={Updated} | AssetsLinked={Linked} | " +
            "RepresentativesSelected={Groups} | PossibleOvergeslagen={Possible}",
            created, updated, linked, groups.Count, possibleCount);

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

    // ── CPM Core API ─────────────────────────────────────────────────────────

    public async Task<List<CanonicalProjectSummaryDto>> GetCanonicalProjectsAsync(CancellationToken ct = default)
    {
        var projects = await _db.CanonicalProjects
            .Where(cp => cp.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<CanonicalProjectSummaryDto>(projects.Count);

        foreach (var cp in projects)
        {
            var linkedAssetIds = await _db.CanonicalProjectAssets
                .Where(a => a.CanonicalProjectId == cp.Id)
                .Select(a => a.MarketAssetId)
                .ToListAsync(ct);

            var listings = await _db.MarketListings
                .Where(l => linkedAssetIds.Contains(l.MarketAssetId) && l.IsActive)
                .Include(l => l.Source)
                .AsNoTracking()
                .ToListAsync(ct);

            var units = await _db.MarketAssets
                .Where(a => a.ParentMarketAssetId.HasValue && linkedAssetIds.Contains(a.ParentMarketAssetId.Value))
                .AsNoTracking()
                .ToListAsync(ct);

            result.Add(new CanonicalProjectSummaryDto
            {
                CanonicalProjectId = cp.Id,
                CanonicalName      = cp.CanonicalName,
                PostalCode         = cp.PostalCode,
                Street             = cp.Street,
                HouseNumber        = cp.HouseNumber,
                DeveloperName      = cp.DeveloperName,
                Latitude           = cp.Latitude,
                Longitude          = cp.Longitude,
                MinPrice           = listings.Where(l => l.AskingPrice.HasValue).Select(l => l.AskingPrice!.Value).DefaultIfEmpty().Min(),
                MaxPrice           = listings.Where(l => l.AskingPrice.HasValue).Select(l => l.AskingPrice!.Value).DefaultIfEmpty().Max(),
                TotalUnits         = units.Count,
                AvailableUnits     = units.Count(u => u.SaleStatus == SaleStatus.Available),
                SoldUnits          = units.Count(u => u.SaleStatus == SaleStatus.Sold),
                ReservedUnits      = units.Count(u => u.SaleStatus is SaleStatus.Reserved or SaleStatus.Option),
                Verkoopgraad       = units.Count > 0 ? Math.Round((decimal)units.Count(u => u.SaleStatus == SaleStatus.Sold) / units.Count * 100, 1) : 0m,
                LinkedSourceCount  = linkedAssetIds.Count,
                SourceNames        = listings.Select(l => l.Source.Name).Distinct().OrderBy(n => n).ToList(),
                LastSeenAt         = listings.Count > 0 ? listings.Max(l => l.LastSeenAt) : cp.UpdatedAt,
                UpdatedAt          = cp.UpdatedAt,
            });
        }

        return result;
    }

    public async Task<List<CanonicalProjectSourceDto>> GetCanonicalProjectSourcesAsync(
        long canonicalProjectId, CancellationToken ct = default)
    {
        var cpas = await _db.CanonicalProjectAssets
            .Where(a => a.CanonicalProjectId == canonicalProjectId)
            .AsNoTracking()
            .ToListAsync(ct);

        if (cpas.Count == 0) return [];

        var assetIds = cpas.Select(a => a.MarketAssetId).ToList();

        var listings = await _db.MarketListings
            .Where(l => assetIds.Contains(l.MarketAssetId))
            .Include(l => l.Source)
            .OrderByDescending(l => l.LastSeenAt)
            .AsNoTracking()
            .ToListAsync(ct);

        var unitCounts = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && assetIds.Contains(a.ParentMarketAssetId.Value))
            .GroupBy(a => a.ParentMarketAssetId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var aiData = await _db.ProjectAiExtractionCaches
            .Where(a => !string.IsNullOrEmpty(a.ExtractedProjectName))
            .AsNoTracking()
            .ToListAsync(ct);

        var listingExtIdToAsset = listings
            .GroupBy(l => l.ExternalId)
            .ToDictionary(g => g.Key, g => g.First().MarketAssetId);

        var result = new List<CanonicalProjectSourceDto>();
        foreach (var cpa in cpas)
        {
            var listing = listings.FirstOrDefault(l => l.MarketAssetId == cpa.MarketAssetId);
            if (listing is null) continue;

            var ai = aiData.FirstOrDefault(a => a.ExternalId == listing.ExternalId);

            result.Add(new CanonicalProjectSourceDto
            {
                MarketAssetId          = cpa.MarketAssetId,
                SourceName             = listing.Source.Name,
                ExternalId             = listing.ExternalId,
                Url                    = listing.Url,
                Title                  = listing.Title,
                MatchLevel             = cpa.MatchLevel,
                MatchScore             = cpa.MatchScore,
                MatchReason            = cpa.MatchReason,
                IsPrimary              = cpa.IsPrimary,
                LastSeenAt             = listing.LastSeenAt,
                LastCrawledAt          = listing.Source.LastCrawledAt,
                UnitCount              = unitCounts.GetValueOrDefault(cpa.MarketAssetId, 0),
                AskingPrice            = listing.AskingPrice,
                AiProjectName          = ai?.ExtractedProjectName,
                AiProjectNameConfidence = ai?.ProjectNameConfidence ?? 0,
            });
        }

        return result.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.SourceName).ToList();
    }

    public async Task<List<CanonicalUnitDto>> GetCanonicalProjectUnitsAsync(
        long canonicalProjectId, CancellationToken ct = default)
    {
        var assetIds = await _db.CanonicalProjectAssets
            .Where(a => a.CanonicalProjectId == canonicalProjectId)
            .Select(a => a.MarketAssetId)
            .ToListAsync(ct);

        if (assetIds.Count == 0) return [];

        var units = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && assetIds.Contains(a.ParentMarketAssetId.Value))
            .AsNoTracking()
            .ToListAsync(ct);

        var unitAssetIds = units.Select(u => u.Id).ToList();

        var unitListings = await _db.MarketListings
            .Where(l => unitAssetIds.Contains(l.MarketAssetId))
            .Include(l => l.Source)
            .AsNoTracking()
            .ToListAsync(ct);

        return units.Select(u =>
        {
            var myListings = unitListings.Where(l => l.MarketAssetId == u.Id).ToList();
            return new CanonicalUnitDto
            {
                AssetId       = u.Id,
                UnitExternalId = u.UnitExternalId,
                PropertyType  = u.PropertyType,
                PropertySubType = u.PropertySubType,
                Floor         = u.Floor,
                Bedrooms      = u.Bedrooms,
                Bathrooms     = u.Bathrooms,
                LivingArea    = u.LivingArea,
                TerraceArea   = u.TerraceArea,
                SaleStatus    = u.SaleStatus,
                AskingPrice   = myListings.OrderByDescending(l => l.LastSeenAt).FirstOrDefault()?.AskingPrice,
                Sources       = myListings.Select(l => new CanonicalUnitSourceDto
                {
                    SourceName = l.Source.Name,
                    ExternalId = l.ExternalId,
                    Url        = l.Url,
                    Price      = l.AskingPrice,
                    SaleStatus = u.SaleStatus,
                    LastSeenAt = l.LastSeenAt,
                }).ToList(),
            };
        }).OrderBy(u => u.Floor).ThenBy(u => u.UnitExternalId).ToList();
    }

    public async Task<CanonicalUnitDto?> GetCanonicalUnitAsync(long unitAssetId, CancellationToken ct = default)
    {
        var unit = await _db.MarketAssets
            .Where(a => a.Id == unitAssetId && a.ParentMarketAssetId.HasValue)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (unit is null) return null;

        var listings = await _db.MarketListings
            .Where(l => l.MarketAssetId == unitAssetId)
            .Include(l => l.Source)
            .AsNoTracking()
            .ToListAsync(ct);

        return new CanonicalUnitDto
        {
            AssetId       = unit.Id,
            UnitExternalId = unit.UnitExternalId,
            PropertyType  = unit.PropertyType,
            PropertySubType = unit.PropertySubType,
            Floor         = unit.Floor,
            Bedrooms      = unit.Bedrooms,
            Bathrooms     = unit.Bathrooms,
            LivingArea    = unit.LivingArea,
            TerraceArea   = unit.TerraceArea,
            SaleStatus    = unit.SaleStatus,
            AskingPrice   = listings.OrderByDescending(l => l.LastSeenAt).FirstOrDefault()?.AskingPrice,
            Sources       = listings.Select(l => new CanonicalUnitSourceDto
            {
                SourceName = l.Source.Name,
                ExternalId = l.ExternalId,
                Url        = l.Url,
                Price      = l.AskingPrice,
                SaleStatus = unit.SaleStatus,
                LastSeenAt = l.LastSeenAt,
            }).ToList(),
        };
    }

    public async Task<CanonicalProjectTimelineDto?> GetCanonicalProjectTimelineAsync(
        long canonicalProjectId, CancellationToken ct = default)
    {
        var cp = await _db.CanonicalProjects
            .Where(c => c.Id == canonicalProjectId && c.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (cp is null) return null;

        var assetIds = await _db.CanonicalProjectAssets
            .Where(a => a.CanonicalProjectId == canonicalProjectId)
            .Select(a => a.MarketAssetId)
            .ToListAsync(ct);

        var listings = await _db.MarketListings
            .Where(l => assetIds.Contains(l.MarketAssetId))
            .Include(l => l.Source)
            .AsNoTracking()
            .ToListAsync(ct);

        var listingIds = listings.Select(l => l.Id).ToList();

        var priceHistory = await _db.MarketListingPriceHistories
            .Where(ph => listingIds.Contains(ph.MarketListingId))
            .AsNoTracking()
            .ToListAsync(ct);

        var events = new List<CanonicalProjectTimelineEventDto>();

        // First-seen events
        foreach (var l in listings)
        {
            events.Add(new CanonicalProjectTimelineEventDto
            {
                Date       = l.FirstSeenAt,
                SourceName = l.Source.Name,
                ExternalId = l.ExternalId,
                EventType  = "FirstSeen",
                Price      = l.AskingPrice,
            });
        }

        // Price change events
        foreach (var ph in priceHistory)
        {
            var listing = listings.First(l => l.Id == ph.MarketListingId);
            events.Add(new CanonicalProjectTimelineEventDto
            {
                Date                  = ph.DetectedAt,
                SourceName            = listing.Source.Name,
                ExternalId            = listing.ExternalId,
                EventType             = "PriceChange",
                Price                 = ph.AskingPrice,
                PreviousPrice         = ph.PreviousPrice,
                PriceChangeAmount     = ph.PriceChangeAmount,
                PriceChangePercentage = ph.PriceChangePercentage,
            });
        }

        return new CanonicalProjectTimelineDto
        {
            CanonicalProjectId = canonicalProjectId,
            CanonicalName      = cp.CanonicalName,
            Events             = events.OrderByDescending(e => e.Date).ToList(),
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static (long Id, string Reason) SelectPrimaryAsset(
        List<ProjectGroupData> group,
        Dictionary<long, string> titleByAsset,
        Dictionary<long, int> unitCountByProject,
        Dictionary<long, int> aiScoreByAsset,
        Dictionary<long, int> photoCountByAsset)
    {
        if (group.Count == 1) return (group[0].Id, "SoloAsset");

        var scored = group.Select(p =>
        {
            int score = 0;
            var reasons = new List<string>();

            // Projectnaam aanwezig (niet technisch)
            var title = titleByAsset.GetValueOrDefault(p.Id, "");
            if (!IsTechnicalTitle(title)) { score += 4; reasons.Add("HasTitle"); }

            // AI-extractie kwaliteit
            var aiScore = aiScoreByAsset.GetValueOrDefault(p.Id, 0);
            if (aiScore > 0) { score += aiScore; reasons.Add($"AI={aiScore}"); }

            // Units bekend
            var units = unitCountByProject.GetValueOrDefault(p.Id, 0);
            if (units > 0) { score += Math.Min(units / 5 + 1, 5); reasons.Add($"Units={units}"); }

            // Foto's beschikbaar
            var photos = photoCountByAsset.GetValueOrDefault(p.Id, 0);
            if (photos > 0) { score += Math.Min(photos / 3 + 1, 4); reasons.Add($"Photos={photos}"); }

            // Ontwikkelaar bekend
            if (!string.IsNullOrWhiteSpace(p.DeveloperName)) { score += 2; reasons.Add("HasDeveloper"); }

            // Adres ingevuld
            if (!string.IsNullOrWhiteSpace(p.Street)) { score += 1; reasons.Add("HasStreet"); }

            // Coördinaten beschikbaar
            if (p.Latitude.HasValue && p.Longitude.HasValue) { score += 1; reasons.Add("HasCoords"); }

            // Recentheid (max 3 punten voor de meest recent geziene)
            var ageMonths = (int)((DateTime.UtcNow - p.LastSeenAt).TotalDays / 30);
            if (ageMonths <= 1) { score += 3; reasons.Add("VeryRecent"); }
            else if (ageMonths <= 3) { score += 2; reasons.Add("Recent"); }
            else if (ageMonths <= 6) { score += 1; reasons.Add("Moderate"); }

            return (p.Id, score, string.Join("+", reasons));
        }).ToList();

        var best = scored.OrderByDescending(x => x.score).First();
        return (best.Id, $"Score={best.score}[{best.Item3}]");
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
