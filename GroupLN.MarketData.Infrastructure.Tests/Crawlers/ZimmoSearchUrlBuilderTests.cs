using GroupLN.MarketData.Infrastructure.Crawlers;
using Xunit;
using Xunit.Abstractions;

namespace GroupLN.MarketData.Infrastructure.Tests.Crawlers;

/// <summary>
/// Valideert dat ZimmoSearchUrlBuilder de exacte URLs reproduceert die Zimmo verwacht.
/// Referentie-URLs zijn manueel vastgesteld via de Zimmo zoekopdracht-UI.
/// </summary>
public class ZimmoSearchUrlBuilderTests
{
    private readonly ITestOutputHelper _output;

    public ZimmoSearchUrlBuilderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── Referentie-URLs: ALL-modus (standaard, nieuw) ─────────────────────────

    // Filter: status + category(HOUSE,APARTMENT) + newConstruction(ALL) — zonder locatie
    private const string UrlAllModeAlgemeen =
        "https://www.zimmo.be/nl/zoeken/?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJjYXRlZ29yeSI6eyJpbiI6WyJIT1VTRSIsIkFQQVJUTUVOVCJdfSwibmV3Q29uc3RydWN0aW9uIjp7ImVxIjoiQUxMIn19fQ%3D%3D#gallery";

    // ── Referentie-URLs: Projects-modus (legacy) ──────────────────────────────

    private const string UrlProjectenAlgemeen =
        "https://www.zimmo.be/nl/zoeken/?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJuZXdDb25zdHJ1Y3Rpb24iOnsiZXEiOiJQUk9KRUNUUyJ9fX0%3D#gallery";

    private const string UrlProjectenBrugge8000 =
        "https://www.zimmo.be/nl/zoeken/?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJuZXdDb25zdHJ1Y3Rpb24iOnsiZXEiOiJQUk9KRUNUUyJ9LCJwbGFjZUlkIjp7ImluIjpbMTExM119fX0%3D#gallery";

    private const string UrlProjectenSintMichiels8200 =
        "https://www.zimmo.be/nl/zoeken/?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJuZXdDb25zdHJ1Y3Rpb24iOnsiZXEiOiJQUk9KRUNUUyJ9LCJwbGFjZUlkIjp7ImluIjpbMTExOV19fX0%3D#gallery";

    private const string UrlProjectenBeernem8730 =
        "https://www.zimmo.be/nl/zoeken/?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJuZXdDb25zdHJ1Y3Rpb24iOnsiZXEiOiJQUk9KRUNUUyJ9LCJwbGFjZUlkIjp7ImluIjpbMTEwOF19fX0%3D#gallery";

    // ── JSON-structuur: ALL-modus ─────────────────────────────────────────────

    [Fact]
    public void BuildJson_AllMode_ZonderLocatie_BevatCategoryEnAll()
    {
        var json = ZimmoSearchUrlBuilder.BuildJson(
            null, ZimmoNewConstructionMode.All,
            ["FOR_SALE", "TAKE_OVER"], ["HOUSE", "APARTMENT"]);

        _output.WriteLine($"JSON (ALL, geen locatie): {json}");

        Assert.Equal(
            """{"filter":{"status":{"in":["FOR_SALE","TAKE_OVER"]},"category":{"in":["HOUSE","APARTMENT"]},"newConstruction":{"eq":"ALL"}}}""",
            json);
    }

    [Fact]
    public void BuildJson_AllMode_MetPlaceId_BevatPlaceIdAlsLaatste()
    {
        var json = ZimmoSearchUrlBuilder.BuildJson(
            1113, ZimmoNewConstructionMode.All,
            ["FOR_SALE", "TAKE_OVER"], ["HOUSE", "APARTMENT"]);

        _output.WriteLine($"JSON (ALL, Brugge 1113): {json}");

        Assert.Equal(
            """{"filter":{"status":{"in":["FOR_SALE","TAKE_OVER"]},"category":{"in":["HOUSE","APARTMENT"]},"newConstruction":{"eq":"ALL"},"placeId":{"in":[1113]}}}""",
            json);
    }

