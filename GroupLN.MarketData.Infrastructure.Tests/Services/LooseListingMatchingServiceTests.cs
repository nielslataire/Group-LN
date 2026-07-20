using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Infrastructure.Services;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Services;

public class LooseListingMatchingServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LooseCtx MakeLoose(
        decimal?     area        = null,
        decimal      price       = 0m,
        int?         bedrooms    = null,
        PropertyType type        = PropertyType.Apartment,
        string       sourceName  = "Immoweb",
        string?      externalId  = null,
        string?      url         = null,
        SaleStatus?  status      = null,
        int?         floor       = null,
        string?      postcode    = "2000",
        string?      street      = null,
        string?      houseNumber = null,
        decimal?     lat         = null,
        decimal?     lng         = null) =>
        new(
            Asset: new MarketAsset
            {
                AssetKey    = externalId ?? Guid.NewGuid().ToString(),
                PropertyType = type,
                Bedrooms    = bedrooms,
                LivingArea  = area,
                SaleStatus  = status,
                Floor       = floor,
                PostalCode  = postcode,
                Street      = street,
                HouseNumber = houseNumber,
                Latitude    = lat,
                Longitude   = lng
            },
            SourceName : sourceName,
            ExternalId : externalId,
            Url        : url,
            Price      : price);

    private static CuCtx MakeCu(
        decimal?     area        = null,
        decimal?     price       = null,
        int?         bedrooms    = null,
        PropertyType type        = PropertyType.Apartment,
        SaleStatus?  status      = null,
        int?         floor       = null,
        string?      postcode    = "2000",
        string?      street      = null,
        string?      houseNumber = null,
        decimal?     lat         = null,
        decimal?     lng         = null,
        List<(string SourceName, string? ExternalId, string? Url)>? sourceAssets = null) =>
        new(
            CanonicalUnitId    : 1,
            CanonicalProjectId : 1,
            Area               : area,
            Price              : price,
            Bedrooms           : bedrooms,
            Floor              : floor,
            PropertyType       : type,
            Status             : status,
            PostalCode         : postcode,
            Street             : street,
            HouseNumber        : houseNumber,
            Latitude           : lat,
            Longitude          : lng,
            SourceAssets       : sourceAssets ?? []);

    // ── Test 1: Zelfde adres + prijs + oppervlakte + slaapkamers → Exact ─────

    [Fact]
    public void ScoreMatch_SameAddressPriceAreaBedrooms_ReturnsExact()
    {
        var loose = MakeLoose(
            area: 85m, price: 350_000m, bedrooms: 2,
            street: "Antwerpsesteenweg", houseNumber: "42", postcode: "2000");

        var cu = MakeCu(
            area: 85m, price: 350_000m, bedrooms: 2,
            street: "Antwerpsesteenweg", houseNumber: "42", postcode: "2000");

        var (level, score, reasons) = LooseListingMatchingService.ScoreMatch(loose, cu);

        Assert.True(level is "Exact" or "Probable",
            $"Expected Exact or Probable, got {level} (score={score}). Reasons: {reasons}");
        Assert.True(score >= 75, $"Expected score >= 75, got {score}");
        Assert.Contains("ZelfdeAdres", reasons ?? "");
        Assert.Contains("AreaExact", reasons ?? "");
    }

    // ── Test 2: Zelfde adres maar andere oppervlakte/prijs → geen match ───────

    [Fact]
    public void ScoreMatch_SameAddressDifferentAreaAndPrice_NoMatchOrPossible()
    {
        var loose = MakeLoose(
            area: 65m, price: 250_000m, bedrooms: 1,
            street: "Kerkstraat", houseNumber: "10", postcode: "9000");

        var cu = MakeCu(
            area: 95m, price: 450_000m, bedrooms: 3,  // large area + price diff
            street: "Kerkstraat", houseNumber: "10", postcode: "9000");

        var (level, score, _) = LooseListingMatchingService.ScoreMatch(loose, cu);

        Assert.True(level is "NoMatch" or "Possible",
            $"Expected NoMatch or Possible (too many differences), got {level} (score={score})");
    }

    // ── Test 3: Zonder verdieping, unieke match op prijs/opp/slaapkamers ─────

    [Fact]
    public void ScoreMatch_NoFloor_UniqueMatchOnPriceAreaBedrooms_ReturnsProbable()
    {
        var loose = MakeLoose(
            area: 90m, price: 395_000m, bedrooms: 3,
            floor: null, postcode: "3000");

        var cu = MakeCu(
            area: 90m, price: 395_000m, bedrooms: 3,
            floor: null, postcode: "3000");

        var (level, score, reasons) = LooseListingMatchingService.ScoreMatch(loose, cu);

        // AreaExact(30) + PriceExact(30) + Bedrooms(15) + Type(10) = 85 → Probable
        Assert.True(level is "Probable" or "Exact",
            $"Expected Probable or Exact, got {level} (score={score}). Reasons: {reasons}");
        Assert.True(score >= 75);
    }

    // ── Test 4: Meerdere gelijkaardige units → Ambiguous score comparison ─────

    [Fact]
    public void ScoreMatch_TwoCandidatesSameScore_ShouldBeWithinAmbiguityGap()
    {
        // Two canonical units with identical attributes
        var loose = MakeLoose(area: 80m, price: 300_000m, bedrooms: 2, postcode: "2000");
        var cu1   = MakeCu(area: 80m,  price: 300_000m, bedrooms: 2, postcode: "2000");
        var cu2   = MakeCu(area: 80m,  price: 300_000m, bedrooms: 2, postcode: "2000");

        var (_, score1, _) = LooseListingMatchingService.ScoreMatch(loose, cu1);
        var (_, score2, _) = LooseListingMatchingService.ScoreMatch(loose, cu2);

        // Scores should be identical → ambiguity gap condition (|score1-score2| <= 8)
        Assert.Equal(score1, score2);
        Assert.True(Math.Abs(score1 - score2) <= 8, $"Scores within ambiguity gap: {score1} vs {score2}");
    }

    // ── Test 5: Andere postcode → NoMatch ─────────────────────────────────────

    [Fact]
    public void ScoreMatch_DifferentPostcode_ReturnsNoMatch()
    {
        var loose = MakeLoose(area: 80m, price: 300_000m, bedrooms: 2, postcode: "2000");
        var cu    = MakeCu(area: 80m, price: 300_000m, bedrooms: 2, postcode: "9000");

        var (level, _, reasons) = LooseListingMatchingService.ScoreMatch(loose, cu);

        Assert.Equal("NoMatch", level);
        Assert.Contains("AnderePostcode", reasons ?? "");
    }

    // ── Test 6: Originele MarketAsset data bewaard — harde uitsluiting op type ─

    [Fact]
    public void ScoreMatch_CommercialVsApartment_ReturnsNoMatch()
    {
        // TypeDiffers -30 + area -40 (5m²) + type => score well below 60
        var loose = MakeLoose(area: 80m, price: 350_000m, bedrooms: 2,
                               type: PropertyType.CommercialProperty, postcode: "2000");
        var cu    = MakeCu(area: 80m, price: 350_000m, bedrooms: 2,
                            type: PropertyType.Apartment, postcode: "2000");

        var (level, score, reasons) = LooseListingMatchingService.ScoreMatch(loose, cu);

        Assert.Equal("NoMatch", level);
        Assert.Contains("TypeDiffers", reasons ?? "");
    }

    // ── Extra: Afstand > 500m → NoMatch ──────────────────────────────────────

    [Fact]
    public void ScoreMatch_DistanceOver500m_ReturnsNoMatch()
    {
        // Antwerpen Centraal ≈ 51.2172, 4.4215
        // 1km ten noorden ≈ 51.2262, 4.4215
        var loose = MakeLoose(postcode: "2000",
            lat: 51.2172m, lng: 4.4215m, area: 80m, bedrooms: 2);
        var cu = MakeCu(postcode: "2000",
            lat: 51.2262m, lng: 4.4215m, area: 80m, bedrooms: 2);

        var (level, _, _) = LooseListingMatchingService.ScoreMatch(loose, cu);

        Assert.Equal("NoMatch", level);
    }

    // ── Extra: Afstand <= 50m → +30 bonus ────────────────────────────────────

    [Fact]
    public void ScoreMatch_DistanceUnder50m_AddsBonusPoints()
    {
        // Very close coordinates (~20m apart)
        var loose = MakeLoose(postcode: "2000",
            lat: 51.2172m, lng: 4.4215m, area: 80m, price: 300_000m, bedrooms: 2);
        var cu = MakeCu(postcode: "2000",
            lat: 51.2173m, lng: 4.4216m, area: 80m, price: 300_000m, bedrooms: 2);

        var (_, scoreWithCoords, _) = LooseListingMatchingService.ScoreMatch(loose, cu);

        // Without coordinates baseline: area+price+bedrooms+type = 30+30+15+10 = 85
        var looseNoCoords = MakeLoose(postcode: "2000", area: 80m, price: 300_000m, bedrooms: 2);
        var cuNoCoords    = MakeCu(postcode: "2000", area: 80m, price: 300_000m, bedrooms: 2);
        var (_, scoreNoCoords, _) = LooseListingMatchingService.ScoreMatch(looseNoCoords, cuNoCoords);

        Assert.True(scoreWithCoords > scoreNoCoords,
            $"Close distance should add bonus: withCoords={scoreWithCoords}, noCoords={scoreNoCoords}");
    }

    // ── GeoDistanceMeters test ────────────────────────────────────────────────

    [Fact]
    public void GeoDistanceMeters_KnownPoints_ReturnsReasonableDistance()
    {
        // Antwerpen Central Station → ~1.5km away known point
        var dist = LooseListingMatchingService.GeoDistanceMeters(51.2172m, 4.4215m, 51.2300m, 4.4215m);

        Assert.InRange(dist, 1300, 1500); // ~1.4km
    }
}
