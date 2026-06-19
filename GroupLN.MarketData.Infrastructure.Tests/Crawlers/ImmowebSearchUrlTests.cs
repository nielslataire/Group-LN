using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Crawlers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Crawlers;

/// <summary>
/// Tests voor BuildPagedSearchUrl — URL-generatie met en zonder {page} placeholder.
/// </summary>
public class ImmowebSearchUrlTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static LocationSettings Loc(string city, string postalCode, string? slug = null)
        => new() { City = city, PostalCode = postalCode, CitySlug = slug ?? city.ToLowerInvariant() };

    // ══════════════════════════════════════════════════════════════════════════
    // BuildPagedSearchUrl — template MET {page}
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithPage_Page1_ReplacesCorrectly()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&page={page}";
        var loc = Loc("Brugge", "8000");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 1);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?postalCodes=BE-8000&page=1", result);
    }

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithPage_Page2_ReplacesCorrectly()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&page={page}";
        var loc = Loc("Brugge", "8000");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 2);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?postalCodes=BE-8000&page=2", result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BuildPagedSearchUrl — template ZONDER {page}
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithoutPage_Page1_NoPageParam()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&orderBy=relevance";
        var loc = Loc("Brugge", "8000");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 1);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?postalCodes=BE-8000&orderBy=relevance", result);
        Assert.DoesNotContain("page=", result);
    }

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithoutPage_Page2_AppendsPageParam()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&orderBy=relevance";
        var loc = Loc("Brugge", "8000");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 2);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?postalCodes=BE-8000&orderBy=relevance&page=2", result);
    }

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithoutPage_Page3_AppendsPageParam()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}";
        var loc = Loc("Beernem", "8730");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "beernem", 3);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?postalCodes=BE-8730&page=3", result);
    }

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithoutPageOrQueryString_Page2_UsesQuestionMark()
    {
        // Template zonder '?' in de URL
        const string template = "https://www.immoweb.be/nl/zoeken/{postalCode}";
        var loc = Loc("Brugge", "8000");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 2);

        Assert.Equal("https://www.immoweb.be/nl/zoeken/8000?page=2", result);
    }

    [Fact]
    public void BuildPagedSearchUrl_TemplateWithExistingPageParam_Page2_ReplacesExisting()
    {
        // Template die al page= bevat (bijv. door foutieve config)
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&page=1";
        var loc = Loc("Brugge", "8000");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 2);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?postalCodes=BE-8000&page=2", result);
        Assert.Single(result.Split("page=").Skip(1)); // Slechts één page= parameter
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Duplicate URL dedup — verschillende locaties mogen niet dezelfde URL geven
    // voor page 1 én page 2 als template geen {page} heeft
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildPagedSearchUrl_ThreeLocations_TwoPages_SixUniqueUrls()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&isNewlyBuilt=true&orderBy=relevance";

        var locations = new[]
        {
            Loc("Beernem", "8730", "beernem"),
            Loc("Brugge",  "8000", "brugge"),
            Loc("Sint-Michiels", "8200", "sint-michiels"),
        };

        var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var loc in locations)
            for (var page = 1; page <= 2; page++)
                generated.Add(ImmowebCrawler.BuildPagedSearchUrl(template, loc, loc.CitySlug, page));

        // 3 locaties × 2 pagina's = 6 unieke URLs (geen duplicaten meer)
        Assert.Equal(6, generated.Count);
    }

    [Fact]
    public void BuildPagedSearchUrl_Page1And2_DifferentUrls()
    {
        // Het oorspronkelijke probleem: page 1 en page 2 gaven dezelfde URL
        const string template = "https://www.immoweb.be/nl/zoeken?postalCodes=BE-{postalCode}&isNewlyBuilt=true";
        var loc = Loc("Brugge", "8000");

        var page1 = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 1);
        var page2 = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "brugge", 2);

        Assert.NotEqual(page1, page2);
        Assert.Contains("page=2", page2);
        Assert.DoesNotContain("page=", page1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Locatie-placeholders invullen
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildPagedSearchUrl_AllPlaceholders_FilledCorrectly()
    {
        const string template = "https://www.immoweb.be/nl/zoeken/{transactionType}/{propertyType}/{citySlug}/{postalCode}?page={page}";
        var loc = Loc("Sint-Michiels", "8200", "sint-michiels");

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, loc, "sint-michiels", 3);

        Assert.Equal(
            "https://www.immoweb.be/nl/zoeken/te-koop/huis-en-appartement/sint-michiels/8200?page=3",
            result);
    }

    [Fact]
    public void BuildPagedSearchUrl_GlobalTemplate_Page1_NoPageParam()
    {
        // Globale template (geen locatie-placeholders)
        const string template = "https://www.immoweb.be/nl/zoeken?isNewlyBuilt=true";

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, null, null, 1);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?isNewlyBuilt=true", result);
    }

    [Fact]
    public void BuildPagedSearchUrl_GlobalTemplate_Page2_AppendsPageParam()
    {
        const string template = "https://www.immoweb.be/nl/zoeken?isNewlyBuilt=true";

        var result = ImmowebCrawler.BuildPagedSearchUrl(template, null, null, 2);

        Assert.Equal("https://www.immoweb.be/nl/zoeken?isNewlyBuilt=true&page=2", result);
    }
}
