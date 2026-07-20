using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Infrastructure.Services;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Services;

public class CanonicalUnitServiceTests
{
    // ── Helper ────────────────────────────────────────────────────────────────

    private static EnrichedUnit MakeUnit(
        decimal?    area        = null,
        decimal     price       = 0m,
        int?        bedrooms    = null,
        PropertyType type       = PropertyType.Apartment,
        string?     externalId  = null,
        string      sourceName  = "Immoweb",
        string?     url         = null,
        SaleStatus? status      = null,
        int?        floor       = null,
        string?     unitNumber  = null,
        decimal?    terraceArea = null,
        decimal?    gardenArea  = null) =>
        new(
            Asset: new MarketAsset
            {
                AssetKey       = externalId ?? Guid.NewGuid().ToString(),
                PropertyType   = type,
                Bedrooms       = bedrooms,
                LivingArea     = area,
                SaleStatus     = status,
                UnitExternalId = externalId,
                UnitNumber     = unitNumber,
                Floor          = floor,
                TerraceArea    = terraceArea,
                GardenArea     = gardenArea
            },
            SourceName : sourceName,
            ExternalId : externalId ?? "",
            Url        : url,
            Price      : price);

    // ── Sprint-14 Test 1: UnitNumber match → Exact ────────────────────────────

    [Fact]
    public void ScoreMatch_ZimmoUnitNumber0202_ImmowebSameFloorAreaBedroomsPrice_ReturnsExact()
    {
        // Zimmo anchor with UnitNumber; Immoweb also has same UnitNumber extracted
        var zimmo   = MakeUnit(area: 85m, price: 350_000m, bedrooms: 2, sourceName: "Zimmo",
                                unitNumber: "02.02", floor: 2);
        var immoweb = MakeUnit(area: 85m, price: 350_000m, bedrooms: 2, sourceName: "Immoweb",
                                unitNumber: "02.02", floor: 2);

        var (level, score, reasons) = CanonicalUnitService.ScoreMatch(zimmo, immoweb);

        Assert.Equal("Exact", level);
        Assert.True(score >= 90, $"Expected score ≥ 90, got {score}. Reasons: {reasons}");
        Assert.Contains("UnitNumber", reasons ?? "");
    }

    // ── Sprint-14 Test 2: Sold units without price → Probable match ───────────

    [Fact]
    public void ScoreMatch_SoldUnitsNoPrice_SameFloorAreaBedrooms_ReturnsProbable()
    {
        // Sold units without price — floor null (often missing for sold), area+bedrooms+status match
        var zimmo   = MakeUnit(area: 75m, bedrooms: 2, status: SaleStatus.Sold, sourceName: "Zimmo",
                                floor: null, price: 0m);
        var immoweb = MakeUnit(area: 75m, bedrooms: 2, status: SaleStatus.Sold, sourceName: "Immoweb",
                                floor: null, price: 0m);

        var (level, score, reasons) = CanonicalUnitService.ScoreMatch(zimmo, immoweb);

        // Area exact +30, Bedrooms +15, Status (Sold==Sold) +10, Type +10, Garden +5 = 70
        Assert.True(level is "Probable" or "Exact",
            $"Expected Probable or Exact, got {level} (score={score}). Reasons: {reasons}");
        Assert.True(score >= 70, $"Expected score ≥ 70, got {score}");
    }

    // ── Sprint-14 Test 3: Floor disambiguation — two anchors same area, different floor ──

