using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace CPMCore.Services.PuntenSpraak
{
    public record SpraakSegment(double Start, double End, string Text);
    public record SpraakTranscript(string Text, List<SpraakSegment> Segments);
    public record SpraakItem(int Id, string Name);
    public record SpraakBestaand(int Nr, string Title, string Unit, string Zone);

    /// <summary>Alles wat de AI nodig heeft om een transcript om te zetten naar punten van dit project.</summary>
    public class SpraakContext
    {
        public List<SpraakItem> Units { get; set; } = new();
        public List<SpraakItem> Contractors { get; set; } = new();
        public List<SpraakBestaand> Bestaand { get; set; } = new();
        public int? DefaultUnitId { get; set; }
        /// <summary>Tijdstippen (in seconden) waarop er tijdens de rondgang een foto is genomen; index = fotonummer.</summary>
        public List<double> PhotoTimes { get; set; } = new();
    }

    public class PuntVoorstel
    {
        public string Title { get; set; } = "";
        public int? UnitId { get; set; }
        public string UnitName { get; set; } = "";
        public string Zone { get; set; } = "";
        public int? ContractorId { get; set; }
        public string ContractorName { get; set; } = "";
        public string Description { get; set; } = "";
        public int Priority { get; set; } = 1;
        /// <summary>Nummer van een bestaand punt waar dit mee overeenkomt (ontdubbelen), anders null.</summary>
        public int? ExistingNr { get; set; }
        public double? AtSeconds { get; set; }
        public List<int> PhotoIndexes { get; set; } = new();
    }

    /// <summary>Spraak naar tekst (OpenAI) en tekst naar voorgestelde punten (Anthropic Messages API, tool use zodat het antwoord altijd
    /// gestructureerd is). Zonder sleutels is <see cref="Configured"/> onwaar en doet de UI niets.</summary>
    public class PuntenSpraakService
    {
        public const string HttpClientName = "PuntenSpraakClient";
        private const string TranscribeUrl = "https://api.openai.com/v1/audio/transcriptions";
        private const string MessagesUrl = "https://api.anthropic.com/v1/messages";

        private readonly PuntenSpraakOptions _o;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<PuntenSpraakService> _log;

        public PuntenSpraakService(IOptions<PuntenSpraakOptions> options, IOptions<QuoteExtraction.QuoteExtractionOptions> quote, IHttpClientFactory http, ILogger<PuntenSpraakService> log)
        {
            _o = options.Value;
            if (string.IsNullOrWhiteSpace(_o.AnthropicApiKey)) _o.AnthropicApiKey = quote.Value.AnthropicApiKey;
            _http = http; _log = log;
        }

        public bool Configured => _o.Configured;
        /// <summary>Welke sleutel er ontbreekt, in gewone woorden (voor de melding in de pagina).</summary>
        public string Missing => string.Join(" en ", new[]
        {
            string.IsNullOrWhiteSpace(_o.OpenAiApiKey) ? "de OpenAI-sleutel (PuntenSpraak__OpenAiApiKey)" : null,
            string.IsNullOrWhiteSpace(_o.AnthropicApiKey) ? "de Anthropic-sleutel (PuntenSpraak__AnthropicApiKey of QuoteExtraction__AnthropicApiKey)" : null,
        }.Where(x => x != null));
        public int MaxAudioBytes => _o.MaxAudioMb * 1024 * 1024;

        /// <summary>Spraak naar tekst. Met <paramref name="withTimestamps"/> (rondgang) komen er tijdstempels per zin mee.</summary>
        public async Task<SpraakTranscript> TranscribeAsync(byte[] audio, string fileName, string contentType, bool withTimestamps, string? hint, CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(audio);
            // "audio/webm;codecs=opus" -> "audio/webm": sommige gsm-browsers sturen extra parameters mee die de dienst niet verwacht
            var baseType = (contentType ?? "").Split(';')[0].Trim();
            file.Headers.ContentType = MediaTypeHeaderValue.TryParse(baseType, out var ct0) ? ct0 : new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "file", string.IsNullOrWhiteSpace(fileName) ? "opname.webm" : fileName);
            form.Add(new StringContent(withTimestamps ? _o.RondgangTranscribeModel : _o.TranscribeModel), "model");
            form.Add(new StringContent("nl"), "language");
            // De prompt stuurt de spelling: eerst de namen van dit project (ingekort), dan de bouwtermen die spraakherkenning vaak verkeerd verstaat.
            var names = string.IsNullOrWhiteSpace(hint) ? "" : (hint.Length > 330 ? hint[..330] : hint) + " ";
            form.Add(new StringContent(names + Bouwtermen), "prompt");
            if (withTimestamps)
            {
                form.Add(new StringContent("verbose_json"), "response_format");
                form.Add(new StringContent("segment"), "timestamp_granularities[]");
            }
            using var client = _http.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(_o.TimeoutSeconds * (withTimestamps ? 3 : 1));
            using var req = new HttpRequestMessage(HttpMethod.Post, TranscribeUrl) { Content = form };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _o.OpenAiApiKey);
            using var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Spraak naar tekst: HTTP {Status}: {Body}", (int)res.StatusCode, body.Length > 300 ? body[..300] : body);
                throw new InvalidOperationException(Uitleg("De spraakdienst (OpenAI)", (int)res.StatusCode, body));
            }
            var json = JsonNode.Parse(body);
            var text = json?["text"]?.GetValue<string>() ?? "";
            var segs = new List<SpraakSegment>();
            foreach (var s in json?["segments"]?.AsArray() ?? new JsonArray())
                if (s != null) segs.Add(new SpraakSegment(s["start"]?.GetValue<double>() ?? 0, s["end"]?.GetValue<double>() ?? 0, (s["text"]?.GetValue<string>() ?? "").Trim()));
            return new SpraakTranscript(text.Trim(), segs);
        }

        /// <summary>Vlaamse bouwtermen die als "prompt" aan de spraakherkenning meegaan, zodat bv. "plint" niet als "let" wordt uitgeschreven.</summary>
        private const string Bouwtermen =
            "Bouwtermen: plint, plinten, vloerplint, lat, latten, gyproc, plaatwerk, stucwerk, pleisterwerk, chape, dekvloer, raamdorpel, vensterbank, " +
            "afkitten, kitwerk, silicone, voegen, tegelwerk, schilderwerk, spuitwerk, dakgoot, hemelwaterafvoer, isolatie, dampscherm, luchtdichting, " +
            "binnendeur, buitenschrijnwerk, scheur, gebarsten, vastzetten, bevestigen, herstellen, sanitair, ventilatie, stopcontact, schakelaar, leidingen.";

        private const string SystemPrompt =
            "Je helpt een werfleider van een Belgische bouwpromotor. Je krijgt de uitgeschreven spraak van een werfbezoek en maakt er afzonderlijke werfpunten van " +
            "(dingen die op de werf nog opgelost, afgewerkt of hersteld moeten worden). Regels:\n" +
            "- Elk punt heeft een korte, concrete titel in het Nederlands (maximaal 80 tekens, begint met een hoofdletter, geen eindpunt), bv. \"Silicone rond bad niet afgewerkt\".\n" +
            "- Kies de eenheid enkel uit de gegeven lijst (unitId). Noemt de spreker geen eenheid, gebruik dan de standaard eenheid; gaat het over de hele werf, laat unitId leeg.\n" +
            "- Kies de aannemer enkel uit de gegeven lijst (contractorId), en enkel als de spreker een naam of een duidelijk vak (bv. tegelzetter) noemt dat bij een aannemer past. Twijfel je, laat het leeg.\n" +
            "- zone is de kamer of plaats (bv. badkamer, voorgevel), in kleine letters.\n" +
            "- priority: 1 normaal, 2 hoog, 3 kritiek (enkel bij veiligheid, lekkage of als de spreker het zegt).\n" +
            "- Komt een punt overeen met een bestaand open punt uit de lijst, zet dan existingNr op het nummer van dat punt; maak geen nieuw punt aan voor wat al bestaat zonder dit aan te geven.\n" +
            "- atSeconds is het moment in de opname waarop het punt wordt genoemd (enkel als tijdstippen gegeven zijn). photoIndexes zijn de nummers van de foto's die tijdens dat punt genomen zijn.\n" +
            "- Verzin niets. Geen punten voor opmerkingen die geen actie vragen. Spraakherkenning maakt fouten: corrigeer duidelijke verhoren met behulp van de lijsten.\n" +
            "- Het transcript bevat vaak verhoren van Vlaamse bouwtermen en cijfers. Voorbeelden: \"let\", \"plin\" of \"blint\" bedoelt meestal \"plint\"; \"gips\" kan \"gyproc\" zijn; een enkele letter \"l\" of \"i\" achter \"slaapkamer\" is het cijfer 1. " +
            "Kies de bouwterm die in de context van een werf het meest logisch is, ook als het woord in het transcript anders is geschreven, en schrijf die correct in de titel.\n" +
            "Geef je antwoord altijd door de tool \"voorstellen\" aan te roepen, ook als er geen enkel punt is (lege lijst).";

        public async Task<List<PuntVoorstel>> ExtractAsync(SpraakTranscript transcript, SpraakContext ctx, CancellationToken ct = default)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Datum: " + DateTime.Today.ToString("dd/MM/yyyy"));
            sb.AppendLine("Eenheden (id: naam): " + string.Join("; ", ctx.Units.Select(u => $"{u.Id}: {u.Name}")));
            if (ctx.DefaultUnitId is int d) sb.AppendLine($"Standaard eenheid: {d}");
            sb.AppendLine("Aannemers (id: naam): " + string.Join("; ", ctx.Contractors.Select(c => $"{c.Id}: {c.Name}")));
            if (ctx.Bestaand.Count > 0)
                sb.AppendLine("Bestaande open punten (nr: titel, eenheid, zone):\n" + string.Join("\n", ctx.Bestaand.Take(200).Select(b => $"{b.Nr}: {b.Title}, {b.Unit}, {b.Zone}")));
            if (ctx.PhotoTimes.Count > 0)
                sb.AppendLine("Foto's genomen op (seconden): " + string.Join(", ", ctx.PhotoTimes.Select((t, i) => $"foto {i}: {t:0}s")));
            sb.AppendLine();
            sb.AppendLine("Transcript:");
            if (transcript.Segments.Count > 0)
                foreach (var s in transcript.Segments) sb.AppendLine($"[{s.Start:0}s] {s.Text}");
            else sb.AppendLine(transcript.Text);

            var schema = JsonNode.Parse("""
            {"type":"object","properties":{"punten":{"type":"array","items":{"type":"object","properties":{
              "title":{"type":"string"},"unitId":{"type":["integer","null"]},"zone":{"type":["string","null"]},"contractorId":{"type":["integer","null"]},
              "description":{"type":["string","null"]},"priority":{"type":"integer"},"existingNr":{"type":["integer","null"]},
              "atSeconds":{"type":["number","null"]},"photoIndexes":{"type":"array","items":{"type":"integer"}}},"required":["title"]}}},"required":["punten"]}
            """);
            var request = new JsonObject
            {
                ["model"] = _o.AnthropicModel,
                ["max_tokens"] = 4096,
                ["system"] = SystemPrompt,
                ["tools"] = new JsonArray(new JsonObject { ["name"] = "voorstellen", ["description"] = "De herkende werfpunten.", ["input_schema"] = schema }),
                // Niet elk model aanvaardt een afgedwongen tool ("tool_choice type tool/any is not supported"): automatisch laten kiezen,
                // de opdracht zegt dat de tool gebruikt moet worden en een antwoord in gewone tekst wordt hieronder ook begrepen.
                ["tool_choice"] = new JsonObject { ["type"] = "auto" },
                ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = sb.ToString() }),
            };
            using var client = _http.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(_o.TimeoutSeconds);
            using var req = new HttpRequestMessage(HttpMethod.Post, MessagesUrl) { Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json") };
            req.Headers.Add("x-api-key", _o.AnthropicApiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            using var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Punten herkennen: HTTP {Status}: {Body}", (int)res.StatusCode, body.Length > 300 ? body[..300] : body);
                throw new InvalidOperationException(Uitleg("De AI-dienst (Anthropic)", (int)res.StatusCode, body));
            }
            var content = JsonNode.Parse(body)?["content"]?.AsArray();
            var input = content?.FirstOrDefault(c => c?["type"]?.GetValue<string>() == "tool_use")?["input"];
            if (input == null)
            {
                // terugval: het model antwoordde in gewone tekst (met JSON, eventueel in een codeblok)
                var text = string.Join("\n", (content ?? new JsonArray()).Select(c => c?["text"]?.GetValue<string>()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
                var i0 = text.IndexOf('{'); var i1 = text.LastIndexOf('}');
                if (i0 >= 0 && i1 > i0) { try { input = JsonNode.Parse(text[i0..(i1 + 1)]); } catch { /* geen json */ } }
            }
            var result = new List<PuntVoorstel>();
            var units = ctx.Units.ToDictionary(u => u.Id, u => u.Name);
            var contractors = ctx.Contractors.ToDictionary(c => c.Id, c => c.Name);
            var bestaand = ctx.Bestaand.Select(b => b.Nr).ToHashSet();
            foreach (var p in input?["punten"]?.AsArray() ?? new JsonArray())
            {
                var title = p?["title"]?.GetValue<string>()?.Trim();
                if (p == null || string.IsNullOrWhiteSpace(title)) continue;
                int? unitId = ReadInt(p["unitId"]); if (unitId == null && ctx.DefaultUnitId != null) unitId = ctx.DefaultUnitId;
                if (unitId is int u0 && !units.ContainsKey(u0)) unitId = null;
                int? contractorId = ReadInt(p["contractorId"]); if (contractorId is int c0 && !contractors.ContainsKey(c0)) contractorId = null;
                int? existing = ReadInt(p["existingNr"]); if (existing is int e0 && !bestaand.Contains(e0)) existing = null;
                var pr = ReadInt(p["priority"]) ?? 1;
                result.Add(new PuntVoorstel
                {
                    Title = title, UnitId = unitId, UnitName = unitId is int u1 ? units[u1] : "Algemeen",
                    Zone = p["zone"]?.GetValue<string>()?.Trim() ?? "", ContractorId = contractorId, ContractorName = contractorId is int c1 ? contractors[c1] : "",
                    Description = p["description"]?.GetValue<string>()?.Trim() ?? "", Priority = Math.Clamp(pr, 0, 3), ExistingNr = existing,
                    AtSeconds = p["atSeconds"] is JsonValue av && av.TryGetValue<double>(out var at) ? at : null,
                    PhotoIndexes = (p["photoIndexes"]?.AsArray() ?? new JsonArray()).Select(ReadInt).Where(i => i.HasValue && i >= 0 && i < ctx.PhotoTimes.Count).Select(i => i!.Value).Distinct().ToList(),
                });
            }
            _log.LogInformation("Punten herkennen: {Count} voorstellen, model {Model}.", result.Count, _o.AnthropicModel);
            return result;
        }

        /// <summary>Een leesbare melding voor een foutantwoord: bij 429 (geen tegoed of te veel aanvragen) en 401 (sleutel ongeldig) met wat de leverancier zelf meldt.</summary>
        private static string Uitleg(string dienst, int status, string body)
        {
            string? detail = null;
            try { var e = JsonNode.Parse(body)?["error"]; detail = e?["message"]?.GetValue<string>() ?? e?["code"]?.GetValue<string>(); } catch { /* geen json */ }
            var hint = status switch
            {
                429 => " Meestal is het tegoed op of is er nog geen betaalmethode ingesteld voor de API (een ChatGPT- of Claude-abonnement geldt niet voor de API), of er zijn tijdelijk te veel aanvragen.",
                401 or 403 => " De sleutel is ongeldig of mag deze dienst niet gebruiken.",
                _ => "",
            };
            if (detail != null && detail.Length > 200) detail = detail[..200];
            return $"{dienst} antwoordde met HTTP {status}.{hint}" + (string.IsNullOrWhiteSpace(detail) ? "" : $" Melding: {detail}");
        }

        private static int? ReadInt(JsonNode? n)
        {
            if (n is not JsonValue v) return null;
            if (v.TryGetValue<int>(out var i)) return i;
            if (v.TryGetValue<double>(out var d)) return (int)Math.Round(d);
            if (v.TryGetValue<string>(out var s) && int.TryParse(s, out var j)) return j;
            return null;
        }
    }
}
