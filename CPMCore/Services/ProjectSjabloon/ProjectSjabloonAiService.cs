using System.Text;
using System.Text.Json.Nodes;
using CPMCore.Services.PuntenSpraak;
using FacadeCore;
using Microsoft.Extensions.Options;
using ServiceCore.Planning;

namespace CPMCore.Services.ProjectSjabloon
{
    /// <summary>
    /// Kiest uit een vrije beschrijving van een gebouw het projectsjabloon dat past en haalt de antwoorden eruit die dat sjabloon nodig heeft
    /// (bouwlagen, kelder, dak, terrassen of luifels, één planning of per woning), met de aannames en vragen erbij.
    /// De AI doet enkel een voorstel: wat hij kan zien (<see cref="ProjectSjabloonAiContext"/>) is anoniem, en zijn antwoord wordt door
    /// <see cref="ProjectSjabloonAiVerwerker"/> gecontroleerd (enkel sjablonen uit de lijst, grenzen, bekende waarden). De projectleider bevestigt voor er iets gebeurt.
    /// Hergebruikt de Anthropic-sleutel en het model van "Punten inspreken" (sectie PuntenSpraak), die op haar beurt op de sleutel van "Offerte inlezen" terugvalt.
    /// </summary>
    public class ProjectSjabloonAiService : IProjectSjabloonAiService
    {
        private const string MessagesUrl = "https://api.anthropic.com/v1/messages";

        private readonly PuntenSpraakOptions _o;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ProjectSjabloonAiService> _log;

        public ProjectSjabloonAiService(IOptions<PuntenSpraakOptions> options, IOptions<QuoteExtraction.QuoteExtractionOptions> quote, IHttpClientFactory http, ILogger<ProjectSjabloonAiService> log)
        {
            _o = options.Value;
            if (string.IsNullOrWhiteSpace(_o.AnthropicApiKey)) _o.AnthropicApiKey = quote.Value.AnthropicApiKey;
            _http = http; _log = log;
        }

        public async Task<ProjectSjabloonAiVoorstel> Voorstel(string beschrijving, ProjectSjabloonAiContext? context = null, CancellationToken ct = default)
        {
            var res = new ProjectSjabloonAiVoorstel();
            if (string.IsNullOrWhiteSpace(beschrijving)) { res.Fout = "Beschrijf eerst het gebouw."; return res; }
            if (beschrijving.Length > 2000) beschrijving = beschrijving[..2000];
            if (string.IsNullOrWhiteSpace(_o.AnthropicApiKey))
            {
                res.Fout = "De AI is nog niet ingesteld (de Anthropic-sleutel ontbreekt). Vul de antwoorden zelf in.";
                return res;
            }

            var request = new JsonObject
            {
                ["model"] = _o.AnthropicModel,
                ["max_tokens"] = 1500,
                ["system"] = ProjectSjabloonAiVerwerker.Systeemprompt,
                ["tools"] = new JsonArray(new JsonObject { ["name"] = "antwoorden", ["description"] = "Het gekozen sjabloon en de antwoorden over het gebouw.", ["input_schema"] = JsonNode.Parse(ProjectSjabloonAiVerwerker.Schema) }),
                ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = "antwoorden" },
                ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = ProjectSjabloonAiVerwerker.BouwBericht(beschrijving, context) }),
            };
            try
            {
                using var client = _http.CreateClient(PuntenSpraakService.HttpClientName);
                client.Timeout = TimeSpan.FromSeconds(Math.Min(_o.TimeoutSeconds, 90));
                using var req = new HttpRequestMessage(HttpMethod.Post, MessagesUrl) { Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json") };
                req.Headers.Add("x-api-key", _o.AnthropicApiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                using var http = await client.SendAsync(req, ct);
                var body = await http.Content.ReadAsStringAsync(ct);
                if (!http.IsSuccessStatusCode)
                {
                    _log.LogWarning("Projectsjabloon AI: HTTP {Status}: {Body}", (int)http.StatusCode, body.Length > 300 ? body[..300] : body);
                    res.Fout = http.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                        ? "De AI is nu niet beschikbaar (te veel aanvragen of het tegoed is op). Vul de antwoorden zelf in."
                        : "De AI antwoordde niet zoals verwacht. Vul de antwoorden zelf in.";
                    return res;
                }
                var content = JsonNode.Parse(body)?["content"]?.AsArray();
                var input = content?.FirstOrDefault(c => c?["type"]?.GetValue<string>() == "tool_use")?["input"];
                if (input == null)
                {
                    var text = string.Join("\n", (content ?? new JsonArray()).Select(c => c?["text"]?.GetValue<string>()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
                    var i0 = text.IndexOf('{'); var i1 = text.LastIndexOf('}');
                    if (i0 >= 0 && i1 > i0) { try { input = JsonNode.Parse(text[i0..(i1 + 1)]); } catch { /* geen json */ } }
                }
                if (input == null) { res.Fout = "De AI gaf geen bruikbaar antwoord. Vul de antwoorden zelf in."; return res; }

                res = ProjectSjabloonAiVerwerker.Verwerk(input, context);
                _log.LogInformation("Projectsjabloon AI: sjabloon {Sjabloon}, {Verd} bouwlagen, kelder {Kelder}, dak {Dak}, {Terrassen} terrassen, model {Model}.",
                    res.SjabloonId, res.Antwoorden.Verdiepingen, res.Antwoorden.Kelder, res.Antwoorden.Dak, res.Antwoorden.Terrassen.Count, _o.AnthropicModel);
            }
            catch (TaskCanceledException) { res.Fout = "De AI deed er te lang over. Probeer het opnieuw of vul de antwoorden zelf in."; }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Projectsjabloon AI mislukt");
                res.Fout = "De AI kon niet bereikt worden. Vul de antwoorden zelf in.";
            }
            return res;
        }
    }
}