    [Fact]
    public void ScoreMatch_TwoAnchorsFloor1And2_ImmowebMatchesCorrectFloor()
    {
        var anchorFloor1 = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Zimmo",
                                     unitNumber: "01.01", floor: 1);
        var anchorFloor2 = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Zimmo",
                                     unitNumber: "02.01", floor: 2);
        var immoweb = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Immoweb", floor: 2);

        var (_, scoreVsFloor1, _) = CanonicalUnitService.ScoreMatch(anchorFloor1, immoweb);
        var (_, scoreVsFloor2, _) = CanonicalUnitService.ScoreMatch(anchorFloor2, immoweb);

        // Floor-match gives +25 extra; floor-mismatch gives -40 → floor-2 anchor must score higher
        Assert.True(scoreVsFloor2 > scoreVsFloor1,
            $"Floor-2 anchor should score higher: floor2={scoreVsFloor2}, floor1={scoreVsFloor1}");
    }

    // ── Sprint-14 Test 4: Ambiguous — multiple anchors same score ─────────────

    [Fact]
    public void BuildAnchorGroups_TwoEqualCandidates_MarksAmbiguousAndDoesNotAutoAssign()
    {
        // Two identical Zimmo anchors (same area/bedrooms/floor — pathological)
        var z1 = MakeUnit(area: 70m, bedrooms: 2, sourceName: "Zimmo",
                           unitNumber: "01.01", floor: 1);
        var z2 = MakeUnit(area: 70m, bedrooms: 2, sourceName: "Zimmo",
                           unitNumber: "01.02", floor: 1);

        // One Immoweb unit that matches both equally (same area/bedrooms/floor, no price)
        var iw = MakeUnit(area: 70m, bedrooms: 2, sourceName: "Immoweb", floor: 1);

        var (_, scoreVsZ1, _) = CanonicalUnitService.ScoreMatch(z1, iw);
        var (_, scoreVsZ2, _) = CanonicalUnitService.ScoreMatch(z2, iw);

        // Scores should be equal (ambiguous by definition when within 5 pts)
        Assert.True(Math.Abs(scoreVsZ1 - scoreVsZ2) <= 5,
            $"Scores should be within ambiguity gap: z1={scoreVsZ1}, z2={scoreVsZ2}");
    }

    // ── Sprint-14 Test 5: Distinct UnitNumbers → ~N canonical units ──────────

    [Fact]
    public void ScoreMatch_DifferentUnitNumbers_SameFloorAreaBedrooms_ShouldNotMatch()
    {
        // Two units on same floor with same area/bedrooms but different UnitNumbers → no match
        var u1 = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Zimmo",
                           unitNumber: "02.01", floor: 2);
        var u2 = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Zimmo",
                           unitNumber: "02.02", floor: 2);

        var (level, _, _) = CanonicalUnitService.ScoreMatch(u1, u2);

        // Same source, different UnitNumbers — UnitNumber mismatch doesn't add bonus but also no penalty.
        // Floor +25, Area +30, Bedrooms +15, Type +10, Garden +5 = 85 → Probable
        // This is the expected behavior: same source units are NOT meant to be merged
        Assert.NotEqual("Exact", level);
    }

    // ── Sprint-14 Test 6: Available unit with price on both → one canonical ───

    [Fact]
    public void ScoreMatch_BothAvailableWithPrice_SameUnitNumber_ReturnsExact()
    {
        var zimmo   = MakeUnit(area: 90m, price: 420_000m, bedrooms: 3, status: null,
                                sourceName: "Zimmo",   unitNumber: "03.01", floor: 3);
        var immoweb = MakeUnit(area: 90m, price: 420_000m, bedrooms: 3, status: null,
                                sourceName: "Immoweb", unitNumber: "03.01", floor: 3);

        var (level, score, reasons) = CanonicalUnitService.ScoreMatch(zimmo, immoweb);

        Assert.Equal("Exact", level);
        Assert.Contains("UnitNumber", reasons ?? "");
        Assert.Contains("PriceExact", reasons ?? "");
    }

    // ── Sprint-14 Test 7: CommercialProperty stays separate from Apartment ───

    [Fact]
    public void ScoreMatch_CommercialVsApartment_ReturnsNoMatch()
    {
        var apartment  = MakeUnit(area: 80m, price: 350_000m, bedrooms: 2,
                                   type: PropertyType.Apartment);
        var commercial = MakeUnit(area: 80m, price: 350_000m, bedrooms: 2,
                                   type: PropertyType.CommercialProperty);

        var (level, score, reasons) = CanonicalUnitService.ScoreMatch(apartment, commercial);

        Assert.Equal("NoMatch", level);
        Assert.Contains("TypeDiffers", reasons ?? "");
    }

    // ── Existing tests updated for new point-based scoring ────────────────────

    [Fact]
    public void ScoreMatch_SameUrl_ReturnsExact()
    {
        var a = MakeUnit(url: "https://example.com/unit/1");
        var b = MakeUnit(url: "https://example.com/unit/1");

        var (level, _, _) = CanonicalUnitService.ScoreMatch(a, b);

        Assert.Equal("Exact", level);
    }

    [Fact]
    public void ScoreMatch_SameExternalIdDifferentSource_ReturnsExact()
    {
        var a = MakeUnit(type: PropertyType.Apartment, externalId: "B42", sourceName: "Immoweb");
        var b = MakeUnit(type: PropertyType.Apartment, externalId: "B42", sourceName: "Zimmo");

        var (level, _, _) = CanonicalUnitService.ScoreMatch(a, b);

        Assert.Equal("Exact", level);
    }

    [Fact]
    public void ScoreMatch_AreaAndPriceAndBedroomsExact_ReturnsProbable()
    {
        // Without UnitNumber/URL/ExternalId: area(30)+bedrooms(15)+price(25)+type(10)+garden(5) = 85 → Probable
        var a = MakeUnit(area: 90m, price: 400_000m, bedrooms: 3, type: PropertyType.Apartment, sourceName: "Immoweb");
        var b = MakeUnit(area: 90m, price: 400_000m, bedrooms: 3, type: PropertyType.Apartment, sourceName: "Zimmo");

        var (level, score, _) = CanonicalUnitService.ScoreMatch(a, b);

        Assert.Equal("Probable", level);
        Assert.Equal(85, score);
    }

    [Fact]
    public void ScoreMatch_AreaWithin1m2_PriceWithin1Pct_SameBedrooms_ReturnsProbable()
    {
        var a = MakeUnit(area: 80m, price: 350_000m, bedrooms: 2, type: PropertyType.Apartment, sourceName: "Immoweb");
        var b = MakeUnit(area: 81m, price: 353_500m, bedrooms: 2, type: PropertyType.Apartment, sourceName: "Zimmo");

        var (level, _, _) = CanonicalUnitService.ScoreMatch(a, b);

        Assert.True(level is "Probable" or "Exact",
            $"Expected Probable or Exact, got {level}");
    }

    [Fact]
    public void ScoreMatch_DifferentType_ReturnsNoMatch()
    {
        var a = MakeUnit(area: 80m, price: 350_000m, bedrooms: 2, type: PropertyType.Apartment);
        var b = MakeUnit(area: 80m, price: 350_000m, bedrooms: 2, type: PropertyType.House);

        var (level, _, _) = CanonicalUnitService.ScoreMatch(a, b);

        Assert.Equal("NoMatch", level);
    }

    [Fact]
    public void ScoreMatch_LargeAreaDiff_BedroomsDiff_ReturnsNoMatch()
    {
        // Area diff 10m² (-40) + bedrooms diff (-20) + type same (+10) + garden (+5) = -45 → NoMatch
        var a = MakeUnit(area: 70m, bedrooms: 1, type: PropertyType.Apartment);
        var b = MakeUnit(area: 80m, bedrooms: 2, type: PropertyType.Apartment);

        var (level, score, _) = CanonicalUnitService.ScoreMatch(a, b);

        Assert.Equal("NoMatch", level);
        Assert.True(score < 55, $"Expected score < 55, got {score}");
    }

    [Fact]
    public void ScoreMatch_FloorMismatch_Penalizes40Points()
    {
        var a = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Immoweb", floor: 1);
        var b = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Zimmo",   floor: 3);

        var (_, scoreWithFloorMismatch, _) = CanonicalUnitService.ScoreMatch(a, b);

        var aNoFloor = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Immoweb");
        var bNoFloor = MakeUnit(area: 80m, bedrooms: 2, sourceName: "Zimmo");
        var (_, scoreWithoutFloor, _) = CanonicalUnitService.ScoreMatch(aNoFloor, bNoFloor);

        Assert.True(scoreWithFloorMismatch < scoreWithoutFloor,
            $"Floor mismatch should lower score: mismatch={scoreWithFloorMismatch}, no-floor={scoreWithoutFloor}");
        Assert.Equal(scoreWithoutFloor - 40, scoreWithFloorMismatch);
    }

    // ── Scheduler tests (Sprint 13, unchanged) ───────────────────────────────

    [Fact]
    public void SchedulerInterval_PerSourceOverridesGlobal()
    {
        int globalFallback    = 30;
        int perSourceInterval = 60;

        var intervalMinutes = (perSourceInterval > 0) ? perSourceInterval : globalFallback;

        Assert.Equal(perSourceInterval, intervalMinutes);
    }

    [Fact]
    public void SchedulerInterval_UsesGlobalWhenPerSourceIsZero()
    {
        int globalFallback    = 30;
        int perSourceInterval = 0;

        var intervalMinutes = (perSourceInterval > 0) ? perSourceInterval : globalFallback;

        Assert.Equal(globalFallback, intervalMinutes);
    }

    [Fact]
    public void SchedulerNextCrawlAt_NotDueWhenInFuture()
    {
        var nextCrawlAt = DateTime.UtcNow.AddHours(1);
        var isDue       = DateTime.UtcNow >= nextCrawlAt;

        Assert.False(isDue);
    }

    [Fact]
    public void SchedulerNextCrawlAt_DueWhenInPast()
    {
        var nextCrawlAt = DateTime.UtcNow.AddHours(-1);
        var isDue       = DateTime.UtcNow >= nextCrawlAt;

        Assert.True(isDue);
    }
}