    [Theory]
    [InlineData(1113)]
    [InlineData(1119)]
    [InlineData(1108)]
    public void BuildJson_AllMode_AlleLocaties_BevatVerwachteCategorieën(int placeId)
    {
        var json = ZimmoSearchUrlBuilder.BuildJson(
            placeId, ZimmoNewConstructionMode.All,
            ["FOR_SALE", "TAKE_OVER"], ["HOUSE", "APARTMENT"]);

        _output.WriteLine($"JSON (ALL, placeId={placeId}): {json}");

        Assert.Contains("\"HOUSE\"", json);
        Assert.Contains("\"APARTMENT\"", json);
        Assert.Contains("\"ALL\"", json);
        Assert.Contains($"{placeId}", json);
        Assert.DoesNotContain("PROJECTS", json);
    }

    // ── JSON-structuur: Projects-modus (legacy) ───────────────────────────────

    [Fact]
    public void BuildJson_ProjectsMode_ZonderLocatie_GeenCategoryVeld()
    {
        var json = ZimmoSearchUrlBuilder.BuildJson(
            null, ZimmoNewConstructionMode.Projects,
            ["FOR_SALE", "TAKE_OVER"], []);

        _output.WriteLine($"JSON (PROJECTS, geen locatie): {json}");

        Assert.Equal(
            """{"filter":{"status":{"in":["FOR_SALE","TAKE_OVER"]},"newConstruction":{"eq":"PROJECTS"}}}""",
            json);
        Assert.DoesNotContain("category", json);
    }

    [Theory]
    [InlineData(1113, """{"filter":{"status":{"in":["FOR_SALE","TAKE_OVER"]},"newConstruction":{"eq":"PROJECTS"},"placeId":{"in":[1113]}}}""")]
    [InlineData(1119, """{"filter":{"status":{"in":["FOR_SALE","TAKE_OVER"]},"newConstruction":{"eq":"PROJECTS"},"placeId":{"in":[1119]}}}""")]
    [InlineData(1108, """{"filter":{"status":{"in":["FOR_SALE","TAKE_OVER"]},"newConstruction":{"eq":"PROJECTS"},"placeId":{"in":[1108]}}}""")]
    public void BuildJson_ProjectsMode_MetLocatie_ProduceerCorrecteJson(int placeId, string expected)
    {
        var json = ZimmoSearchUrlBuilder.BuildJson(
            placeId, ZimmoNewConstructionMode.Projects,
            ["FOR_SALE", "TAKE_OVER"], []);

        _output.WriteLine($"JSON (PROJECTS, placeId={placeId}): {json}");

        Assert.Equal(expected, json);
    }

    // ── URL-reproductie: ALL-modus (standaard) ────────────────────────────────

    [Fact]
    public void Build_AllModeDefault_ZonderLocatie_ReproduceerReferentieUrl()
    {
        // Build() zonder argumenten moet ALL-modus gebruiken
        var url = ZimmoSearchUrlBuilder.Build();

        _output.WriteLine($"URL (default/ALL, geen locatie): {url}");
        _output.WriteLine($"Verwacht:                        {UrlAllModeAlgemeen}");

        Assert.Equal(UrlAllModeAlgemeen, url);
    }

    [Fact]
    public void Build_AllModeExpliciet_ZonderLocatie_ReproduceerReferentieUrl()
    {
        var url = ZimmoSearchUrlBuilder.Build(
            placeId: null,
            newConstruction: ZimmoNewConstructionMode.All);

        _output.WriteLine($"URL (ALL expliciet): {url}");

        Assert.Equal(UrlAllModeAlgemeen, url);
    }

