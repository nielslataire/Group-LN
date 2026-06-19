using System.Text.Json;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Deduplication;

public class DeduplicationService : IDeduplicationService
{
    private readonly MarketDataDbContext _context;
    private readonly IProjectPhotoHashService _photoHashService;
    private readonly CrawlerSettings _settings;
    private readonly ILogger<DeduplicationService> _logger;

    public DeduplicationService(
        MarketDataDbContext context,
        IProjectPhotoHashService photoHashService,
        CrawlerSettings settings,
        ILogger<DeduplicationService> logger)
    {
        _context          = context;
        _photoHashService = photoHashService;
        _settings         = settings;
        _logger           = logger;
    }

    public async Task<DeduplicationSummary> RunAsync(DateTime since, CancellationToken ct = default)
    {
        _logger.LogInformation("[Deduplicatie] Start — assets gewijzigd na {Since:u}", since);

        // Projecten apart laden met child-units (nodig voor scoring en fingerprinting)
        var newProjects = await _context.MarketAssets
            .AsNoTracking()
            .Include(a => a.Listings.OrderByDescending(l => l.UpdatedAt).Take(1))
            .Include(a => a.ChildUnits.Where(u => u.IsActive))
                .ThenInclude(u => u.Listings.OrderByDescending(l => l.UpdatedAt).Take(1))
            .Where(a => a.UpdatedAt >= since && a.IsActive && a.IsProjectGroup)
            .ToListAsync(ct);

        var newUnits = await _context.MarketAssets
            .AsNoTracking()
            .Include(a => a.Listings.OrderByDescending(l => l.UpdatedAt).Take(1))
            .Where(a => a.UpdatedAt >= since && a.IsActive && !a.IsProjectGroup && a.ParentMarketAssetId.HasValue)
            .ToListAsync(ct);

        var totalScanned = newProjects.Count + newUnits.Count;

        if (totalScanned == 0)
        {
            _logger.LogInformation("[Deduplicatie] Geen nieuwe assets gevonden.");
            return new DeduplicationSummary(0, 0, 0, 0);
        }

        _logger.LogInformation("[Deduplicatie] {Total} assets: {Projects} projecten, {Units} units.",
            totalScanned, newProjects.Count, newUnits.Count);

        var matchDebugEntries = new List<object>();
        var canonicalGroups   = new List<CanonicalGroup>();

        var projectCandidates = await MatchProjectsAsync(newProjects, canonicalGroups, matchDebugEntries, ct);
        var unitCandidates    = await MatchUnitsAsync(newUnits, matchDebugEntries, ct);

        WriteMatchDebugFile(matchDebugEntries);
        WriteCanonicalDebugFile(canonicalGroups);

        var saved = projectCandidates + unitCandidates;
        _logger.LogInformation(
            "[Deduplicatie] Klaar. {Projects} project-candidates, {Units} unit-candidates, {Saved} opgeslagen.",
            projectCandidates, unitCandidates, saved);

        // PhotoHash DB summary
        if (_settings.PhotoHashing.EnableProjectPhotoHashing)
        {
            var projectsWithHashes = await _context.ProjectPhotoHashes
                .AsNoTracking()
                .Select(h => h.MarketAssetId)
                .Distinct()
                .CountAsync(ct);
            var totalPhotoHashes = await _context.ProjectPhotoHashes
                .AsNoTracking()
                .CountAsync(ct);
            var distinctContentHashes = await _context.ProjectPhotoHashes
                .AsNoTracking()
                .Select(h => h.ContentHash)
                .Distinct()
                .CountAsync(ct);
            var distinctPerceptualHashes = await _context.ProjectPhotoHashes
                .AsNoTracking()
                .Where(h => h.PerceptualHash.HasValue)
                .Select(h => h.PerceptualHash!.Value)
                .Distinct()
                .CountAsync(ct);

            _logger.LogInformation(
                "[PhotoHash] DbSummary | ProjectsWithHashes={P} | TotalPhotoHashes={T} | " +
                "DistinctContentHashes={C} | DistinctPerceptualHashes={PH}",
                projectsWithHashes, totalPhotoHashes, distinctContentHashes, distinctPerceptualHashes);
        }

        return new DeduplicationSummary(totalScanned, projectCandidates, unitCandidates, saved);
    }

    // ── Project matching ──────────────────────────────────────────────────────

