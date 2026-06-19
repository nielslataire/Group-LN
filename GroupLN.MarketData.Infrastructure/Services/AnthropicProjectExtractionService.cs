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

        // Limiet per run
        if (_extractionsThisRun >= ai.MaxAiExtractionsPerRun)
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
            max_tokens = 1024,
            messages   = new[] { new { role = "user", content = BuildPrompt(input) } }
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

    private static string BuildPrompt(AiProjectExtractionInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyseer de volgende gegevens van een Belgisch nieuwbouwproject en geef UITSLUITEND geldig JSON terug (geen markdown, geen extra tekst).");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(input.RawTitle))        sb.AppendLine($"RawTitle: {input.RawTitle}");
        if (!string.IsNullOrEmpty(input.OgTitle))         sb.AppendLine($"OgTitle: {input.OgTitle}");
        if (!string.IsNullOrEmpty(input.MetaTitle))       sb.AppendLine($"MetaTitle: {input.MetaTitle}");
        if (!string.IsNullOrEmpty(input.MetaDescription)) sb.AppendLine($"MetaDescription: {input.MetaDescription}");
        if (!string.IsNullOrEmpty(input.Address))         sb.AppendLine($"Address: {input.Address}");
        if (!string.IsNullOrEmpty(input.Developer))       sb.AppendLine($"Developer: {input.Developer}");
        if (!string.IsNullOrEmpty(input.BodyText))
            sb.AppendLine($"BodyText (eerste 1000 tekens): {input.BodyText[..Math.Min(1000, input.BodyText.Length)]}");

        sb.AppendLine();
        sb.AppendLine("""
            PRIORITEIT 1 – Expliciete projectnamen:
            Zoek EERST naar woorden/zinnen die expliciet een projectnaam benoemen, bijv.:
              "Residentie [Naam]", "het project [Naam]", "Woonproject [Naam]",
              "Nieuwbouwproject [Naam]", "Site [Naam]", "Domein [Naam]",
              "Verkaveling [Naam]", "Appartementscomplex [Naam]", "Gebouw [Naam]".
            Gebruik die naam letterlijk als projectName.

            PRIORITEIT 2 – Geen expliciete naam gevonden:
            Als er GEEN expliciete projectnaam in de tekst staat, geef dan "projectName": null terug.
            Verzin NOOIT een creatieve naam op basis van de locatie of beschrijving.
            Een zelf bedachte naam is altijd slechter dan null.

            Geef terug:
            {
              "projectName": "officiële naam van het project of null",
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
            """);

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
            (input.BodyText ?? "")[..Math.Min(500, (input.BodyText ?? "").Length)]);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(combined)));
    }
}
