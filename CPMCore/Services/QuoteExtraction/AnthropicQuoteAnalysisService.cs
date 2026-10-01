using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.QuoteExtraction
{
    /// <summary>Offerte inlezen (20c) via de Anthropic Messages API, met de bijgesneden regio als
    /// afbeelding (vision). Lay-out-onafhankelijk — in tegenstelling tot
    /// <see cref="AzureQuoteAnalysisService"/> (prebuilt-layout + kolomheuristiek) begrijpt het model zelf
    /// dat specificatierijen onder een artikel bij dat artikel horen, dat "1388,87" en "1.00" in hetzelfde
    /// document kunnen staan, en dat "Bedrag" een regeltotaal is en geen eenheidsprijs. Zelfde HTTP-patroon
    /// als de MarketData-crawler (GroupLN.MarketData.Infrastructure.Services.AnthropicProjectExtractionService):
    /// raw HttpClient, geen SDK, antwoord = content[0].text als strikte JSON.</summary>
    public class AnthropicQuoteAnalysisService : IQuoteRegionAnalysisService
    {
        public const string HttpClientName = "AnthropicClient";
        private const string MessagesUrl = "https://api.anthropic.com/v1/messages";

        private readonly QuoteExtractionOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnthropicQuoteAnalysisService> _logger;

        public bool IsEnabled => _options.AnthropicConfigured;

        public AnthropicQuoteAnalysisService(IOptions<QuoteExtractionOptions> options, IHttpClientFactory httpClientFactory, ILogger<AnthropicQuoteAnalysisService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<QuoteRegionResult> AnalyzeTableAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return new QuoteRegionResult { ErrorMessage = "Automatisch inlezen is niet geconfigureerd (geen Anthropic-sleutel)." };

            try
            {
                var requestBody = new
                {
                    model = _options.AnthropicModel,
                    max_tokens = 4096,
                    system = SystemPrompt,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "image", source = new { type = "base64", media_type = "image/png", data = Convert.ToBase64String(imageBytes) } },
                                new { type = "text", text = "Lees alle artikelregels uit deze uitsnede van een leveranciersofferte en geef ze terug als JSON volgens de instructies." },
                            },
                        },
                    },
                };

                using var client = _httpClientFactory.CreateClient(HttpClientName);
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
                using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                };
                request.Headers.Add("x-api-key", _options.AnthropicApiKey);

                using var response = await client.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Anthropic offerte-extractie: HTTP {Status}: {Body}", (int)response.StatusCode, body.Length > 300 ? body[..300] : body);
                    return new QuoteRegionResult { ErrorMessage = $"AI-dienst antwoordde met HTTP {(int)response.StatusCode}." };
                }

                var text = JsonNode.Parse(body)?["content"]?[0]?["text"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(text)) return new QuoteRegionResult { Success = true };

                text = text.Trim();
                if (text.StartsWith("```")) text = text.Split('\n', 2)[1];
                if (text.EndsWith("```")) text = text[..text.LastIndexOf("```")];

                var json = JsonNode.Parse(text.Trim());
                var lines = new List<QuoteRegionLine>();
                foreach (var item in json?["lines"]?.AsArray() ?? new JsonArray())
                {
                    if (item is null) continue;
                    lines.Add(new QuoteRegionLine
                    {
                        Description = item["description"]?.GetValue<string>(),
                        Unit = item["unit"]?.GetValue<string>(),
                        Number = ReadDecimal(item["quantity"]) ?? 1m,
                        Price = ReadDecimal(item["unitPrice"]),
                        NeedsReview = item["needsReview"]?.GetValue<bool>() ?? false,
                    });
                }

                _logger.LogInformation("Anthropic offerte-extractie: {Count} regels, model {Model}.", lines.Count, _options.AnthropicModel);
                return new QuoteRegionResult { Success = true, Lines = lines };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Anthropic offerte-extractie mislukt.");
                return new QuoteRegionResult { ErrorMessage = ex.Message };
            }
        }

        /// <summary>Het model krijgt de opdracht getallen al als JSON-number terug te geven, maar wees
        /// tolerant voor een string met komma — dan valt de Belgische notatie terug op dezelfde parser-regels.</summary>
        private static decimal? ReadDecimal(JsonNode node)
        {
            if (node is null) return null;
            try
            {
                if (node is JsonValue v && v.TryGetValue<decimal>(out var d)) return d;
                var s = node.ToString().Replace("€", "").Replace(" ", "").Trim();
                if (s.Contains(',') && !s.Contains('.')) s = s.Replace(',', '.');
                else if (s.Contains(',') && s.Contains('.')) s = s.LastIndexOf(',') > s.LastIndexOf('.') ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
                return decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var r) ? r : null;
            }
            catch { return null; }
        }

        private const string SystemPrompt = """
            Je leest een uitsnede (afbeelding) van een offerte van een Belgische/Nederlandse leverancier of aannemer voor een bouwproject.
            Haal er de ARTIKELREGELS uit: wat wordt er geleverd, hoeveel, in welke eenheid en tegen welke eenheidsprijs excl. btw.

            REGELS
            - Eén JSON-object per artikelregel. Een artikelregel heeft een eenheidsprijs.
            - Rijen ZONDER prijs die onder een artikel staan (specificaties zoals kleur, afmetingen, motortype, referentie, "Totale breedte 1840") zijn GEEN eigen regels: ze horen in de omschrijving van het artikel erboven.
            - Bewaar daarbij de STRUCTUUR van de omschrijving: elke specificatie/subregel op een eigen regel (gebruik "\n" in de JSON-string), en neem nummeringen, letters en opsommingstekens over zoals ze in de offerte staan ("1.", "a)", "-", "•"). Een kenmerk met een waarde schrijf je als "Kenmerk: waarde" (bv. "Totale breedte (mm): 1840"). Eerste regel = de artikelnaam/referentie zelf.
            - "unitPrice" is de prijs per eenheid (kolom zoals Prijs/St., Eenheidsprijs, E.P.). Een kolom "Bedrag"/"Totaal" is het regeltotaal; gebruik die enkel om unitPrice af te leiden (bedrag ÷ aantal) als er geen eenheidsprijs-kolom is.
            - "quantity": het aantal; ontbreekt het, gebruik 1.
            - "unit": de eenheid zoals vermeld (st, stuk, m, m², m³, lm, u, forfait, sog …), anders null.
            - Getallen in Belgische notatie: "1388,87" = 1388.87, "1.250,00" = 1250.00, "1.00" = 1. Geef getallen als JSON-numbers, niet als tekst.
            - Sla totaalrijen, btw-rijen, subtotalen, kortingen en voorwaardentekst over.
            - Zet "needsReview" op true als je over de omschrijving, het aantal of de prijs van die regel twijfelt (onleesbaar, dubbelzinnig, afgeleid i.p.v. gelezen). Anders false.
            - Staat er geen enkele artikelregel in de uitsnede, geef een lege lijst.

            OUTPUT
            Geef uitsluitend geldige JSON terug. Geen markdown, geen uitleg, geen extra tekst.
            {
              "lines": [
                { "description": "tekst", "unit": "st" of null, "quantity": 1, "unitPrice": 1388.87, "needsReview": false }
              ]
            }
            """;
    }
}