    private async Task<int> MatchProjectsAsync(
        List<MarketAsset> newProjects,
        List<CanonicalGroup> canonicalGroups,
        List<object> debugEntries,
        CancellationToken ct)
    {
        if (newProjects.Count == 0) return 0;

        var postalCodes = newProjects
            .Select(a => a.PostalCode).Where(p => p != null).Distinct().ToList();

        var existingProjects = await _context.MarketAssets
            .AsNoTracking()
            .Include(a => a.Listings.OrderByDescending(l => l.UpdatedAt).Take(1))
            .Include(a => a.ChildUnits.Where(u => u.IsActive))
                .ThenInclude(u => u.Listings.OrderByDescending(l => l.UpdatedAt).Take(1))
            .Where(a => a.IsActive && a.IsProjectGroup && postalCodes.Contains(a.PostalCode))
            .ToListAsync(ct);

        // Fingerprint- en norm-cache voor alle betrokken projecten
        var existingIds = existingProjects.Select(e => e.Id).ToHashSet();
        var allForCache = existingProjects
            .Concat(newProjects.Where(p => !existingIds.Contains(p.Id)))
            .ToList();

        var fpCache   = BuildProjectFingerprintCache(allForCache);
        var normCache = allForCache.ToDictionary(
            p => p.Id,
            p => ProjectNameNormalizer.Normalize(p.Listings.FirstOrDefault()?.Title));

        // Foto-hash cache (ContentHash + PerceptualHash per project-ID)
        var allProjectIds     = allForCache.Select(p => p.Id).ToHashSet();
        var photoContentCache    = new Dictionary<long, List<string>>();
        var photoPerceptualCache = new Dictionary<long, List<long>>();
        if (_settings.PhotoHashing.EnableProjectPhotoHashing)
        {
            var photoHashes = await _context.ProjectPhotoHashes
                .Where(h => allProjectIds.Contains(h.MarketAssetId))
                .Select(h => new { h.MarketAssetId, h.ContentHash, h.PerceptualHash })
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var row in photoHashes)
            {
                if (!photoContentCache.TryGetValue(row.MarketAssetId, out var cList))
                    photoContentCache[row.MarketAssetId] = cList = [];
                cList.Add(row.ContentHash);

                if (row.PerceptualHash.HasValue)
                {
                    if (!photoPerceptualCache.TryGetValue(row.MarketAssetId, out var pList))
                        photoPerceptualCache[row.MarketAssetId] = pList = [];
                    pList.Add(row.PerceptualHash.Value);
                }
            }
        }

        int saved = 0;
        var exactPairs = new List<(MarketAsset A, MarketAsset B, double Score)>();

        foreach (var newProject in newProjects)
        {
            normCache.TryGetValue(newProject.Id, out var normNew);
            normNew ??= string.Empty;
            fpCache.TryGetValue(newProject.Id, out var fpNew);
            fpNew ??= [];
            photoContentCache.TryGetValue(newProject.Id, out var photoContentNew);
            photoContentNew ??= [];
            photoPerceptualCache.TryGetValue(newProject.Id, out var photoPerceptualNew);
            photoPerceptualNew ??= [];

            // Scoor alle candidates voor dit project
            var rawCandidates = new List<(MarketAsset Existing, double Score, string Level, object Fields)>();

            foreach (var existing in existingProjects)
            {
                if (existing.Id == newProject.Id) continue;

                normCache.TryGetValue(existing.Id, out var normExisting);
                normExisting ??= string.Empty;
                fpCache.TryGetValue(existing.Id, out var fpExisting);
                fpExisting ??= [];
                photoContentCache.TryGetValue(existing.Id, out var photoContentExisting);
                photoContentExisting ??= [];
                photoPerceptualCache.TryGetValue(existing.Id, out var photoPerceptualExisting);
                photoPerceptualExisting ??= [];

                var (score, level, fields) = ScoreProjectPair(
                    normNew, fpNew, photoContentNew, photoPerceptualNew, newProject,
                    normExisting, fpExisting, photoContentExisting, photoPerceptualExisting, existing,
                    _settings.PhotoHashing, _logger);
                if (level == "None") continue;

                rawCandidates.Add((existing, score, level, fields));
            }

            // Max 10 beste candidates per project
            var topCandidates = rawCandidates
                .OrderByDescending(c => c.Score)
                .Take(10)
                .ToList();

            foreach (var (existing, score, level, fields) in topCandidates)
            {
                var (lowId, highId) = Canonical(existing.Id, newProject.Id);
                if (lowId == highId) continue; // self-match verbod
                if (await CandidateExistsAsync(lowId, highId, ct)) continue;

                var candidate = new MarketAssetMatchCandidate
                {
                    ExistingMarketAssetId  = lowId,
                    CandidateMarketAssetId = highId,
                    SourceId               = 0,
                    ExternalId             = newProject.ProjectExternalId ?? newProject.Id.ToString(),
                    Url                    = newProject.Listings.FirstOrDefault()?.Url ?? string.Empty,
                    MatchScore             = (decimal)Math.Round(score, 4),
                    MatchReason            = BuildProjectReason(fields),
                    MatchType              = "Project",
                    MatchLevel             = level,
                    ComparedFieldsJson     = JsonSerializer.Serialize(fields),
                    CreatedAt              = DateTime.UtcNow
                };

                _context.MarketAssetMatchCandidates.Add(candidate);
                await _context.SaveChangesAsync(ct);
                saved++;

                _logger.LogInformation(
                    "[ProjectMatchCandidate] Existing={Existing} Candidate={Candidate} Score={Score:F3} Level={Level} Reason={Reason}",
                    lowId, highId, score, level, candidate.MatchReason);

                if (level == "Exact" || (level == "Probable" && score >= 0.80))
                    exactPairs.Add((newProject, existing, score));

                debugEntries.Add(new
                {
                    matchType        = "Project",
                    matchLevel       = level,
                    matchScore       = Math.Round(score, 4),
                    existingAssetId  = lowId,
                    candidateAssetId = highId,
                    comparedFields   = fields
                });
            }
        }