    [Fact]
    public void Build_AllMode_MetBrugge_BevatPlaceIdInUrl()
    {
        var url = ZimmoSearchUrlBuilder.Build(
            placeId: 1113,
            newConstruction: ZimmoNewConstructionMode.All);

        _output.WriteLine($"URL (ALL, Brugge 1113): {url}");

        var decoded = ZimmoSearchUrlBuilder.DecodeSearchParam(ExtractSearchParam(url));
        _output.WriteLine($"Decoded JSON: {decoded}");

        Assert.Contains("\"HOUSE\"", decoded);
        Assert.Contains("\"APARTMENT\"", decoded);
        Assert.Contains("\"ALL\"", decoded);
        Assert.Contains("1113", decoded);
    }

    // ── URL-reproductie: Projects-modus (legacy) ──────────────────────────────

    [Fact]
    public void Build_ProjectsMode_ZonderLocatie_ReproduceerLegacyUrl()
    {
        var url = ZimmoSearchUrlBuilder.Build(
            placeId: null,
            newConstruction: ZimmoNewConstructionMode.Projects);

        _output.WriteLine($"URL (PROJECTS, geen locatie): {url}");
        _output.WriteLine($"Verwacht:                     {UrlProjectenAlgemeen}");

        Assert.Equal(UrlProjectenAlgemeen, url);
    }

    [Fact]
    public void Build_ProjectsMode_Brugge8000_ReproduceerLegacyUrl()
    {
        var url = ZimmoSearchUrlBuilder.Build(
            placeId: 1113,
            newConstruction: ZimmoNewConstructionMode.Projects);

        _output.WriteLine($"URL (PROJECTS, Brugge): {url}");

        Assert.Equal(UrlProjectenBrugge8000, url);
    }

    [Theory]
    [InlineData(1113, UrlProjectenBrugge8000)]
    [InlineData(1119, UrlProjectenSintMichiels8200)]
    [InlineData(1108, UrlProjectenBeernem8730)]
    public void Build_ProjectsMode_AlleLocaties_ReproduceerLegacyUrls(int placeId, string expected)
    {
        var url = ZimmoSearchUrlBuilder.Build(placeId, ZimmoNewConstructionMode.Projects);
        _output.WriteLine($"URL (PROJECTS, placeId={placeId}): {url}");

        Assert.Equal(expected, url);
    }

    // ── Postcode-mapping ──────────────────────────────────────────────────────

    [Fact]
    public void BuildForPostalCode_DefaultAllMode_BevatCategoryEnAll()
    {
        var url = ZimmoSearchUrlBuilder.BuildForPostalCode("8000");
        var decoded = ZimmoSearchUrlBuilder.DecodeSearchParam(ExtractSearchParam(url));

        _output.WriteLine($"URL (8000, default): {url}");
        _output.WriteLine($"Decoded:             {decoded}");

        Assert.Contains("\"HOUSE\"", decoded);
        Assert.Contains("\"APARTMENT\"", decoded);
        Assert.Contains("\"ALL\"", decoded);
        Assert.Contains("1113", decoded);
    }

    [Theory]
    [InlineData("8000", 1113)]
    [InlineData("8200", 1119)]
    [InlineData("8730", 1108)]
    public void BuildForPostalCode_AllMode_AlleBekendePostcodes_BevatJuisteLocatie(string postcode, int verwachtePlaceId)
    {
        var url     = ZimmoSearchUrlBuilder.BuildForPostalCode(postcode);
        var decoded = ZimmoSearchUrlBuilder.DecodeSearchParam(ExtractSearchParam(url));

        _output.WriteLine($"URL ({postcode}): {url}");
        _output.WriteLine($"Decoded: {decoded}");

        Assert.Contains($"{verwachtePlaceId}", decoded);
        Assert.Contains("\"ALL\"", decoded);
    }

    [Theory]
    [InlineData("8000", UrlProjectenBrugge8000)]
    [InlineData("8200", UrlProjectenSintMichiels8200)]
    [InlineData("8730", UrlProjectenBeernem8730)]
    public void BuildForPostalCode_ProjectsMode_ReproduceerLegacyUrls(string postcode, string expected)
    {
        var url = ZimmoSearchUrlBuilder.BuildForPostalCode(postcode, ZimmoNewConstructionMode.Projects);
        _output.WriteLine($"URL ({postcode}, PROJECTS): {url}");

        Assert.Equal(expected, url);
    }

