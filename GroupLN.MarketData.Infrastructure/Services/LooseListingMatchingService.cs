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
/// Matcht losse listings aan bestaande CanonicalUnits in nabijgelegen projecten.
/// </summary>
public class LooseListingMatchingService : ILooseListingMatchingService
{
    private readonly MarketDataDbContext _db;
    private readonly ILogger<LooseListingMatchingService> _logger;

    private const int ThresholdExact    = 90;
    private const int ThresholdProbable = 75;
    private const int ThresholdPossible = 60;
    private const int AmbiguityGap      = 8;
    private const double MaxMatchDistanceMeters = 500;

    public LooseListingMatchingService(MarketDataDbContext db, ILogger<LooseListingMatchingService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Publieke interface ────────────────────────────────────────────────────

    public async Task<LooseListingMatchResult> MatchLooseListingsAsync(
        CancellationToken ct = default)
    {
        _logger.LogInformation("[LooseUnitMatch] Start");

        // 1. Verwijder bestaande losse koppelingen (herbouw volledig)
        var existing = await _db.CanonicalUnitAssets
            .Where(a => a.IsFromLooseListing)
            .ToListAsync(ct);
        _db.CanonicalUnitAssets.RemoveRange(existing);

        var linkedAssets = await _db.MarketAssets
            .Where(a => a.LinkedCanonicalUnitId.HasValue && !a.IsProjectGroup && a.ParentMarketAssetId == null)
            .ToListAsync(ct);
        foreach (var a in linkedAssets) a.LinkedCanonicalUnitId = null;

        await _db.SaveChangesAsync(ct);

        // 2. Laad actieve losse listings
        var looseAssets = await _db.MarketAssets
            .Where(a => !a.IsProjectGroup
                     && a.ParentMarketAssetId == null
                     && a.IsActive
                     && (a.PropertyType == PropertyType.Apartment
                      || a.PropertyType == PropertyType.House
                      || a.PropertyType == PropertyType.CommercialProperty))
            .AsNoTracking()
            .ToListAsync(ct);

        _logger.LogInformation("[LooseUnitMatch] Candidates={C}", looseAssets.Count);

        if (looseAssets.Count == 0)
        {
            _logger.LogInformation("[LooseUnitMatch] Done | Geen losse listings gevonden.");
            return new LooseListingMatchResult();
        }

        // 3. Listings ophalen voor prijs + URL + bron
        var looseIds = looseAssets.Select(a => a.Id).ToList();
        var listings = await _db.MarketListings
            .Where(l => looseIds.Contains(l.MarketAssetId) && l.IsActive)
            .Select(l => new { l.MarketAssetId, SourceName = l.Source.Name, l.ExternalId, l.Url, l.AskingPrice })
            .AsNoTracking()
            .ToListAsync(ct);

        var listingByAsset = listings
            .GroupBy(l => l.MarketAssetId)
            .ToDictionary(g => g.Key, g => g.First());

        var looseCandidates = looseAssets
            .Select(a =>
            {
                listingByAsset.TryGetValue(a.Id, out var lst);
                return new LooseCtx(
                    Asset      : a,
                    SourceName : lst?.SourceName ?? "",
                    ExternalId : lst?.ExternalId,
                    Url        : lst?.Url,
                    Price      : lst?.AskingPrice ?? 0m);
            })
            .ToList();

        // 4. Groepeer per postcode voor efficiënte candidate lookup
        var postalCodes = looseCandidates
            .Where(l => !string.IsNullOrEmpty(l.Asset.PostalCode))
            .Select(l => l.Asset.PostalCode!)
            .Distinct()
            .ToList();

        // 5. Laad CanonicalUnits per postcode (via CanonicalProjectAsset → MarketAsset.PostalCode)
        var projectIdsByPostcode = await _db.CanonicalProjectAssets
            .Where(cpa => cpa.MarketAsset.PostalCode != null
                       && postalCodes.Contains(cpa.MarketAsset.PostalCode))
            .Select(cpa => new { cpa.CanonicalProjectId, cpa.MarketAsset.PostalCode })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        var allProjectIds = projectIdsByPostcode.Select(x => x.CanonicalProjectId).Distinct().ToList();

        var canonicalUnits = await _db.CanonicalUnits
            .Where(u => allProjectIds.Contains(u.CanonicalProjectId))
            .Include(u => u.SourceAssets)
            .AsNoTracking()
            .ToListAsync(ct);

        // 6. Laad representatieve MarketAssets voor adres/coördinaten
        var repIds = canonicalUnits.Select(u => u.RepresentativeMarketAssetId).Distinct().ToList();
        var repAssets = await _db.MarketAssets
            .Where(a => repIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, ct);

        // 7. Bouw CuCtx per CanonicalUnit
        var projectPostalMap = projectIdsByPostcode
            .GroupBy(x => x.CanonicalProjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PostalCode!).ToHashSet());

        var cuContexts = canonicalUnits.Select(cu =>
        {
            repAssets.TryGetValue(cu.RepresentativeMarketAssetId, out var rep);
            return new CuCtx(
                CanonicalUnitId    : cu.Id,
                CanonicalProjectId : cu.CanonicalProjectId,
                Area               : cu.Area,
                Price              : cu.Price,
                Bedrooms           : cu.Bedrooms,
                Floor              : cu.Floor,
                PropertyType       : cu.PropertyType,
                Status             : cu.Status,
                PostalCode         : rep?.PostalCode,
                Street             : rep?.Street,
                HouseNumber        : rep?.HouseNumber,
                Latitude           : rep?.Latitude,
                Longitude          : rep?.Longitude,
                SourceAssets       : cu.SourceAssets
                    .Select(sa => (sa.SourceName, sa.ExternalId, (string?)null))
                    .ToList());
        }).ToList();

        // ── Groepeer CuCtx per postcode voor snelle lookup ────────────────────
        var cuByPostcode = cuContexts
            .Where(cu => !string.IsNullOrEmpty(cu.PostalCode))
            .GroupBy(cu => cu.PostalCode!)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 8. Score + match
        var debugReport = new DebugReport();
        int matchedCount   = 0;
        int ambiguousCount = 0;
        int unmatchedCount = 0;

        // Aantal losse listings dat deze run al aan een CanonicalUnit gekoppeld is —
        // gebruikt om gelijkwaardige duplicaten één-op-één te verdelen.
        var usedCuCounts = new Dictionary<long, int>();

        foreach (var loose in looseCandidates)
        {
            if (ct.IsCancellationRequested) break;

            var postcode = loose.Asset.PostalCode;
            if (string.IsNullOrEmpty(postcode))
            {
                unmatchedCount++;
                debugReport.Unmatched.Add(new DebugUnmatched { Unit = loose.Label, Reason = "Geen postcode" });
                continue;
            }

            if (!cuByPostcode.TryGetValue(postcode, out var candidates) || candidates.Count == 0)
            {
                unmatchedCount++;
                debugReport.Unmatched.Add(new DebugUnmatched { Unit = loose.Label, Reason = "Geen projectunits in zelfde postcode" });
                continue;
            }

            var scored = new List<(CuCtx Cu, string Level, int Score, string? Reasons)>();
            foreach (var cu in candidates)
            {
                var (level, score, reasons) = ScoreMatch(loose, cu);
                if (score > 0 || level != "NoMatch")
                {
                    debugReport.Candidates.Add(new DebugCandidate
                    {
                        LooseUnit     = loose.Label,
                        CanonicalUnit = $"CU#{cu.CanonicalUnitId}",
                        Score         = score,
                        Level         = level,
                        Reasons       = reasons ?? ""
                    });
                }
                if (score >= ThresholdPossible)
                    scored.Add((cu, level, score, reasons));
            }

            if (scored.Count == 0)
            {
                unmatchedCount++;
                debugReport.Unmatched.Add(new DebugUnmatched { Unit = loose.Label, Reason = "Score te laag" });
                _logger.LogDebug("[LooseUnitMatch] Unmatched | LooseAsset={Id} | Reason=ScoreTeLaag", loose.Asset.Id);
                continue;
            }

            // Sorteer op score desc
            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            var best = scored[0];

            // Ambiguity check: top-2 binnen gap
            if (scored.Count > 1 && scored[1].Score >= best.Score - AmbiguityGap)
            {
                // Als de gebonden topkandidaten gelijkwaardig zijn (zelfde gebouw,
                // zelfde prijs, circa-oppervlakte), is koppelen aan om het even welke
                // correct — verdeel dan één-op-één (minst gebruikte eerst) i.p.v. overslaan.
                var tied = scored.Where(s => s.Score >= best.Score - AmbiguityGap).ToList();

                // Verdieping kan alleen onderscheiden als de losse listing er zelf één heeft;
                // zonder verdieping op de listing zijn units die enkel in verdieping
                // verschillen niet uit elkaar te houden en dus gelijkwaardig.
                bool ignoreFloor = !loose.Asset.Floor.HasValue;

                var equivalentGroup = tied
                    .Where(t => SameBuilding(best.Cu, t.Cu) && AreEquivalentUnits(best.Cu, t.Cu, ignoreFloor))
                    .ToList();

                // Niet-gelijkwaardige kandidaten blokkeren de resolutie alleen als ze
                // even hoog scoren als de topkandidaat.
                bool blocked = tied
                    .Where(t => !equivalentGroup.Contains(t))
                    .Any(o => o.Score >= best.Score);

                if (!blocked && equivalentGroup.Count > 0)
                {
                    best = equivalentGroup
                        .OrderBy(t => usedCuCounts.GetValueOrDefault(t.Cu.CanonicalUnitId))
                        .ThenByDescending(t => t.Score)
                        .First();
                    best = (best.Cu, best.Level, best.Score,
                        best.Reasons is null ? "EquivalentTieResolved" : best.Reasons + ", EquivalentTieResolved");
                }
                else
                {
                    ambiguousCount++;
                    debugReport.Ambiguous.Add(new DebugAmbiguous
                    {
                        LooseUnit  = loose.Label,
                        TopScore   = best.Score,
                        Candidates = scored.Take(3).Select(s => $"CU#{s.Cu.CanonicalUnitId} ({s.Score})").ToList(),
                        Reason     = "MultipleSimilarUnits"
                    });
                    _logger.LogInformation(
                        "[LooseUnitMatch] Ambiguous | LooseAsset={Id} | Candidates={Cands} | Reason=MultipleSimilarUnits",
                        loose.Asset.Id,
                        string.Join(", ", scored.Take(3).Select(s => $"CU#{s.Cu.CanonicalUnitId}({s.Score})")));
                    continue;
                }
            }

            if (best.Score < ThresholdPossible) { unmatchedCount++; continue; }

            usedCuCounts[best.Cu.CanonicalUnitId] = usedCuCounts.GetValueOrDefault(best.Cu.CanonicalUnitId) + 1;

            // Koppelen
            _logger.LogInformation(
                "[LooseUnitMatch] Matched | LooseAsset={Id} | CanonicalUnit={CuId} | Score={S} | Level={L} | Reasons={R}",
                loose.Asset.Id, best.Cu.CanonicalUnitId, best.Score, best.Level, best.Reasons);

            debugReport.Assigned.Add(new DebugAssignment
            {
                LooseUnit     = loose.Label,
                CanonicalUnit = $"CU#{best.Cu.CanonicalUnitId}",
                Score         = best.Score,
                Level         = best.Level
            });

            // CanonicalUnitAsset toevoegen
            _db.CanonicalUnitAssets.Add(new CanonicalUnitAsset
            {
                CanonicalUnitId    = best.Cu.CanonicalUnitId,
                MarketAssetId      = loose.Asset.Id,
                SourceName         = loose.SourceName,
                ExternalId         = loose.ExternalId,
                MatchLevel         = best.Level,
                MatchScore         = best.Score,
                MatchReasons       = best.Reasons,
                IsRepresentative   = false,
                IsFromLooseListing = true,
                CreatedAt          = DateTime.UtcNow,
                UpdatedAt          = DateTime.UtcNow
            });

            // LinkedCanonicalUnitId op het MarketAsset zetten
            var assetToUpdate = await _db.MarketAssets.FindAsync([loose.Asset.Id], ct);
            if (assetToUpdate is not null)
                assetToUpdate.LinkedCanonicalUnitId = best.Cu.CanonicalUnitId;

            await _db.SaveChangesAsync(ct);
            matchedCount++;
        }

        _logger.LogInformation(
            "[LooseUnitMatch] Done | Matched={M} | Ambiguous={A} | Unmatched={U}",
            matchedCount, ambiguousCount, unmatchedCount);

        WriteDebugJson(debugReport);

        return new LooseListingMatchResult
        {
            CandidatesEvaluated = looseCandidates.Count,
            Matched             = matchedCount,
            Ambiguous           = ambiguousCount,
            Unmatched           = unmatchedCount
        };
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    internal static (string Level, int Score, string? Reasons) ScoreMatch(LooseCtx loose, CuCtx cu)
    {
        var reasons = new List<string>();
        int score   = 0;

        // ── Harde uitsluiting: andere postcode ──────────────────────────────
        if (!string.IsNullOrEmpty(loose.Asset.PostalCode)
            && !string.IsNullOrEmpty(cu.PostalCode)
            && loose.Asset.PostalCode != cu.PostalCode)
        {
            return ("NoMatch", 0, "AnderePostcode");
        }

        // ── Zelfde bron + zelfde ExternalId ─────────────────────────────────
        if (!string.IsNullOrEmpty(loose.ExternalId)
            && cu.SourceAssets.Any(sa =>
                sa.SourceName.Equals(loose.SourceName, StringComparison.OrdinalIgnoreCase)
                && sa.ExternalId == loose.ExternalId))
        {
            score += 100; reasons.Add("SameSourceExternalId");
        }

        // ── Zelfde URL ───────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(loose.Url)
            && cu.SourceAssets.Any(sa =>
                !string.IsNullOrEmpty(sa.Url) && sa.Url == loose.Url))
        {
            score += 100; reasons.Add("SameUrl");
        }

        // ── Afstand (indien coördinaten beschikbaar) ─────────────────────────
        double? distM = null;
        if (loose.Asset.Latitude.HasValue && loose.Asset.Longitude.HasValue
            && cu.Latitude.HasValue && cu.Longitude.HasValue)
        {
            distM = GeoDistanceMeters(
                loose.Asset.Latitude.Value, loose.Asset.Longitude.Value,
                cu.Latitude.Value, cu.Longitude.Value);

            if (distM > MaxMatchDistanceMeters)
                return ("NoMatch", 0, $"AfstandTeProot ({distM:F0}m)");

            if (distM <= 50)  { score += 30; reasons.Add("Dist≤50m"); }
            else if (distM <= 100) { score += 20; reasons.Add("Dist≤100m"); }
            else if (distM <= 250) { score += 10; reasons.Add("Dist≤250m"); }
        }

        // ── Adres ────────────────────────────────────────────────────────────
        var looseStreet = Norm(loose.Asset.Street);
        var cuStreet    = Norm(cu.Street);
        var looseNum    = Norm(loose.Asset.HouseNumber);
        var cuNum       = Norm(cu.HouseNumber);

        bool sameStreet = !string.IsNullOrEmpty(looseStreet) && looseStreet == cuStreet;
        bool sameNumber = !string.IsNullOrEmpty(looseNum)    && looseNum == cuNum;

        if (sameStreet && sameNumber) { score += 40; reasons.Add("ZelfdeAdres"); }
        else if (sameStreet)          { score += 20; reasons.Add("ZelfdeStraat"); }
        else if (sameNumber)          { score += 20; reasons.Add("ZelfdeHuisnummer"); }
        else if (distM is null && !sameStreet && !string.IsNullOrEmpty(looseStreet) && !string.IsNullOrEmpty(cuStreet))
        {
            // Andere straat én geen coördinaten — sterke penalty
            score -= 30; reasons.Add("AndereStraat");
        }

        // ── PropertyType ─────────────────────────────────────────────────────
        if (loose.Asset.PropertyType == cu.PropertyType) { score += 10; reasons.Add("Type"); }
        else { score -= 30; reasons.Add("TypeDiffers"); }

        // ── LivingArea ───────────────────────────────────────────────────────
        if (loose.Asset.LivingArea.HasValue && cu.Area.HasValue)
        {
            var diff = Math.Abs(loose.Asset.LivingArea.Value - cu.Area.Value);
            if (diff == 0)       { score += 30; reasons.Add("AreaExact"); }
            else if (diff <= 1m) { score += 25; reasons.Add("Area≤1m²"); }
            else if (diff <= 2m) { score += 20; reasons.Add("Area≤2m²"); }
            else if (diff > 5m)  { score -= 40; reasons.Add("AreaDiffers>5m²"); }
        }

        // ── Bedrooms ─────────────────────────────────────────────────────────
        if (loose.Asset.Bedrooms.HasValue && cu.Bedrooms.HasValue)
        {
            if (loose.Asset.Bedrooms == cu.Bedrooms) { score += 15; reasons.Add("Bedrooms"); }
            else                                     { score -= 20; reasons.Add("BedroomsDiffer"); }
        }

        // ── Price ────────────────────────────────────────────────────────────
        if (loose.Price > 0 && cu.Price is > 0)
        {
            var maxP    = Math.Max(loose.Price, cu.Price.Value);
            var pctDiff = Math.Abs(loose.Price - cu.Price.Value) / maxP * 100m;
            if (pctDiff == 0)       { score += 30; reasons.Add("PriceExact"); }
            else if (pctDiff <= 1m) { score += 25; reasons.Add("Price≤1%"); }
            else if (pctDiff <= 2m) { score += 20; reasons.Add("Price≤2%"); }
            else if (pctDiff > 5m)  { score -= 40; reasons.Add("PriceDiffers>5%"); }
        }

        // ── Floor ────────────────────────────────────────────────────────────
        if (loose.Asset.Floor.HasValue && cu.Floor.HasValue)
        {
            if (loose.Asset.Floor == cu.Floor) { score += 20; reasons.Add("Floor"); }
            else                               { score -= 15; reasons.Add("FloorDiffers"); }
        }

        // ── Status ───────────────────────────────────────────────────────────
        if (loose.Asset.SaleStatus.HasValue && cu.Status.HasValue
            && loose.Asset.SaleStatus == cu.Status)
        {
            score += 10; reasons.Add("Status");
        }

        // ── Sterke combinatie: zelfde adres + zelfde prijs + circa-oppervlakte ─
        // Dekt units waarvan enkel de m² licht afwijkt tussen bronnen (bijv. 73 vs 75 m²).
        bool priceClose = loose.Price > 0 && cu.Price is > 0
            && Math.Abs(loose.Price - cu.Price.Value) / Math.Max(loose.Price, cu.Price.Value) * 100m <= 1m;
        bool areaClose = loose.Asset.LivingArea.HasValue && cu.Area.HasValue
            && Math.Abs(loose.Asset.LivingArea.Value - cu.Area.Value) <= 3m;
        bool floorCompatible = !loose.Asset.Floor.HasValue || !cu.Floor.HasValue
            || loose.Asset.Floor == cu.Floor;
        if (sameStreet && sameNumber && priceClose && areaClose && floorCompatible)
        {
            score += 30; reasons.Add("AdresPrijsOppCombo");
        }

        var reasonStr = reasons.Count > 0 ? string.Join(", ", reasons) : null;
        if (score < ThresholdPossible) return ("NoMatch",  score, reasonStr);
        if (score < ThresholdProbable) return ("Possible", score, reasonStr);
        if (score < ThresholdExact)    return ("Probable", score, reasonStr);
        return ("Exact", score, reasonStr);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Twee projectunits zijn gelijkwaardig als prijs, circa-oppervlakte, verdieping
    // en slaapkamers overeenkomen (null telt als compatibel). Bij gelijkwaardige
    // kandidaten is koppelen aan om het even welke unit correct voor deduplicatie.
    // Met ignoreFloor=true telt een verdiepingsverschil niet mee (gebruikt wanneer
    // de losse listing zelf geen verdieping heeft en dus niet kan onderscheiden).
    internal static bool AreEquivalentUnits(CuCtx a, CuCtx b, bool ignoreFloor = false)
    {
        if (a.CanonicalUnitId == b.CanonicalUnitId) return true;

        bool priceEq = a.Price is null || b.Price is null
            || (a.Price > 0 && b.Price > 0
                && Math.Abs(a.Price.Value - b.Price.Value) / Math.Max(a.Price.Value, b.Price.Value) * 100m <= 1m);

        bool areaEq = a.Area is null || b.Area is null
            || Math.Abs(a.Area.Value - b.Area.Value) <= 3m;

        bool floorEq = ignoreFloor || !a.Floor.HasValue || !b.Floor.HasValue || a.Floor == b.Floor;

        bool bedsEq = !a.Bedrooms.HasValue || !b.Bedrooms.HasValue || a.Bedrooms == b.Bedrooms;

        return priceEq && areaEq && floorEq && bedsEq;
    }

    // Zelfde gebouw: zelfde CanonicalProject, of zelfde genormaliseerde straat +
    // huisnummer (dekt duplicaat-projecten op hetzelfde adres die de projectdeduplicatie
    // nog niet heeft samengevoegd).
    internal static bool SameBuilding(CuCtx a, CuCtx b)
    {
        if (a.CanonicalProjectId == b.CanonicalProjectId) return true;

        var street = Norm(a.Street);
        var number = Norm(a.HouseNumber);
        return !string.IsNullOrEmpty(street) && street == Norm(b.Street)
            && !string.IsNullOrEmpty(number) && number == Norm(b.HouseNumber);
    }

    internal static double GeoDistanceMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double R = 6371000;
        var dLat = (double)(lat2 - lat1) * Math.PI / 180;
        var dLng = (double)(lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos((double)lat1 * Math.PI / 180) * Math.Cos((double)lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static string Norm(string? s) =>
        (s ?? "").Trim().ToLowerInvariant()
                 .Replace("  ", " ")
                 .Replace(".", "")
                 .Replace(",", "");

    private void WriteDebugJson(DebugReport report)
    {
        try
        {
            var dir  = Path.Combine(AppContext.BaseDirectory, "debug", "loose-unit-matching");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "report.json");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented          = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LooseUnitMatch] Debug JSON wegschrijven mislukt.");
        }
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    internal sealed class DebugReport
    {
        public List<DebugCandidate>   Candidates { get; set; } = [];
        public List<DebugAssignment>  Assigned   { get; set; } = [];
        public List<DebugAmbiguous>   Ambiguous  { get; set; } = [];
        public List<DebugUnmatched>   Unmatched  { get; set; } = [];
    }

    internal sealed class DebugCandidate
    {
        public string LooseUnit     { get; set; } = "";
        public string CanonicalUnit { get; set; } = "";
        public int    Score         { get; set; }
        public string Level         { get; set; } = "";
        public string Reasons       { get; set; } = "";
    }

    internal sealed class DebugAssignment
    {
        public string LooseUnit     { get; set; } = "";
        public string CanonicalUnit { get; set; } = "";
        public int    Score         { get; set; }
        public string Level         { get; set; } = "";
    }

    internal sealed class DebugAmbiguous
    {
        public string       LooseUnit  { get; set; } = "";
        public int          TopScore   { get; set; }
        public List<string> Candidates { get; set; } = [];
        public string       Reason     { get; set; } = "";
    }

    internal sealed class DebugUnmatched
    {
        public string Unit   { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}

// ── Context records (outside class for testability) ───────────────────────────

internal sealed record LooseCtx(
    MarketAsset Asset,
    string  SourceName,
    string? ExternalId,
    string? Url,
    decimal Price)
{
    public string Label => $"{SourceName}:{Asset.Id}";
}

internal sealed record CuCtx(
    long          CanonicalUnitId,
    long          CanonicalProjectId,
    decimal?      Area,
    decimal?      Price,
    int?          Bedrooms,
    int?          Floor,
    PropertyType  PropertyType,
    SaleStatus?   Status,
    string?       PostalCode,
    string?       Street,
    string?       HouseNumber,
    decimal?      Latitude,
    decimal?      Longitude,
    List<(string SourceName, string? ExternalId, string? Url)> SourceAssets);