        // Canonical groepen bepalen uit Exact-pairs
        ProcessCanonicalGroups(exactPairs, canonicalGroups);

        return saved;
    }

    /// <summary>
    /// Scoor een paar projecten. Geeft score [0,1], level ("Exact"/"Probable"/"Possible"/"None")
    /// en debug-velden terug. Level wordt bepaald via expliciete combinatie-regels (niet puur score).
    /// </summary>
    private static (double Score, string Level, object Fields) ScoreProjectPair(
        string normA, IReadOnlyList<string> fpA,
        List<string> photoContentA, List<long> photoPerceptualA, MarketAsset a,
        string normB, IReadOnlyList<string> fpB,
        List<string> photoContentB, List<long> photoPerceptualB, MarketAsset b,
        PhotoHashingSettings photoSettings, ILogger? logger = null)
    {
        var nameSim = ProjectNameNormalizer.Similarity(normA, normB);

        var samePostal  = string.Equals(a.PostalCode, b.PostalCode, StringComparison.Ordinal);
        var sameStreet  = !string.IsNullOrEmpty(a.Street)
                       && string.Equals(NormalizeStr(a.Street), NormalizeStr(b.Street), StringComparison.OrdinalIgnoreCase);
        var sameHouseNr = !string.IsNullOrEmpty(a.HouseNumber)
                       && string.Equals(a.HouseNumber?.Trim(), b.HouseNumber?.Trim(), StringComparison.OrdinalIgnoreCase);
        var sameDev     = !string.IsNullOrEmpty(a.DeveloperName)
                       && string.Equals(NormalizeStr(a.DeveloperName), NormalizeStr(b.DeveloperName), StringComparison.OrdinalIgnoreCase);

        var distM = GeoDistance(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

        var countA = a.ChildUnits?.Count ?? 0;
        var countB = b.ChildUnits?.Count ?? 0;
        var unitCountSimilar = countA > 0 && countB > 0
            && (double)Math.Min(countA, countB) / Math.Max(countA, countB) >= 0.75;

        var overlap = fpA.Count > 0 && fpB.Count > 0
            ? UnitFingerprintBuilder.OverlapRatio(fpA, fpB)
            : 0.0;

        // Foto-hash overlap (ContentHash = exacte overeenkomst)
        var contentSetB     = photoContentB.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var photoContentMatches = photoContentA.Count(h => contentSetB.Contains(h));

        // Perceptuele hash overlap (dHash Hamming-afstand ≤ maxDistance)
        var photoPerceptualMatches = 0;
        var minPerceptualDistance  = int.MaxValue;
        if (photoPerceptualA.Count > 0 && photoPerceptualB.Count > 0)
        {
            foreach (var ha in photoPerceptualA)
            {
                foreach (var hb in photoPerceptualB)
                {
                    var dist = System.Numerics.BitOperations.PopCount((ulong)(ha ^ hb));
                    if (dist < minPerceptualDistance) minPerceptualDistance = dist;
                    if (dist <= photoSettings.PhotoPerceptualHashMaxDistance)
                        photoPerceptualMatches++;
                }
            }
        }

        // ── Combinatie-regels voor Exact (minstens één moet kloppen) ──────────
        // A: naam bijna identiek + zelfde postcode + zelfde straat of < 50m
        var comboA = normA.Length > 0 && normB.Length > 0
                  && nameSim >= 0.95
                  && samePostal
                  && (sameStreet || (distM.HasValue && distM.Value <= 50.0));

        // B: exact zelfde adres + developer of unit-overlap > 70%
        var comboB = sameStreet && sameHouseNr && samePostal
                  && (sameDev || overlap > 0.70);

        // C: unit fingerprint overlap > 85% + zelfde postcode
        var comboC = overlap > 0.85 && samePostal;

        // D: 2+ exacte foto-matches + zelfde postcode/gemeente + adres of coördinaten
        var comboD = photoSettings.EnableProjectPhotoHashing
                  && photoContentMatches >= 2
                  && samePostal
                  && (sameStreet || (distM.HasValue && distM.Value <= 100.0));

        // ── Probable ──────────────────────────────────────────────────────────
        // P1: naam sterk gelijkend + zelfde postcode + < 150m
        var probCond1 = nameSim >= 0.80 && samePostal
                     && distM.HasValue && distM.Value <= 150.0;
        // P2: exact zelfde adres + gelijkaardig aantal units + overlap > 40%
        var probCond2 = sameStreet && sameHouseNr && unitCountSimilar && overlap > 0.40;
        // P3: 2+ content foto-matches + zelfde postcode
        var probCond3 = photoSettings.EnableProjectPhotoHashing
                     && photoContentMatches >= 2 && samePostal;
        // P4: 2+ perceptuele foto-matches + zelfde postcode
        var probCondPerceptual = photoSettings.EnableProjectPhotoHashing
                              && photoPerceptualMatches >= 2 && samePostal;

        // ── Gewogen score (voor ranking en Possible-drempel) ──────────────────
        var nameScore  = nameSim * 0.25;
        var addrScore  = (sameStreet ? 0.15 : 0.0) + (sameHouseNr ? 0.10 : 0.0);
        var coordScore = distM switch { <= 50 => 0.15, <= 100 => 0.10, <= 200 => 0.05, _ => 0.0 };
        var devScore   = sameDev ? 0.10 : 0.0;
        var unitScore  = unitCountSimilar ? 0.07 : 0.0;
        var fpScore    = overlap switch { > 0.70 => 0.10, > 0.50 => 0.07, > 0.30 => 0.03, _ => 0.0 };
        var photoScore = photoContentMatches switch { >= 3 => 0.12, >= 2 => 0.08, 1 => 0.03, _ => 0.0 };
        var perceptualScore = photoPerceptualMatches switch { >= 3 => 0.08, >= 2 => 0.05, 1 => 0.02, _ => 0.0 };
        var score = nameScore + addrScore + coordScore + devScore + unitScore + fpScore + photoScore + perceptualScore;

        // ── Level bepalen ─────────────────────────────────────────────────────
        string level;
        if      (comboA || comboB || comboC || comboD)                        level = "Exact";
        else if (probCond1 || probCond2 || probCond3 || probCondPerceptual)   level = "Probable";
        else if (score >= 0.60 && samePostal)                                 level = "Possible";
        else                                                                   return (score, "None", new { });

        // Fases (Green Yard I vs II) nooit Exact
        if (level == "Exact" && ProjectNameNormalizer.IsPhaseVariant(normA, normB))
            level = "Possible";

        // ── PhotoMatch log (enkel wanneer foto-informatie bijdraagt) ──────────
        if (photoSettings.EnableProjectPhotoHashing
            && (photoContentMatches > 0 || photoPerceptualMatches > 0))
        {
            logger?.LogInformation(
                "[PhotoMatch] ProjectA={A} ProjectB={B} | ContentHashMatches={CM} | PerceptualHashMatches={PM} | " +
                "MinPerceptualDistance={MPD} | SamePostalCode={SP} | SameStreet={SS} | DistanceMeters={Dist} | " +
                "PhotoMatchScore={PhotoScore:F3} | FinalMatchLevel={Level}",
                a.Id, b.Id,
                photoContentMatches, photoPerceptualMatches,
                minPerceptualDistance == int.MaxValue ? (int?)null : minPerceptualDistance,
                samePostal, sameStreet,
                distM.HasValue ? (int?)Math.Round(distM.Value) : null,
                Math.Round(photoScore + perceptualScore, 3),
                level);
        }

        var fields = (object)new
        {
            normTitleA               = normA,
            normTitleB               = normB,
            nameSimilarity           = Math.Round(nameSim, 3),
            samePostalCode           = samePostal,
            sameStreet,
            sameHouseNr,
            sameDeveloper            = sameDev,
            distanceMeters           = distM.HasValue ? (int?)Math.Round(distM.Value) : null,
            unitCountA               = countA,
            unitCountB               = countB,
            unitCountSimilar,
            fingerprintOverlap       = Math.Round(overlap, 3),
            photoContentMatches,
            photoPerceptualMatches,
            minPerceptualDistance    = minPerceptualDistance == int.MaxValue ? (int?)null : minPerceptualDistance,
            isPhaseVariant           = ProjectNameNormalizer.IsPhaseVariant(normA, normB),
            comboA, comboB, comboC, comboD,
            probCond1, probCond2, probCond3, probCondPerceptual,
            scores = new
            {
                name       = Math.Round(nameScore,       3),
                addr       = Math.Round(addrScore,       3),
                coord      = Math.Round(coordScore,      3),
                dev        = Math.Round(devScore,        3),
                units      = Math.Round(unitScore,       3),
                fp         = Math.Round(fpScore,         3),
                photo      = Math.Round(photoScore,      3),
                perceptual = Math.Round(perceptualScore, 3),
                total      = Math.Round(score,           3)
            }
        };

        return (score, level, fields);
    }

    // ── Canonical ──────────────────────────────────────────────────────────────

    private void ProcessCanonicalGroups(
        List<(MarketAsset A, MarketAsset B, double Score)> exactPairs,
        List<CanonicalGroup> groups)
    {
        foreach (var (a, b, score) in exactPairs)
        {
            var (canonical, duplicate, reason) = DetermineCanonical(a, b);

            _logger.LogInformation(
                "[CanonicalSuggested] Canonical={CanonicalId} Duplicate={DuplicateId} Reason={Reason}",
                canonical.Id, duplicate.Id, reason);

            // Zoek bestaande groep die één van deze IDs al bevat
            var existing = groups.FirstOrDefault(g =>
                g.CanonicalId == canonical.Id  || g.CanonicalId == duplicate.Id  ||
                g.Duplicates.Any(d => d.AssetId == canonical.Id || d.AssetId == duplicate.Id));

            if (existing is not null)
            {
                if (!existing.Duplicates.Any(d => d.AssetId == duplicate.Id))
                    existing.Duplicates.Add(new DuplicateEntry(
                        duplicate.Id,
                        duplicate.Listings.FirstOrDefault()?.Title,
                        Math.Round(score, 4)));
            }
            else
            {
                groups.Add(new CanonicalGroup(
                    canonical.Id,
                    canonical.Listings.FirstOrDefault()?.Title,
                    [new DuplicateEntry(
                        duplicate.Id,
                        duplicate.Listings.FirstOrDefault()?.Title,
                        Math.Round(score, 4))]));
            }
        }
    }

    private static (MarketAsset Canonical, MarketAsset Duplicate, string Reason) DetermineCanonical(
        MarketAsset a, MarketAsset b)
    {
        // Regel 1: meeste child-units
        var ua = a.ChildUnits?.Count ?? 0;
        var ub = b.ChildUnits?.Count ?? 0;
        if (ua != ub) return ua > ub ? (a, b, "meer units") : (b, a, "meer units");

        // Regel 2: meeste units met prijs
        var pa = a.ChildUnits?.Count(u => u.Listings.Any(l => l.AskingPrice.HasValue)) ?? 0;
        var pb = b.ChildUnits?.Count(u => u.Listings.Any(l => l.AskingPrice.HasValue)) ?? 0;
        if (pa != pb) return pa > pb ? (a, b, "meer prijzen") : (b, a, "meer prijzen");

        // Regel 3: betere titel (geen marketing-suffix "biedt...")
        var ta = a.Listings.FirstOrDefault()?.Title ?? string.Empty;
        var tb = b.Listings.FirstOrDefault()?.Title ?? string.Empty;
        var taClean = !ta.Contains("biedt", StringComparison.OrdinalIgnoreCase);
        var tbClean = !tb.Contains("biedt", StringComparison.OrdinalIgnoreCase);
        if (taClean != tbClean) return taClean ? (a, b, "betere titel") : (b, a, "betere titel");

        // Regel 4: kortste titel (minder marketing-tekst)
        if (ta.Length != tb.Length) return ta.Length < tb.Length ? (a, b, "kortere titel") : (b, a, "kortere titel");

        // Regel 5: oudste FirstSeenAt
        return a.FirstSeenAt <= b.FirstSeenAt ? (a, b, "oudste") : (b, a, "oudste");
    }

    // ── Unit matching ─────────────────────────────────────────────────────────

    private async Task<int> MatchUnitsAsync(
        List<MarketAsset> newUnits,
        List<object> debugEntries,
        CancellationToken ct)
    {
        if (newUnits.Count == 0) return 0;

        var postalCodes   = newUnits.Select(a => a.PostalCode).Where(p => p != null).Distinct().ToList();
        var propertyTypes = newUnits.Select(a => a.PropertyType).Distinct().ToList();

        var existingUnits = await _context.MarketAssets
            .AsNoTracking()
            .Include(a => a.Listings.OrderByDescending(l => l.UpdatedAt).Take(1))
            .Where(a => a.IsActive && !a.IsProjectGroup && a.ParentMarketAssetId.HasValue
                     && postalCodes.Contains(a.PostalCode)
                     && propertyTypes.Contains(a.PropertyType))
            .ToListAsync(ct);

        int saved = 0;

        foreach (var newUnit in newUnits)
        {
            var newPrice  = newUnit.Listings.FirstOrDefault()?.AskingPrice;
            var hasPrice  = newPrice.HasValue;
            var hasUnitId = !string.IsNullOrEmpty(newUnit.UnitExternalId);

            foreach (var existing in existingUnits)
            {
                if (existing.Id == newUnit.Id) continue;

                var (score, fields, capped) = ScoreUnitPair(newUnit, newPrice, existing);

                var isSameParent = existing.ParentMarketAssetId.HasValue
                                && existing.ParentMarketAssetId == newUnit.ParentMarketAssetId;
                var noDataUnit   = !hasPrice && !hasUnitId;

                // Drempel per situatie
                if (isSameParent && score < 0.90)  continue;
                if (noDataUnit   && score < 0.80)  continue;
                if (score < 0.60)                   continue;

                // Cap bij ontbrekende data of specifieke condities
                var level = (capped || noDataUnit) ? "Possible" : GetMatchLevel(score);

                var (lowId, highId) = Canonical(existing.Id, newUnit.Id);
                if (await CandidateExistsAsync(lowId, highId, ct)) continue;

                var candidate = new MarketAssetMatchCandidate
                {
                    ExistingMarketAssetId  = lowId,
                    CandidateMarketAssetId = highId,
                    SourceId               = 0,
                    ExternalId             = newUnit.UnitExternalId ?? newUnit.Id.ToString(),
                    Url                    = newUnit.Listings.FirstOrDefault()?.Url ?? string.Empty,
                    MatchScore             = (decimal)Math.Round(score, 4),
                    MatchReason            = BuildUnitReason(fields),
                    MatchType              = "Unit",
                    MatchLevel             = level,
                    ComparedFieldsJson     = JsonSerializer.Serialize(fields),
                    CreatedAt              = DateTime.UtcNow
                };

                _context.MarketAssetMatchCandidates.Add(candidate);
                await _context.SaveChangesAsync(ct);
                saved++;

                _logger.LogInformation(
                    "[UnitMatchCandidate] Existing={Existing} Candidate={Candidate} Score={Score:F3} Level={Level}",
                    lowId, highId, score, level);

                debugEntries.Add(new
                {
                    matchType        = "Unit",
                    matchLevel       = level,
                    matchScore       = Math.Round(score, 4),
                    existingAssetId  = lowId,
                    candidateAssetId = highId,
                    comparedFields   = fields
                });
            }
        }

        return saved;
    }

    private static (double Score, object Fields, bool Capped) ScoreUnitPair(
        MarketAsset a, decimal? priceA, MarketAsset b)
    {
        var priceB = b.Listings.FirstOrDefault()?.AskingPrice;

        var sameParent  = a.ParentMarketAssetId.HasValue && a.ParentMarketAssetId == b.ParentMarketAssetId;
        var samePostal  = string.Equals(a.PostalCode, b.PostalCode, StringComparison.Ordinal);
        var sameType    = a.PropertyType    == b.PropertyType;
        var sameSubType = a.PropertySubType == b.PropertySubType;

        double areaScore;
        if (a.LivingArea.HasValue && b.LivingArea.HasValue)
        {
            var diff = Math.Abs((double)(a.LivingArea.Value - b.LivingArea.Value));
            areaScore = diff <= 2 ? 0.20 : diff <= 5 ? 0.10 : diff <= 10 ? 0.05 : 0.0;
        }
        else areaScore = 0.0;

        double priceScore;
        if (priceA.HasValue && priceB.HasValue && priceA.Value > 0)
        {
            var pct = Math.Abs((double)((priceA.Value - priceB.Value) / priceA.Value));
            priceScore = pct <= 0.01 ? 0.15 : pct <= 0.05 ? 0.10 : pct <= 0.10 ? 0.05 : 0.0;
        }
        else priceScore = 0.0;

        var sameBeds  = a.Bedrooms.HasValue && a.Bedrooms == b.Bedrooms;
        var sameFloor = a.Floor.HasValue    && a.Floor    == b.Floor;

        var parentScore = sameParent ? 0.30 : 0.0;
        var postalScore = samePostal ? 0.10 : 0.0;
        var typeScore   = (sameType ? 0.10 : 0.0) + (sameSubType ? 0.05 : 0.0);
        var bedScore    = sameBeds  ? 0.05 : 0.0;
        var floorScore  = sameFloor ? 0.05 : 0.0;
        var total       = parentScore + postalScore + typeScore + areaScore + priceScore + bedScore + floorScore;

        // Zelfde project, type, opp, kamers maar ontbrekende verdieping/prijs → cap Possible
        var capped = sameParent && sameType && areaScore >= 0.20 && sameBeds && !sameFloor && priceScore < 0.15;

        var fields = (object)new
        {
            sameParentProject = sameParent,
            samePostalCode    = samePostal,
            samePropertyType  = sameType,
            sameSubType,
            livingAreaA       = a.LivingArea,
            livingAreaB       = b.LivingArea,
            priceA,
            priceB,
            sameBedrooms      = sameBeds,
            sameFloor,
            cappedToPossible  = capped,
            scores = new
            {
                parent = parentScore,
                postal = postalScore,
                type   = typeScore,
                area   = areaScore,
                price  = priceScore,
                beds   = bedScore,
                floor  = floorScore,
                total  = Math.Round(total, 3)
            }
        };

        return (total, fields, capped);
    }

    // ── Fingerprint cache ─────────────────────────────────────────────────────

    private static Dictionary<long, List<string>> BuildProjectFingerprintCache(List<MarketAsset> projects)
    {
        var cache = new Dictionary<long, List<string>>();
        foreach (var project in projects)
        {
            var normName = ProjectNameNormalizer.Normalize(project.Listings.FirstOrDefault()?.Title);
            cache[project.Id] = project.ChildUnits?
                .Select(u => UnitFingerprintBuilder.Build(u, normName, u.Listings.FirstOrDefault()?.AskingPrice))
                .ToList() ?? [];
        }
        return cache;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (long Low, long High) Canonical(long a, long b) => a < b ? (a, b) : (b, a);

    private async Task<bool> CandidateExistsAsync(long lowId, long highId, CancellationToken ct) =>
        await _context.MarketAssetMatchCandidates
            .AnyAsync(c => c.ExistingMarketAssetId == lowId
                        && c.CandidateMarketAssetId  == highId, ct);

    private static string GetMatchLevel(double score) =>
        score >= 0.95 ? "Exact" : score >= 0.80 ? "Probable" : "Possible";

    private static double? GeoDistance(decimal? lat1, decimal? lon1, decimal? lat2, decimal? lon2)
    {
        if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue) return null;
        const double R = 6_371_000;
        var dLat = (double)(lat2 - lat1) * Math.PI / 180;
        var dLon = (double)(lon2 - lon1) * Math.PI / 180;
        var la1  = (double)lat1.Value * Math.PI / 180;
        var la2  = (double)lat2.Value * Math.PI / 180;
        var sl   = Math.Sin(dLat / 2);
        var slo  = Math.Sin(dLon / 2);
        var a    = sl * sl + Math.Cos(la1) * Math.Cos(la2) * slo * slo;
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static string NormalizeStr(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    private static string BuildProjectReason(object fields)
    {
        try
        {
            var doc   = JsonDocument.Parse(JsonSerializer.Serialize(fields)).RootElement;
            var parts = new List<string>();
            if (doc.GetProperty("comboA").GetBoolean())            parts.Add("ComboA(naam+postcode+locatie)");
            if (doc.GetProperty("comboB").GetBoolean())            parts.Add("ComboB(adres+dev/overlap)");
            if (doc.GetProperty("comboC").GetBoolean())            parts.Add("ComboC(fingerprint>85%)");
            if (doc.GetProperty("comboD").GetBoolean())            parts.Add("ComboD(foto+postcode+locatie)");
            if (doc.GetProperty("probCond1").GetBoolean())         parts.Add("Probable(naam+postcode+<150m)");
            if (doc.GetProperty("probCond2").GetBoolean())         parts.Add("Probable(adres+units+overlap>40%)");
            if (doc.GetProperty("probCond3").GetBoolean())         parts.Add("Probable(foto+postcode)");
            if (doc.GetProperty("probCondPerceptual").GetBoolean()) parts.Add("Probable(perceptueel+postcode)");
            if (doc.GetProperty("isPhaseVariant").GetBoolean())    parts.Add("fase-variant");
            if (parts.Count == 0)
            {
                var sim = doc.GetProperty("nameSimilarity").GetDouble();
                parts.Add($"naam {sim:P0}");
            }
            return string.Join(", ", parts);
        }
        catch { return string.Empty; }
    }

    private static string BuildUnitReason(object fields)
    {
        try
        {
            var doc   = JsonDocument.Parse(JsonSerializer.Serialize(fields)).RootElement;
            var parts = new List<string>();
            if (doc.GetProperty("sameParentProject").GetBoolean()) parts.Add("zelfde project");
            if (doc.GetProperty("samePropertyType").GetBoolean())  parts.Add("zelfde type");
            var aA = doc.GetProperty("livingAreaA");
            var aB = doc.GetProperty("livingAreaB");
            if (aA.ValueKind != JsonValueKind.Null && aB.ValueKind != JsonValueKind.Null)
            {
                var diff = Math.Abs(aA.GetDecimal() - aB.GetDecimal());
                if (diff <= 2) parts.Add($"opp ~{aA.GetDecimal():N0}m²");
            }
            if (doc.GetProperty("sameBedrooms").GetBoolean()) parts.Add("zelfde slaapkamers");
            if (doc.GetProperty("sameFloor").GetBoolean())    parts.Add("zelfde verdieping");
            return string.Join(", ", parts);
        }
        catch { return string.Empty; }
    }

    // ── Debug bestanden ───────────────────────────────────────────────────────

    private static void WriteMatchDebugFile(List<object> entries)
    {
        if (entries.Count == 0) return;
        try
        {
            var dir   = Path.Combine(AppContext.BaseDirectory, "debug", "matching");
            Directory.CreateDirectory(dir);
            var path  = Path.Combine(dir, $"match-candidates-{DateTime.UtcNow:yyyyMMdd-HHmm}.json");
            foreach (var e in entries)
                File.AppendAllText(path, JsonSerializer.Serialize(e) + "\n");
        }
        catch { }
    }

    private static void WriteCanonicalDebugFile(List<CanonicalGroup> groups)
    {
        if (groups.Count == 0) return;
        try
        {
            var dir  = Path.Combine(AppContext.BaseDirectory, "debug", "matching");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"project-duplicate-groups-{DateTime.UtcNow:yyyyMMdd-HHmm}.json");
            foreach (var g in groups)
            {
                var entry = new
                {
                    canonical  = new { id = g.CanonicalId,  title = g.CanonicalTitle },
                    duplicates = g.Duplicates.Select(d => new { id = d.AssetId, title = d.Title, score = d.Score })
                };
                File.AppendAllText(path, JsonSerializer.Serialize(entry) + "\n");
            }
        }
        catch { }
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private sealed class CanonicalGroup(long canonicalId, string? canonicalTitle, List<DuplicateEntry> duplicates)
    {
        public long CanonicalId       { get; } = canonicalId;
        public string? CanonicalTitle { get; } = canonicalTitle;
        public List<DuplicateEntry> Duplicates { get; } = duplicates;
    }

    private sealed record DuplicateEntry(long AssetId, string? Title, double Score);
}
