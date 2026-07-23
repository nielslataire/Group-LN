using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

/// <summary>
/// Groepeert source-units (MarketAsset) binnen een CanonicalProject tot CanonicalUnits.
/// Gebruikt Zimmo als anchor-set wanneer UnitNumber of UnitExternalId beschikbaar is.
/// </summary>
public class CanonicalUnitService : ICanonicalUnitService
{
    private readonly MarketDataDbContext _db;
    private readonly ILogger<CanonicalUnitService> _logger;

    private static readonly string[] SourcePriority = ["Immoweb", "Zimmo", "Immoscoop", "Immovlan", "Realo"];

    // Thresholds
    private const int ThresholdExact    = 90;
    private const int ThresholdProbable = 70;
    private const int ThresholdPossible = 55;
    private const int AmbiguityGap      = 5;   // als #2 ≥ #1 - AmbiguityGap → ambiguous

    public CanonicalUnitService(MarketDataDbContext db, ILogger<CanonicalUnitService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Publieke interface ────────────────────────────────────────────────────

    public async Task<CanonicalUnitRebuildResult> RebuildCanonicalUnitsAsync(
        long? canonicalProjectId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new CanonicalUnitRebuildResult();

        var projectIds = canonicalProjectId.HasValue
            ? [canonicalProjectId.Value]
            : await _db.CanonicalProjects
                .Where(p => p.IsActive)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

        _logger.LogInformation("[CanonicalUnits] Start | {Count} projecten", projectIds.Count);

        foreach (var pid in projectIds)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var pr = await RebuildProjectAsync(pid, cancellationToken);
                result.ProjectsProcessed     += 1;
                result.SourceUnitsScanned    += pr.SourceUnitsScanned;
                result.CanonicalUnitsCreated += pr.CanonicalUnitsCreated;
                result.UnitsMerged           += pr.UnitsMerged;
                result.ConflictsDetected     += pr.ConflictsDetected;

                _logger.LogInformation(
                    "[CanonicalUnits] Project={Id} | SourceUnits={S} | CanonicalUnits={C} | Merged={M} | Conflicts={Conf}",
                    pid, pr.SourceUnitsScanned, pr.CanonicalUnitsCreated, pr.UnitsMerged, pr.ConflictsDetected);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CanonicalUnits] Fout bij project {Id}. Overgeslagen.", pid);
            }
        }

        _logger.LogInformation(
            "[CanonicalUnits] Done | Projects={P} | SourceUnits={S} | CanonicalUnits={C} | Merged={M} | Conflicts={Conf}",
            result.ProjectsProcessed, result.SourceUnitsScanned,
            result.CanonicalUnitsCreated, result.UnitsMerged, result.ConflictsDetected);

        return result;
    }

    // ── Per-project rebuild ───────────────────────────────────────────────────

    private async Task<CanonicalUnitRebuildResult> RebuildProjectAsync(
        long projectId, CancellationToken ct)
    {
        // 1. Verwijder bestaande CanonicalUnits
        var existing = await _db.CanonicalUnits
            .Where(u => u.CanonicalProjectId == projectId)
            .ToListAsync(ct);
        _db.CanonicalUnits.RemoveRange(existing);
        await _db.SaveChangesAsync(ct);

        // 2. Laad gekoppelde project-asset IDs
        var linkedIds = await _db.CanonicalProjectAssets
            .Where(l => l.CanonicalProjectId == projectId)
            .Select(l => l.MarketAssetId)
            .ToListAsync(ct);

        if (linkedIds.Count == 0)
            return new CanonicalUnitRebuildResult();

        // 3. Laad source units
        var sourceUnits = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && linkedIds.Contains(a.ParentMarketAssetId.Value))
            .AsNoTracking()
            .ToListAsync(ct);

        if (sourceUnits.Count == 0)
            return new CanonicalUnitRebuildResult();

        // 4. Laad listings (bron + prijs)
        var unitIds = sourceUnits.Select(u => u.Id).ToList();
        var listings = await _db.MarketListings
            .Where(l => unitIds.Contains(l.MarketAssetId) && l.IsActive)
            .Select(l => new { l.MarketAssetId, SourceName = l.Source.Name, l.ExternalId, l.Url, l.AskingPrice })
            .AsNoTracking()
            .ToListAsync(ct);

        var listingByUnit = listings
            .GroupBy(l => l.MarketAssetId)
            .ToDictionary(g => g.Key, g => g.First());

        // 5. Verrijkt model
        var enriched = sourceUnits.Select(u =>
        {
            listingByUnit.TryGetValue(u.Id, out var lst);
            return new EnrichedUnit(
                Asset      : u,
                SourceName : lst?.SourceName ?? "",
                ExternalId : lst?.ExternalId ?? u.UnitExternalId ?? "",
                Url        : lst?.Url,
                Price      : lst?.AskingPrice ?? 0m);
        }).ToList();

        // 6. Matching
        var (canonicalGroups, debugReport) = BuildCanonicalGroups(projectId, enriched);

        // 7. Debug JSON wegschrijven
        WriteDebugJson(projectId, debugReport);

        // 8. Opslaan
        int mergedCount   = 0;
        int conflictCount = 0;
        int ambiguousCount = 0;

        foreach (var cg in canonicalGroups)
        {
            var rep       = ChooseRepresentative(cg.Members);
            var conflicts = DetectConflicts(cg.Members);
            if (cg.IsAmbiguous) ambiguousCount++;
            if (conflicts.Any()) conflictCount++;
            if (cg.Members.Count > 1) mergedCount++;

            var cu = new CanonicalUnit
            {
                CanonicalProjectId          = projectId,
                RepresentativeMarketAssetId = rep.Asset.Id,
                Title                       = BuildTitle(rep),
                UnitNumber                  = rep.Asset.UnitNumber,
                PropertyType                = rep.Asset.PropertyType,
                Bedrooms                    = rep.Asset.Bedrooms,
                Area                        = rep.Asset.LivingArea,
                Price                       = rep.Price > 0 ? rep.Price : null,
                PricePerSqm                 = rep.Asset.LivingArea is > 0 && rep.Price > 0
                    ? Math.Round(rep.Price / rep.Asset.LivingArea!.Value, 2) : null,
                Status                      = rep.Asset.SaleStatus,
                Floor                       = rep.Floor,
                HasPriceConflict            = conflicts.Contains("Prijs"),
                HasStatusConflict           = conflicts.Contains("Status"),
                HasAreaConflict             = conflicts.Contains("Oppervlakte"),
                ConflictSummary             = conflicts.Any() ? string.Join(", ", conflicts) : null,
                IsAmbiguous                 = cg.IsAmbiguous,
                CreatedAt                   = DateTime.UtcNow,
                UpdatedAt                   = DateTime.UtcNow
            };

            _db.CanonicalUnits.Add(cu);
            await _db.SaveChangesAsync(ct);

            foreach (var m in cg.Members)
            {
                _db.CanonicalUnitAssets.Add(new CanonicalUnitAsset
                {
                    CanonicalUnitId  = cu.Id,
                    MarketAssetId    = m.Unit.Asset.Id,
                    SourceName       = m.Unit.SourceName,
                    ExternalId       = m.Unit.ExternalId,
                    MatchLevel       = m.Level,
                    MatchScore       = m.Score,
                    MatchReasons     = m.Reasons,
                    IsRepresentative = m.Unit == rep,
                    CreatedAt        = DateTime.UtcNow,
                    UpdatedAt        = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        return new CanonicalUnitRebuildResult
        {
            SourceUnitsScanned    = sourceUnits.Count,
            CanonicalUnitsCreated = canonicalGroups.Count,
            UnitsMerged           = mergedCount,
            ConflictsDetected     = conflictCount
        };
    }

    // ── Groepering ────────────────────────────────────────────────────────────

    private (List<CanonicalGroup> Groups, DebugReport Debug) BuildCanonicalGroups(
        long projectId, List<EnrichedUnit> all)
    {
        var debug = new DebugReport { ProjectId = projectId, TotalSourceUnits = all.Count };

        var zimmo   = all.Where(u => u.SourceName.Equals("Zimmo", StringComparison.OrdinalIgnoreCase)).ToList();
        var nonZimmo = all.Where(u => !u.SourceName.Equals("Zimmo", StringComparison.OrdinalIgnoreCase)).ToList();

        // Gebruik Zimmo als anchor als er Zimmo-units zijn met UnitNumber of niet-placeholder ExternalId
        bool useZimmoAnchor = zimmo.Any(u =>
            !string.IsNullOrEmpty(u.Asset.UnitNumber) ||
            (!string.IsNullOrEmpty(u.ExternalId) && !u.ExternalId.StartsWith("SOLD_")));

        debug.ZimmoUnits    = zimmo.Count;
        debug.NonZimmoUnits = nonZimmo.Count;
        debug.UseZimmoAnchor = useZimmoAnchor;

        if (useZimmoAnchor && zimmo.Count > 0)
            return (BuildAnchorGroups(zimmo, nonZimmo, debug), debug);

        // Fallback: generische pairwise matching (geen Zimmo-anchor beschikbaar)
        return (BuildPairwiseGroups(all, debug), debug);
    }

    private List<CanonicalGroup> BuildAnchorGroups(
        List<EnrichedUnit> anchors,       // Zimmo
        List<EnrichedUnit> candidates,    // Immoweb + anderen
        DebugReport debug)
    {
        // Maak één CanonicalGroup per Zimmo anchor
        var groups = anchors.Select(a => new CanonicalGroup(
            new UnitMatch(a, "Exact", 100, "ZimmoAnchor"))).ToList();

        var assigned   = new HashSet<int>(); // index in candidates-lijst
        var groupHasNonZimmo = new bool[groups.Count]; // max één non-Zimmo unit per CanonicalUnit

        // Bouw score matrix
        var matrix = new List<(int CandIdx, int GrpIdx, int Score, string Level, string Reasons)>();

        for (int ci = 0; ci < candidates.Count; ci++)
        {
            int?   bestScore  = null;
            int?   bestGrp    = null;
            string bestLevel  = "NoMatch";
            string bestReason = "";

            for (int gi = 0; gi < groups.Count; gi++)
            {
                var anchor = groups[gi].Members[0].Unit; // anchor is always first
                var (level, score, reasons) = ScoreMatchInternal(anchor, candidates[ci]);
                if (score > 0)
                {
                    debug.Candidates.Add(new DebugCandidate
                    {
                        CandidateUnit = candidates[ci].Label,
                        AnchorUnit    = anchor.Label,
                        Score         = score,
                        Level         = level,
                        Reasons       = reasons ?? ""
                    });
                }
                if (bestScore is null || score > bestScore)
                {
                    bestScore  = score;
                    bestGrp    = gi;
                    bestLevel  = level;
                    bestReason = reasons ?? "";
                }
            }
            if (bestScore.HasValue && bestScore >= ThresholdPossible)
                matrix.Add((ci, bestGrp!.Value, bestScore.Value, bestLevel, bestReason));
        }

        // Sorteer op score desc voor greedy assignment
        matrix.Sort((a, b) => b.Score.CompareTo(a.Score));

        foreach (var (ci, gi, score, level, reasons) in matrix)
        {
            if (assigned.Contains(ci)) continue;
            if (groupHasNonZimmo[gi]) continue;
            if (score < ThresholdPossible) continue;

            // Ambiguity check: zijn er meerdere even goede kandidaten voor dezelfde candidate?
            var rivals = matrix
                .Where(x => x.CandIdx == ci && x.GrpIdx != gi && x.Score >= score - AmbiguityGap)
                .ToList();

            if (rivals.Any())
            {
                _logger.LogInformation(
                    "[CanonicalUnits] Ambiguous | Unit={Unit} | TopScore={S} | RivalCount={R}",
                    candidates[ci].Label, score, rivals.Count);
                debug.Ambiguous.Add(new DebugAmbiguous
                {
                    Unit       = candidates[ci].Label,
                    TopScore   = score,
                    Candidates = rivals.Select(r => groups[r.GrpIdx].Members[0].Unit.Label).ToList()
                });
                groups[gi].IsAmbiguous = true;
                // Niet toewijzen — laat als aparte CanonicalUnit
                continue;
            }

            _logger.LogDebug(
                "[CanonicalUnits] Assigned | Unit={Unit} | Anchor={Anchor} | Score={S} | Level={L}",
                candidates[ci].Label, groups[gi].Members[0].Unit.Label, score, level);

            debug.Assigned.Add(new DebugAssignment
            {
                CandidateUnit = candidates[ci].Label,
                AnchorUnit    = groups[gi].Members[0].Unit.Label,
                Score         = score,
                Level         = level
            });

            groups[gi].Members.Add(new UnitMatch(candidates[ci], level, score, reasons));
            assigned.Add(ci);
            groupHasNonZimmo[gi] = true;
        }

        // Niet-toegewezen candidates krijgen hun eigen CanonicalGroup
        for (int ci = 0; ci < candidates.Count; ci++)
        {
            if (assigned.Contains(ci)) continue;
            _logger.LogDebug(
                "[CanonicalUnits] Unmatched | Source={Source} | Unit={Unit}",
                candidates[ci].SourceName, candidates[ci].Label);
            debug.Unmatched.Add(candidates[ci].Label);
            groups.Add(new CanonicalGroup(new UnitMatch(candidates[ci], "NoMatch", 0, null)));
        }

        return groups;
    }

    private static List<CanonicalGroup> BuildPairwiseGroups(List<EnrichedUnit> all, DebugReport debug)
    {
        var assigned = new HashSet<int>();
        var groups   = new List<CanonicalGroup>();

        for (int i = 0; i < all.Count; i++)
        {
            if (assigned.Contains(i)) continue;
            var grp = new CanonicalGroup(new UnitMatch(all[i], "Exact", 100, "Primary"));
            assigned.Add(i);

            for (int j = i + 1; j < all.Count; j++)
            {
                if (assigned.Contains(j)) continue;
                var (level, score, reasons) = ScoreMatchInternal(all[i], all[j]);
                if (score >= ThresholdProbable && level is not "NoMatch")
                {
                    grp.Members.Add(new UnitMatch(all[j], level, score, reasons));
                    assigned.Add(j);
                }
            }
            groups.Add(grp);
        }
        return groups;
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    internal static (string Level, int Score, string? Reasons) ScoreMatch(EnrichedUnit a, EnrichedUnit b)
        => ScoreMatchInternal(a, b);

    private static (string Level, int Score, string? Reasons) ScoreMatchInternal(
        EnrichedUnit a, EnrichedUnit b)
    {
        int score = 0;
        var reasons = new List<string>();

        // ── Sterke signalen ──────────────────────────────────────────────────

        // UnitNumber exact
        if (!string.IsNullOrEmpty(a.Asset.UnitNumber)
            && !string.IsNullOrEmpty(b.Asset.UnitNumber)
            && a.Asset.UnitNumber == b.Asset.UnitNumber)
        {
            score += 100; reasons.Add("UnitNumber");
        }

        // SourceUnitCode / Z-code (cross-source ExternalId — alleen als niet placeholder)
        if (!string.IsNullOrEmpty(a.ExternalId) && !string.IsNullOrEmpty(b.ExternalId)
            && a.ExternalId == b.ExternalId
            && !a.ExternalId.StartsWith("SOLD_") && !b.ExternalId.StartsWith("SOLD_")
            && a.SourceName != b.SourceName)
        {
            score += 100; reasons.Add("SourceUnitCode");
        }

        // Same URL
        if (!string.IsNullOrEmpty(a.Url) && a.Url == b.Url)
        {
            score += 100; reasons.Add("URL");
        }

        // Floor
        if (a.Floor.HasValue && b.Floor.HasValue)
        {
            if (a.Floor == b.Floor) { score += 25; reasons.Add("Floor"); }
            else { score -= 40; reasons.Add("FloorDiffers"); }
        }

        // LivingArea
        if (a.Asset.LivingArea.HasValue && b.Asset.LivingArea.HasValue)
        {
            var diff = Math.Abs(a.Asset.LivingArea.Value - b.Asset.LivingArea.Value);
            if (diff == 0)       { score += 30; reasons.Add("AreaExact"); }
            else if (diff <= 1m) { score += 25; reasons.Add("Area≤1m²"); }
            else if (diff <= 2m) { score += 20; reasons.Add("Area≤2m²"); }
            else if (diff > 5m)  { score -= 40; reasons.Add("AreaDiffers>5m²"); }
        }

        // Bedrooms
        if (a.Asset.Bedrooms.HasValue && b.Asset.Bedrooms.HasValue)
        {
            if (a.Asset.Bedrooms == b.Asset.Bedrooms) { score += 15; reasons.Add("Bedrooms"); }
            else { score -= 20; reasons.Add("BedroomsDiffer"); }
        }

        // Price
        if (a.Price > 0 && b.Price > 0)
        {
            var maxP = Math.Max(a.Price, b.Price);
            var pctDiff = Math.Abs(a.Price - b.Price) / maxP * 100m;
            if (pctDiff == 0)       { score += 25; reasons.Add("PriceExact"); }
            else if (pctDiff <= 1m) { score += 20; reasons.Add("Price≤1%"); }
            else if (pctDiff <= 2m) { score += 15; reasons.Add("Price≤2%"); }
            else if (pctDiff > 5m)  { score -= 30; reasons.Add("PriceDiffers>5%"); }
        }

        // SaleStatus (zwak signaal — alleen bonus)
        if (a.Asset.SaleStatus.HasValue && b.Asset.SaleStatus.HasValue
            && a.Asset.SaleStatus == b.Asset.SaleStatus)
        {
            score += 10; reasons.Add("Status");
        }

        // TerraceArea
        if (a.Asset.TerraceArea.HasValue && b.Asset.TerraceArea.HasValue)
        {
            if (Math.Abs(a.Asset.TerraceArea.Value - b.Asset.TerraceArea.Value) <= 2m)
            { score += 10; reasons.Add("Terrace"); }
        }

        // PropertyType
        if (a.Asset.PropertyType == b.Asset.PropertyType) { score += 10; reasons.Add("Type"); }
        else { score -= 30; reasons.Add("TypeDiffers"); }

        // Garden
        var aHasGarden = a.Asset.GardenArea > 0;
        var bHasGarden = b.Asset.GardenArea > 0;
        if (aHasGarden == bHasGarden) { score += 5; reasons.Add("Garden"); }

        // ── Level bepalen ────────────────────────────────────────────────────
        var reasonStr = reasons.Count > 0 ? string.Join(", ", reasons) : null;
        if (score < ThresholdPossible) return ("NoMatch", score, reasonStr);
        if (score < ThresholdProbable) return ("Possible", score, reasonStr);
        if (score < ThresholdExact)    return ("Probable", score, reasonStr);
        return ("Exact", score, reasonStr);
    }

    // ── Representative selectie ───────────────────────────────────────────────

    private static EnrichedUnit ChooseRepresentative(List<UnitMatch> members)
    {
        return members
            .Select(m => m.Unit)
            .OrderByDescending(u => !string.IsNullOrEmpty(u.Asset.UnitNumber) ? 1 : 0)
            .ThenByDescending(u => u.Price > 0 ? 1 : 0)
            .ThenByDescending(u => u.Asset.LivingArea.HasValue ? 1 : 0)
            .ThenByDescending(u => u.Asset.Floor.HasValue ? 1 : 0)
            .ThenByDescending(u => u.Asset.SaleStatus.HasValue ? 1 : 0)
            .ThenBy(u =>
            {
                var idx = Array.IndexOf(SourcePriority, u.SourceName);
                return idx < 0 ? 999 : idx;
            })
            .ThenByDescending(u => u.Asset.LastSeenAt)
            .First();
    }

    // ── Conflict ─────────────────────────────────────────────────────────────

    private static List<string> DetectConflicts(List<UnitMatch> members)
    {
        if (members.Count < 2) return [];
        var units     = members.Select(m => m.Unit).ToList();
        var conflicts = new List<string>();

        var prices = units.Where(u => u.Price > 0).Select(u => u.Price).Distinct().ToList();
        if (prices.Count > 1 && (prices.Max() - prices.Min()) / prices.Max() * 100m > 2m)
            conflicts.Add("Prijs");

        var statuses = units.Where(u => u.Asset.SaleStatus.HasValue)
            .Select(u => u.Asset.SaleStatus!.Value).Distinct().ToList();
        if (statuses.Count > 1) conflicts.Add("Status");

        var areas = units.Where(u => u.Asset.LivingArea.HasValue)
            .Select(u => u.Asset.LivingArea!.Value).Distinct().ToList();
        if (areas.Count > 1 && areas.Max() - areas.Min() > 2m) conflicts.Add("Oppervlakte");

        return conflicts;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildTitle(EnrichedUnit rep)
    {
        var type = rep.Asset.PropertyType switch
        {
            PropertyType.Apartment       => "Appartement",
            PropertyType.House           => "Woning",
            PropertyType.CommercialProperty => "Handelspand",
            _                            => rep.Asset.PropertyType.ToString()
        };

        if (!string.IsNullOrEmpty(rep.Asset.UnitNumber))
            return $"{type} {rep.Asset.UnitNumber}";

        var parts = new List<string> { type };
        if (rep.Asset.LivingArea is > 0) parts.Add($"{rep.Asset.LivingArea!.Value:N0} m²");
        if (rep.Asset.Bedrooms is > 0)   parts.Add($"{rep.Asset.Bedrooms} slpk");
        return string.Join(" / ", parts);
    }

    private void WriteDebugJson(long projectId, DebugReport report)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "debug", "canonical-units");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{projectId}.json");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CanonicalUnits] Debug JSON wegschrijven mislukt voor project {Id}.", projectId);
        }
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    internal sealed class CanonicalGroup
    {
        public List<UnitMatch> Members { get; } = new();
        public bool IsAmbiguous { get; set; }

        public CanonicalGroup(UnitMatch seed) => Members.Add(seed);
    }

    internal sealed record UnitMatch(EnrichedUnit Unit, string Level, int Score, string? Reasons);

    // ── Debug ─────────────────────────────────────────────────────────────────

    internal sealed class DebugReport
    {
        public long ProjectId { get; set; }
        public int TotalSourceUnits { get; set; }
        public int ZimmoUnits { get; set; }
        public int NonZimmoUnits { get; set; }
        public bool UseZimmoAnchor { get; set; }
        public List<DebugCandidate>   Candidates  { get; set; } = [];
        public List<DebugAssignment>  Assigned    { get; set; } = [];
        public List<DebugAmbiguous>   Ambiguous   { get; set; } = [];
        public List<string>           Unmatched   { get; set; } = [];
    }

    internal sealed class DebugCandidate
    {
        public string CandidateUnit { get; set; } = "";
        public string AnchorUnit    { get; set; } = "";
        public int    Score         { get; set; }
        public string Level         { get; set; } = "";
        public string Reasons       { get; set; } = "";
    }

    internal sealed class DebugAssignment
    {
        public string CandidateUnit { get; set; } = "";
        public string AnchorUnit    { get; set; } = "";
        public int    Score         { get; set; }
        public string Level         { get; set; } = "";
    }

    internal sealed class DebugAmbiguous
    {
        public string       Unit       { get; set; } = "";
        public int          TopScore   { get; set; }
        public List<string> Candidates { get; set; } = [];
    }
}

// ── EnrichedUnit ─────────────────────────────────────────────────────────────

internal sealed record EnrichedUnit(
    MarketAsset Asset,
    string SourceName,
    string ExternalId,
    string? Url,
    decimal Price)
{
    public int? Floor => Asset.Floor ?? DeriveFloorFromUnitNumber(Asset.UnitNumber);
    public string Label => !string.IsNullOrEmpty(Asset.UnitNumber)
        ? $"{SourceName}:{Asset.UnitNumber}"
        : $"{SourceName}:{ExternalId}";

    private static int? DeriveFloorFromUnitNumber(string? unitNumber)
    {
        if (string.IsNullOrEmpty(unitNumber)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(unitNumber, @"^(\d+)\.");
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }
}