    [Fact]
    public void BuildForPostalCode_OnbekendePostcode_GooidKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            ZimmoSearchUrlBuilder.BuildForPostalCode("9000")); // Gent = niet in mapping
    }

    // ── Decode / round-trip ───────────────────────────────────────────────────

    [Fact]
    public void DecodeSearchParam_AllModeReferentie_ToonDecodedJson()
    {
        var param   = ExtractSearchParam(UrlAllModeAlgemeen);
        var decoded = ZimmoSearchUrlBuilder.DecodeSearchParam(param);

        _output.WriteLine($"Decoded JSON (ALL): {decoded}");

        Assert.Contains("\"FOR_SALE\"", decoded);
        Assert.Contains("\"TAKE_OVER\"", decoded);
        Assert.Contains("\"HOUSE\"",     decoded);
        Assert.Contains("\"APARTMENT\"", decoded);
        Assert.Contains("\"ALL\"",       decoded);
        Assert.DoesNotContain("placeId", decoded);
        Assert.DoesNotContain("PROJECTS", decoded);
    }

    [Fact]
    public void DecodeSearchParam_AlleReferentieUrls_ToonDecodedJson()
    {
        var testCases = new[]
        {
            ("ALL geen locatie",   ExtractSearchParam(UrlAllModeAlgemeen)),
            ("PROJECTS algemeen",  ExtractSearchParam(UrlProjectenAlgemeen)),
            ("PROJECTS Brugge",    ExtractSearchParam(UrlProjectenBrugge8000)),
            ("PROJECTS S-Michiels",ExtractSearchParam(UrlProjectenSintMichiels8200)),
            ("PROJECTS Beernem",   ExtractSearchParam(UrlProjectenBeernem8730)),
        };

        foreach (var (label, param) in testCases)
        {
            var decoded = ZimmoSearchUrlBuilder.DecodeSearchParam(param);
            _output.WriteLine($"[{label}] {decoded}");
            Assert.Contains("FOR_SALE", decoded);
        }
    }

    [Fact]
    public void RoundTrip_AllMode_BuildDanDecode_GeeftOrigineleJson()
    {
        var original = ZimmoSearchUrlBuilder.BuildJson(
            1113, ZimmoNewConstructionMode.All,
            ["FOR_SALE", "TAKE_OVER"], ["HOUSE", "APARTMENT"]);

        var url     = ZimmoSearchUrlBuilder.Build(1113, ZimmoNewConstructionMode.All);
        var decoded = ZimmoSearchUrlBuilder.DecodeSearchParam(ExtractSearchParam(url));

        _output.WriteLine($"Origineel: {original}");
        _output.WriteLine($"Na decode: {decoded}");

        Assert.Equal(original, decoded);
    }

    // ── URL-structuur ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_AlleUrls_EindigenOpGalleryFragment()
    {
        Assert.EndsWith("#gallery", ZimmoSearchUrlBuilder.Build());
        Assert.EndsWith("#gallery", ZimmoSearchUrlBuilder.Build(1113));
        Assert.EndsWith("#gallery", ZimmoSearchUrlBuilder.Build(1113, ZimmoNewConstructionMode.Projects));
        Assert.EndsWith("#gallery", ZimmoSearchUrlBuilder.BuildForPostalCode("8000"));
    }

    [Fact]
    public void Build_AlleUrls_BevatSearchQueryParamEnCorrectBase()
    {
        var url = ZimmoSearchUrlBuilder.Build(1113);

        Assert.StartsWith("https://www.zimmo.be/nl/zoeken/", url);
        Assert.Contains("?search=", url);
    }

    // ── Hulpmethode ───────────────────────────────────────────────────────────

    private static string ExtractSearchParam(string url)
    {
        var start = url.IndexOf("?search=") + "?search=".Length;
        var end   = url.IndexOf('#');
        return url[start..end];
    }
}
