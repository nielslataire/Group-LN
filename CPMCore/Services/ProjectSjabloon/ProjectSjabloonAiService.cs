using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using CPMCore.Services.PuntenSpraak;
using FacadeCore;
using Microsoft.Extensions.Options;

namespace CPMCore.Services.ProjectSjabloon
{
    /// <summary>
    /// Haalt uit een vrije beschrijving van een gebouw de antwoorden die een projectsjabloon nodig heeft (verdiepingen, kelder, dak, één planning of per woning).
    /// De AI doet enkel een voorstel: de uitkomst wordt gecontroleerd (grenzen, enkel bekende waarden) en de projectleider bevestigt voor er iets gebeurt.
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

        private const string SystemPrompt =
            "Je helpt een projectleider bij een bouwbedrijf in Vlaanderen. Hij beschrijft een gebouw in gewone taal en jij haalt eruit wat een planningssjabloon nodig heeft om zich aan dat gebouw aan te passen.\n" +
            "Regels:\n" +
            "- verdiepingen: het aantal bouwlagen waarvoor ruwbouw per verdieping herhaald wordt, een geheel getal van 1 tot 20. Het gelijkvloers telt mee als verdieping. Een woning met gelijkvloers en een verdieping is 2. Staat er niets over, gebruik dan 1.\n" +
            "- kelder: true bij een kelder of ondergrondse verdieping of parking, false als er duidelijk geen is, null als het niet gezegd wordt.\n" +
            "- dak: \"hellend\" (zadeldak, schilddak, lessenaarsdak met helling) of \"plat\" (platte of groendaken), null als het niet gezegd wordt of als er geen dak vermeld wordt.\n" +
            "- eenPlanningVoorAlles: true als het één gebouw met meerdere eenheden is (appartementsgebouw, residentie), false bij losse woningen (rijwoningen, een verkaveling).\n" +
            "- toelichting: één of twee korte zinnen in het Nederlands: wat je begrepen hebt en wat je niet kon afleiden. Verzin niets.\n" +
            "Geef je antwoord altijd door de tool \"antwoorden\" aan te roepen.";

        public async Task<ProjectSjabloonAiVoorstel> Voorstel(string beschrijving, CancellationToken ct = default)
        {
            var res = new ProjectSjabloonAiVoorstel();
            if (string.IsNullOrWhiteSpace(beschrijving)) { res.Fout = "Beschrijf eerst het gebouw."; return res; }
            if (beschrijving.Length > 2000) beschrijving = beschrijving[..2000];
            if (string.IsNullOrWhiteSpace(_o.AnthropicApiKey))
            {
                res.Fout = "De AI is nog niet ingesteld (de Anthropic-sleutel ontbreekt). Vul de antwoorden zelf in.";
                return res;
            }

            var schema = JsonNode.Parse("""
            {"type":"object","properties":{
              "verdiepingen":{"type":"integer"},
              "kelder":{"type":["boolean","null"]},
              "dak":{"type":["string","null"],"enum":["hellend","plat",null]},
              "eenPlanningVoorAlles":{"type":"boolean"},
              "toelichting":{"type":"string"}},"required":["verdiepingen","eenPlanningVoorAlles","toelichting"]}
            """);
            var request = new JsonObject
            {
                ["model"] = _o.AnthropicModel,
                ["max_tokens"] = 600,
                ["system"] = SystemPrompt,
                ["tools"] = new JsonArray(new JsonObject { ["name"] = "antwoorden", ["description"] = "De antwoorden over het gebouw.", ["input_schema"] = schema }),
                ["tool_choice"] = new JsonObject { ["type"] = "auto" },
                ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = "Beschrijving van het gebouw:\n" + beschrijving.Trim() }),
            };
            try
            {
                using var client = _http.CreateClient(PuntenSpraakService.HttpClientName);
                client.Timeout = TimeSpan.FromSeconds(Math.Min(_o.TimeoutSeconds, 60));
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

                // Controleren en begrenzen: de AI is een voorstel, geen waarheid.
                int verd = 1;
                if (input["verdiepingen"] is JsonValue vv)
                {
                    if (vv.TryGetValue<int>(out var vi)) verd = vi;
                    else if (vv.TryGetValue<double>(out var vd)) verd = (int)Math.Round(vd);
                }
                var dak = input["dak"] is JsonValue dv && dv.TryGetValue<string>(out var ds) ? ds?.Trim().ToLowerInvariant() : null;
                res.Antwoorden = new ProjectSjabloonAntwoorden
                {
                    Verdiepingen = Math.Clamp(verd, 1, 20),
                    Kelder = input["kelder"] is JsonValue kv && kv.TryGetValue<bool>(out var kb) && kb,
                    Dak = dak is "hellend" or "plat" ? dak : null,
                    EenPlanningVoorAlles = input["eenPlanningVoorAlles"] is JsonValue pv && pv.TryGetValue<bool>(out var pb) && pb,
                };
                var toelichting = input["toelichting"]?.GetValue<string>()?.Trim();
                res.Toelichting = string.IsNullOrWhiteSpace(toelichting) ? null : (toelichting.Length > 400 ? toelichting[..400] : toelichting);
                res.Ok = true;
                _log.LogInformation("Projectsjabloon AI: {Verd} verdiepingen, kelder {Kelder}, dak {Dak}, model {Model}.", res.Antwoorden.Verdiepingen, res.Antwoorden.Kelder, res.Antwoorden.Dak, _o.AnthropicModel);
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
