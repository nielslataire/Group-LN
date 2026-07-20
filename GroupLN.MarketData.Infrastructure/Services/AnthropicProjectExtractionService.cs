using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class AnthropicProjectExtractionService : IAiProjectExtractionService
{
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private readonly MarketDataDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CrawlerSettings _settings;
    private readonly ILogger<AnthropicProjectExtractionService> _logger;

    private int _extractionsThisRun;

    public AnthropicProjectExtractionService(
        MarketDataDbContext db,
        IHttpClientFactory httpClientFactory,
        CrawlerSettings settings,
        ILogger<AnthropicProjectExtractionService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task<AiProjectExtractionResult?> ExtractAsync(
        AiProjectExtractionInput input,
        CancellationToken ct)
    {
        var ai = _settings.AiExtraction;
        if (!ai.EnableAiProjectExtraction) return null;

        var inputHash = ComputeInputHash(input);

        // Cache-check
        var cached = await _db.ProjectAiExtractionCaches
            .FirstOrDefaultAsync(c => c.InputHash == inputHash, ct);

        if (cached is not null)
        {
            _logger.LogInformation("[AI] CacheHit | ExternalId={Id} | InputHash={Hash}",
                input.ExternalId, inputHash[..8]);
            return MapCachedResult(cached);
        }

        // Limiet per run (0 = onbeperkt)
        if (ai.MaxAiExtractionsPerRun > 0 && _extractionsThisRun >= ai.MaxAiExtractionsPerRun)
        {
            _logger.LogInformation("[AI] MaxAiExtractionsPerRun ({Max}) bereikt — {Id} overgeslagen.",
                ai.MaxAiExtractionsPerRun, input.ExternalId);
            return null;
        }

        if (string.IsNullOrEmpty(ai.AnthropicApiKey))
        {
            _logger.LogWarning("[AI] AnthropicApiKey niet geconfigureerd.");
            return null;
        }

        // ── DEEL A: RequestPrepared ───────────────────────────────────────────
        _logger.LogInformation(
            "[AI] RequestPrepared | Source={Source} | ExternalId={Id} | Url={Url} | " +
            "RawTitle={RawTitle} | MetaTitle={MetaTitle} | OgTitle={OgTitle} | " +
            "Address={Address} | Developer={Dev} | BodyTextLength={BTL} | " +
            "UnitTableLength={UTL} | Model={Model} | InputHash={Hash}",
            input.SourceName, input.ExternalId, input.Url ?? "(none)",
            Trunc60(input.RawTitle), Trunc60(input.MetaTitle), Trunc60(input.OgTitle),
            Trunc60(input.Address), Trunc60(input.Developer),
            input.BodyText?.Length ?? 0, input.UnitTableText?.Length ?? 0,
            ai.AnthropicModel, inputHash[..8]);

        if (_settings.Debug.Enabled)
        {
            WriteAiDebugFile($"{input.SourceName}-{input.ExternalId}-request.json",
                new
                {
                    sourceName       = input.SourceName,
                    externalId       = input.ExternalId,
                    url              = input.Url,
                    rawTitle         = input.RawTitle,
                    metaTitle        = input.MetaTitle,
                    ogTitle          = input.OgTitle,
                    address          = input.Address,
                    developer        = input.Developer,
                    bodyTextPreview  = input.BodyText is { Length: > 0 } bt
                        ? bt[..Math.Min(5000, bt.Length)] : null,
                    unitTablePreview = input.UnitTableText is { Length: > 0 } ut
                        ? ut[..Math.Min(5000, ut.Length)] : null,
                    model            = ai.AnthropicModel,
                    inputHash
                });
        }

        try
        {
            var json = await CallAnthropicAsync(input, ai, ct);
            if (json is null) return null;

            var result = ParseAnthropicResponse(json, input.ExternalId, input.SourceName);
            if (result is null) return null;

            // Cache opslaan
            var now = DateTime.UtcNow;
            _db.ProjectAiExtractionCaches.Add(new ProjectAiExtractionCache
            {
                SourceName             = input.SourceName,
                ExternalId             = input.ExternalId,
                Url                    = input.Url,
                InputHash              = inputHash,
                Model                  = ai.AnthropicModel,
                RawTitle               = input.RawTitle,
                ExtractedProjectName   = result.ProjectName,
                ProjectNameConfidence  = result.ProjectNameConfidence,
                IsMarketingTitle       = result.IsMarketingTitle,
                ExtractedStreet        = result.Street,
                ExtractedHouseNumber   = result.HouseNumber,
                ExtractedPostalCode    = result.PostalCode,
                ExtractedCity          = result.City,
                ExtractedDeveloper     = result.Developer,
                ExtractedNumberOfUnits = result.NumberOfUnits,
                ExtractedJson          = result.ExtractedJson,
                CreatedAt              = now,
                UpdatedAt              = now
            });
            await _db.SaveChangesAsync(ct);

            _extractionsThisRun++;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[AI] AiExtractionFailed voor {ExternalId}: {Err}", input.ExternalId, ex.Message);
            return null;
        }
    }

    // ── API call ──────────────────────────────────────────────────────────────

    private async Task<string?> CallAnthropicAsync(
        AiProjectExtractionInput input,
        AiExtractionSettings ai,
        CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient("AnthropicClient");
        client.Timeout = TimeSpan.FromSeconds(ai.AiExtractionTimeoutSeconds);

        var requestBody = new
        {
            model      = ai.AnthropicModel,
            system     = BuildSystemPrompt(),
            max_tokens = 1024,
            messages   = new[] { new { role = "user", content = BuildUserContent(input) } }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl) { Content = content };
        request.Headers.Add("x-api-key", ai.AnthropicApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        using var response = await client.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        // ── DEEL B: ResponseReceived ──────────────────────────────────────────
        string? stopReason = null;
        var inputTokens  = 0;
        var outputTokens = 0;
        try
        {
            var root = JsonNode.Parse(responseBody);
            stopReason   = root?["stop_reason"]?.GetValue<string>();
            inputTokens  = root?["usage"]?["input_tokens"]?.GetValue<int>()  ?? 0;
            outputTokens = root?["usage"]?["output_tokens"]?.GetValue<int>() ?? 0;
        }
        catch { /* metadata parse is best-effort */ }

        _logger.LogInformation(
            "[AI] ResponseReceived | ExternalId={Id} | HttpStatus={Status} | " +
            "ResponseLength={Len} | StopReason={SR} | InputTokens={IT} | OutputTokens={OT}",
            input.ExternalId, (int)response.StatusCode, responseBody.Length,
            stopReason ?? "?", inputTokens, outputTokens);

        if (_settings.Debug.Enabled)
            WriteAiDebugFile($"{input.SourceName}-{input.ExternalId}-response.json", responseBody);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[AI] HTTP {Status} van Anthropic voor {Id}.",
                (int)response.StatusCode, input.ExternalId);
            return null;
        }

        return responseBody;
    }

    private static string BuildSystemPrompt() => """
        Je bent een gespecialiseerde AI voor Belgische nieuwbouwprojecten.

        Je taak is om uit ALLE beschikbare gegevens de commerciële projectnaam en de belangrijkste projectgegevens van een nieuwbouwproject te extraheren.

        Gebruik hiervoor alle informatie die je krijgt, waaronder (indien beschikbaar):

        - RawTitle
        - MetaTitle
        - OpenGraphTitle
        - H1
        - H2
        - H3
        - volledige bodytekst
        - projectbeschrijving
        - unit tabel
        - adres
        - ontwikkelaar
        - meta description
        - structured data (JSON-LD)
        - alle overige tekst op de pagina

        Gebruik NOOIT slechts één veld wanneer andere informatie beschikbaar is.

        ==========================================================
        PROJECTNAAM
        ==========================================================

        Zoek de commerciële naam waarmee het project door de ontwikkelaar of makelaar wordt aangeboden.

        Dat is NIET noodzakelijk een expliciet voorafgegaan woord zoals:

        - Residentie
        - Woonproject
        - Nieuwbouwproject
        - Project

        Deze woorden mogen voorkomen maar zijn geen vereiste.

        De projectnaam mag dus perfect zijn:

        Weylerhof
        Karmel
        Linum Park
        Villa Cauxyde
        De Kroon
        De Berk
        't Molenhof
        Hof ter Linden

        Wanneer de titel bijvoorbeeld "Residentie Weylerhof" is,
        geef als projectnaam:

        "Weylerhof"

        Wanneer de titel "Project Karmel" is,
        geef:

        "Karmel"

        Wanneer de titel gewoon "Weylerhof" is,
        geef:

        "Weylerhof"

        Wanneer de titel "Villa Cauxyde" is,
        geef:

        "Villa Cauxyde"

        ==========================================================
        GEEN PROJECTNAAM
        ==========================================================

        Gebruik NOOIT als projectnaam:

        - straatnamen
        - gemeenten
        - postcodes
        - woningtypes
        - marketingzinnen
        - prijszinnen

        Voorbeelden:

        "Vier eigentijdse woningen"

        "Appartementen vanaf € ..."

        "Nieuwbouw te koop"

        "Theresianenstraat 17"

        "Brugge"

        "Appartement"

        Dit zijn GEEN projectnamen.

        ==========================================================
        BELANGRIJK
        ==========================================================

        Wanneer meerdere bronnen elkaar tegenspreken:

        Gebruik de naam die:

        1. het vaakst voorkomt;
        2. het meest duidelijk als commerciële naam gebruikt wordt;
        3. door een normale koper als projectnaam herkend zou worden.

        De projectnaam hoeft dus NIET letterlijk voorafgegaan te worden door "Residentie" of "Project".

        ==========================================================
        CONFIDENCE
        ==========================================================

        95-100
        De projectnaam staat duidelijk op meerdere plaatsen.

        80-94
        De projectnaam staat duidelijk minstens één keer vermeld.

        60-79
        Sterke aanwijzingen maar niet volledig zeker.

        0
        Geen commerciële projectnaam gevonden.

        ==========================================================
        KIES CORRECTE NAAM
        ==========================================================

        Kies liever een correcte commerciële projectnaam dan null.

        Gebruik uitsluitend null wanneer de volledige pagina enkel een adres of generieke marketingtekst bevat en er werkelijk geen commerciële projectnaam kan worden afgeleid.

        ==========================================================
        OUTPUT
        ==========================================================

        Geef uitsluitend geldige JSON terug.
        Geen markdown.
        Geen uitleg.
        Geen extra tekst.

        {
          "projectName": "commerciële projectnaam of null",
          "projectNameConfidence": 0-100,
          "isMarketingTitle": true/false,
          "street": "straatnaam of null",
          "houseNumber": "huisnummer of null",
          "postalCode": "postcode of null",
          "city": "gemeente of null",
          "developer": "naam promotor of null",
          "numberOfUnits": geheel getal of null,
          "reasoningShort": "max 1 zin uitleg"
        }
        """;

    private static string BuildUserContent(AiProjectExtractionInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyseer de volgende gegevens:");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(input.RawTitle))        sb.AppendLine($"RawTitle: {input.RawTitle}");
        if (!string.IsNullOrEmpty(input.OgTitle))         sb.AppendLine($"OpenGraphTitle: {input.OgTitle}");
        if (!string.IsNullOrEmpty(input.MetaTitle))       sb.AppendLine($"MetaTitle: {input.MetaTitle}");
        if (!string.IsNullOrEmpty(input.MetaDescription)) sb.AppendLine($"MetaDescription: {input.MetaDescription}");
        if (!string.IsNullOrEmpty(input.H1))              sb.AppendLine($"H1: {input.H1}");
        if (!string.IsNullOrEmpty(input.H2))              sb.AppendLine($"H2: {input.H2}");
        if (!string.IsNullOrEmpty(input.H3))              sb.AppendLine($"H3: {input.H3}");
        if (!string.IsNullOrEmpty(input.Address))         sb.AppendLine($"Address: {input.Address}");
        if (!string.IsNullOrEmpty(input.Developer))       sb.AppendLine($"Developer: {input.Developer}");
        if (!string.IsNullOrEmpty(input.UnitTableText))
            sb.AppendLine($"UnitTabel: {input.UnitTableText[..Math.Min(2000, input.UnitTableText.Length)]}");
        if (!string.IsNullOrEmpty(input.StructuredData))
            sb.AppendLine($"StructuredData (JSON-LD): {input.StructuredData[..Math.Min(2000, input.StructuredData.Length)]}");
        if (!string.IsNullOrEmpty(input.BodyText))
            sb.AppendLine($"BodyText: {input.BodyText[..Math.Min(3000, input.BodyText.Length)]}");
        return sb.ToString();
    }

    // ── Parsing ───────────────────────────────────────────────────────────────

    private AiProjectExtractionResult? ParseAnthropicResponse(
        string rawResponse,
        string externalId,
        string sourceName)
    {
        try
        {
            var root = JsonNode.Parse(rawResponse);
            var text = root?["content"]?[0]?["text"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Verwijder eventuele markdown code-fences
            text = text.Trim();
            if (text.StartsWith("```")) text = text.Split('\n', 2)[1];
            if (text.EndsWith("```")) text = text[..text.LastIndexOf("```")];
            text = text.Trim();

            // ── DEEL C: JsonReceived ──────────────────────────────────────────
            _logger.LogInformation(
                "[AI] JsonReceived | ExternalId={Id} | Json={Json}",
                externalId,
                text.Length > 500 ? text[..500] + "…" : text);

            JsonNode? json;
            try
            {
                json = JsonNode.Parse(text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "[AI] JsonParseFailed | ExternalId={Id} | Error={Err} | Json={Json}",
                    externalId, ex.Message,
                    text.Length > 300 ? text[..300] + "…" : text);

                if (_settings.Debug.Enabled)
                    WriteAiDebugFile($"{sourceName}-{externalId}-jsonparse-failed.json", text);

                return null;
            }
            if (json is null) return null;

            var result = new AiProjectExtractionResult
            {
                ProjectName           = json["projectName"]?.GetValue<string>(),
                ProjectNameConfidence = json["projectNameConfidence"]?.GetValue<int>() ?? 0,
                IsMarketingTitle      = json["isMarketingTitle"]?.GetValue<bool>() ?? false,
                Street                = json["street"]?.GetValue<string>(),
                HouseNumber           = json["houseNumber"]?.GetValue<string>(),
                PostalCode            = json["postalCode"]?.GetValue<string>(),
                City                  = json["city"]?.GetValue<string>(),
                Developer             = json["developer"]?.GetValue<string>(),
                NumberOfUnits         = json["numberOfUnits"]?.GetValue<int?>(),
                ExtractedJson         = text,
                FromCache             = false
            };

            // ── DEEL C: Parsed ────────────────────────────────────────────────
            _logger.LogInformation(
                "[AI] Parsed | ExternalId={Id} | ProjectName={Name} | Confidence={Conf} | " +
                "Street={Street} | HouseNumber={HN} | PostalCode={PC} | City={City} | " +
                "Developer={Dev} | NumberOfUnits={Units}",
                externalId,
                result.ProjectName ?? "(null)", result.ProjectNameConfidence,
                result.Street ?? "(none)", result.HouseNumber ?? "(none)",
                result.PostalCode ?? "(none)", result.City ?? "(none)",
                result.Developer ?? "(none)", result.NumberOfUnits?.ToString() ?? "(none)");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[AI] Parseerfout in Anthropic-antwoord: {Err}", ex.Message);
            return null;
        }
    }

    private static AiProjectExtractionResult MapCachedResult(ProjectAiExtractionCache c)
        => new()
        {
            ProjectName           = c.ExtractedProjectName,
            ProjectNameConfidence = c.ProjectNameConfidence,
            IsMarketingTitle      = c.IsMarketingTitle,
            Street                = c.ExtractedStreet,
            HouseNumber           = c.ExtractedHouseNumber,
            PostalCode            = c.ExtractedPostalCode,
            City                  = c.ExtractedCity,
            Developer             = c.ExtractedDeveloper,
            NumberOfUnits         = c.ExtractedNumberOfUnits,
            ExtractedJson         = c.ExtractedJson,
            FromCache             = true
        };

    // ── Hulpmethoden ─────────────────────────────────────────────────────────

    private void WriteAiDebugFile(string fileName, object data)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "debug", "ai-extraction");
            Directory.CreateDirectory(dir);
            var path    = Path.Combine(dir, fileName);
            var content = data is string s
                ? s
                : JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[AI] WriteAiDebugFile mislukt voor {File}: {Err}", fileName, ex.Message);
        }
    }

    private static string Trunc60(string? s)
        => s is null ? "(none)" : (s.Length > 60 ? s[..60] + "…" : s);

    internal static string ComputeInputHash(AiProjectExtractionInput input)
    {
        var combined = string.Join("|",
            input.SourceName, input.ExternalId,
            input.RawTitle ?? "", input.OgTitle ?? "", input.MetaTitle ?? "",
            input.MetaDescription ?? "",
            input.H1 ?? "", input.H2 ?? "", input.H3 ?? "",
            (input.BodyText ?? "")[..Math.Min(500, (input.BodyText ?? "").Length)]);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(combined)));
    }
}
