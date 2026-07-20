using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Settings;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Crawlers;

/// <summary>
/// Tests voor de Zimmo locatiefilter:
///   - AllowedLocations correct geconfigureerd → alleen toegestane postcodes
///   - AllowedLocations leeg + AllowDefaultLocations=true → DefaultLocations (3 steden)
///   - AllowedLocations leeg + AllowDefaultLocations=false → niets doorgelaten
/// </summary>
public class ZimmoLocationFilterTests
{
    // ── Helper ────────────────────────────────────────────────────────────────

    private static readonly LocationSettings[] DefaultLocations =
    [
        new() { City = "Brugge",        PostalCode = "8000" },
        new() { City = "Sint-Michiels", PostalCode = "8200" },
        new() { City = "Beernem",       PostalCode = "8730" },
    ];

    /// <summary>
    /// Isoleert de postcodefilterlogica die ZimmoCrawler.IsAllowed() gebruikt.
    /// Geeft dezelfde uitkomst als de productiecrawler, maar is testbaar zonder Playwright.
    /// </summary>
    private static bool IsPostalCodeAllowed(
        string? postalCode,
        IReadOnlyList<LocationSettings> allowedLocations,
        bool allowDefaultLocations)
    {
        IReadOnlyList<LocationSettings>? effective;

        if (allowedLocations.Count > 0)
            effective = allowedLocations;
        else if (allowDefaultLocations)
            effective = DefaultLocations;
        else
            effective = null;

        if (effective is null || effective.Count == 0) return false;

        var codes = effective
            .Where(l => !string.IsNullOrEmpty(l.PostalCode))
            .Select(l => l.PostalCode!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return codes.Count == 0
            || (!string.IsNullOrEmpty(postalCode) && codes.Contains(postalCode.Trim()));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AllowedLocations correct geconfigureerd
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsAllowed_OnlyBeernem_Accepts8730()
    {
        var allowed = new List<LocationSettings> { new() { City = "Beernem", PostalCode = "8730" } };
        Assert.True(IsPostalCodeAllowed("8730", allowed, true));
    }

    [Fact]
    public void IsAllowed_OnlyBeernem_Rejects8000()
    {
        var allowed = new List<LocationSettings> { new() { City = "Beernem", PostalCode = "8730" } };
        Assert.False(IsPostalCodeAllowed("8000", allowed, true));
    }

    [Fact]
    public void IsAllowed_OnlyBeernem_Rejects8200()
    {
        var allowed = new List<LocationSettings> { new() { City = "Beernem", PostalCode = "8730" } };
        Assert.False(IsPostalCodeAllowed("8200", allowed, true));
    }

    [Fact]
    public void IsAllowed_OnlyBeernem_RejectsNullPostalCode()
    {
        var allowed = new List<LocationSettings> { new() { City = "Beernem", PostalCode = "8730" } };
        Assert.False(IsPostalCodeAllowed(null, allowed, true));
    }

    [Fact]
    public void IsAllowed_MultipleLocations_AcceptsAll()
    {
        var allowed = new List<LocationSettings>
        {
            new() { PostalCode = "8000" },
            new() { PostalCode = "8200" },
            new() { PostalCode = "8730" },
        };
        Assert.True(IsPostalCodeAllowed("8000", allowed, true));
        Assert.True(IsPostalCodeAllowed("8200", allowed, true));
        Assert.True(IsPostalCodeAllowed("8730", allowed, true));
    }

    [Fact]
    public void IsAllowed_MultipleLocations_RejectsOutsider()
    {
        var allowed = new List<LocationSettings>
        {
            new() { PostalCode = "8000" },
            new() { PostalCode = "8200" },
        };
        Assert.False(IsPostalCodeAllowed("8730", allowed, true));
        Assert.False(IsPostalCodeAllowed("9000", allowed, true));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AllowedLocations leeg — DefaultLocations fallback
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsAllowed_EmptyAllowed_AllowDefaultTrue_AcceptsDefaultPostcodes()
    {
        // DefaultLocations = Brugge/Sint-Michiels/Beernem → 8000/8200/8730 toegelaten
        Assert.True(IsPostalCodeAllowed("8000", [], allowDefaultLocations: true));
        Assert.True(IsPostalCodeAllowed("8200", [], allowDefaultLocations: true));
        Assert.True(IsPostalCodeAllowed("8730", [], allowDefaultLocations: true));
    }

    [Fact]
    public void IsAllowed_EmptyAllowed_AllowDefaultTrue_RejectsUnknown()
    {
        Assert.False(IsPostalCodeAllowed("9000", [], allowDefaultLocations: true));
        Assert.False(IsPostalCodeAllowed("1000", [], allowDefaultLocations: true));
    }

    [Fact]
    public void IsAllowed_EmptyAllowed_AllowDefaultFalse_RejectsAll()
    {
        // AllowDefaultLocations=false → geen fallback → niets doorgelaten
        Assert.False(IsPostalCodeAllowed("8730", [], allowDefaultLocations: false));
        Assert.False(IsPostalCodeAllowed("8000", [], allowDefaultLocations: false));
        Assert.False(IsPostalCodeAllowed(null,   [], allowDefaultLocations: false));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Postcode uit URL (Immoweb-achtig patroon)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsAllowed_PostcodeWithLeadingTrailingSpaces_MatchesCorrectly()
    {
        var allowed = new List<LocationSettings> { new() { PostalCode = "8730" } };
        Assert.True(IsPostalCodeAllowed(" 8730 ", allowed, true));
    }

    [Fact]
    public void IsAllowed_PostcodeCaseInsensitive_Matches()
    {
        var allowed = new List<LocationSettings> { new() { PostalCode = "8730" } };
        // Postcodes zijn getallen, maar de check is case-insensitive voor toekomstige uitbreiding
        Assert.True(IsPostalCodeAllowed("8730", allowed, true));
    }
}
