using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FacadeCore;

namespace ServiceCore.Planning;

/// <summary>
/// De zuivere kant van het AI-voorstel bij een projectsjabloon (geen netwerk, dus te testen): wat de AI te zien krijgt, wat hij terug mag geven en
/// hoe zijn antwoord gecontroleerd en begrensd wordt. De AI doet enkel een voorstel; elk getal en elke verwijzing wordt hier gecontroleerd.
/// </summary>
public static class ProjectSjabloonAiVerwerker
{
    public const string Systeemprompt =
        "Je helpt een projectleider bij een bouwbedrijf in Vlaanderen. Hij beschrijft een gebouw in gewone taal. Jij kiest uit zijn eigen projectsjablonen het sjabloon dat het best past en haalt uit de beschrijving de antwoorden die dat sjabloon nodig heeft.\n" +
        "Je krijgt de beschrijving en een lijst met de sjablonen (met hun structuur) en enkele aantallen van het project. Je krijgt bewust geen namen, adressen of bedragen; verzin ze niet.\n" +
        "Regels:\n" +
        "- sjabloonId: het id van het sjabloon uit de lijst dat het best past. Past er geen, zet dan sjabloonId op null en leg in sjabloonReden uit waarom. Kies nooit een id dat niet in de lijst staat.\n" +
        "- sjabloonReden: één korte zin in het Nederlands waarom dit sjabloon past (of waarom geen).\n" +
        "- verdiepingen: het aantal bouwlagen boven de grond, een geheel getal van 1 tot 20. Wij tellen zoals in Vlaanderen: gelijkvloers, 1ste verdieping, 2de verdieping. Het gelijkvloers is geen verdieping maar telt wel als bouwlaag: gelijkvloers + 1 verdieping = 2, gelijkvloers + 2 verdiepingen = 3. Staat er niets over, gebruik dan het standaardaantal van het sjabloon.\n" +
        "- terrassen: de bouwlagen waarvan de afdek terrassen of luifels krijgt, genummerd met 1 = gelijkvloers, 2 = 1ste verdieping, 3 = 2de verdieping. Een terras op de 1ste verdieping hoort bij de afdek van het gelijkvloers, dus bouwlaag 1. Leeg als er geen terrassen of luifels vermeld worden.\n" +
        "- kelder: true bij een kelder of ondergrondse verdieping of parking, false als er duidelijk geen is, null als het niet gezegd wordt.\n" +
        "- dak: \"hellend\" (zadeldak, schilddak, lessenaarsdak met helling) of \"plat\" (platte of groendaken), null als het niet gezegd wordt.\n" +
        "- eenPlanningVoorAlles: true als het één gebouw met meerdere eenheden is (appartementsgebouw, residentie), false bij losse woningen (rijwoningen, een verkaveling).\n" +
        "- aannames: korte zinnen over alles wat je moest aannemen omdat het er niet stond (bv. \"Geen kelder vermeld: geen kelder aangenomen\"). Geef er geen die je zeker uit de tekst haalt.\n" +
        "- vragen: hoogstens drie korte vragen, enkel over iets dat het plan sterk zou veranderen en dat de beschrijving niet beantwoordt. Geen vragen over zaken die je redelijk kan aannemen.\n" +
        "- toelichting: één of twee korte zinnen in het Nederlands: wat je begrepen hebt. Verzin niets.\n" +
        "Geef je antwoord altijd door de tool \"antwoorden\" aan te roepen.";

    public const string Schema = """
        {"type":"object","properties":{
          "sjabloonId":{"type":["integer","null"]},
          "sjabloonReden":{"type":"string"},
          "verdiepingen":{"type":"integer"},
          "terrassen":{"type":"array","items":{"type":"integer"}},
          "kelder":{"type":["boolean","null"]},
          "dak":{"type":["string","null"],"enum":["hellend","plat",null]},
          "eenPlanningVoorAlles":{"type":"boolean"},
          "aannames":{"type":"array","items":{"type":"string"}},
          "vragen":{"type":"array","items":{"type":"string"}},
          "toelichting":{"type":"string"}},
          "required":["sjabloonId","verdiepingen","eenPlanningVoorAlles","toelichting"]}
        """;

    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");

    /// <summary>Het bericht aan de AI: de beschrijving en de anonieme context (sjablonen en aantallen).</summary>
    public static string BouwBericht(string beschrijving, ProjectSjabloonAiContext? ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Beschrijving van het gebouw:");
        sb.AppendLine((beschrijving ?? "").Trim());
        if (ctx == null) return sb.ToString();
        sb.AppendLine();
        sb.AppendLine("Het project: " + ctx.AantalEenheden + (ctx.AantalEenheden == 1 ? " eenheid" : " eenheden") +
            (ctx.MaxWerkdagen is int m ? ", uitvoeringstermijn " + m + " werkdagen" : ", nog geen uitvoeringstermijn") +
            "; planning " + (ctx.HeeftPlanning ? "bestaat al" : "bestaat nog niet") + ", traject " + (ctx.HeeftTraject ? "bestaat al" : "bestaat nog niet") +
            ", schijven " + (ctx.HeeftSchijven ? "bestaan al" : "bestaan nog niet") + ".");
        sb.AppendLine();
        sb.AppendLine("Beschikbare projectsjablonen (kies een id uit deze lijst):");
        foreach (var k in ctx.Keuzes)
        {
            sb.Append("- id ").Append(k.Id).Append(": ").Append(k.Naam);
            if (!string.IsNullOrWhiteSpace(k.Beschrijving)) sb.Append(" — ").Append(k.Beschrijving.Trim());
            sb.AppendLine();
            sb.Append("  standaard: ").Append(k.Bouwlagen).Append(k.Bouwlagen == 1 ? " bouwlaag" : " bouwlagen")
              .Append(k.Kelder ? ", met kelder" : "").Append(string.IsNullOrEmpty(k.Dak) ? "" : ", " + k.Dak + " dak").AppendLine();
            var kent = new List<string>();
            if (k.HeeftBouwlagen) kent.Add("herhaalt de ruwbouw per bouwlaag");
            if (k.HeeftKelder) kent.Add("kelder");
            if (k.HeeftDak) kent.Add("dak (hellend of plat)");
            if (k.HeeftTerras) kent.Add("terrassen of luifels per bouwlaag");
            if (kent.Count > 0) sb.AppendLine("  past zich aan op: " + string.Join(", ", kent));
            if (!string.IsNullOrWhiteSpace(k.Planning)) sb.AppendLine("  planning: " + k.Planning);
            if (!string.IsNullOrWhiteSpace(k.Traject)) sb.AppendLine("  traject: " + k.Traject);
            if (!string.IsNullOrWhiteSpace(k.Schijven)) sb.AppendLine("  schijven: " + k.Schijven);
        }
        return sb.ToString();
    }

    private static string? Tekst(JsonNode? n, int max)
    {
        if (n is not JsonValue v || !v.TryGetValue<string>(out var s)) return null;
        s = Regex.Replace(s ?? "", @"\s+", " ").Trim();
        if (s.Length == 0) return null;
        return s.Length > max ? s[..max] : s;
    }

    private static List<string> Lijst(JsonNode? n, int maxAantal, int maxLengte)
        => n is JsonArray a ? a.Select(x => Tekst(x, maxLengte)).Where(t => t != null).Select(t => t!).Distinct().Take(maxAantal).ToList() : new List<string>();

    /// <summary>Controleert en begrenst het antwoord van de AI. Wat niet klopt, wordt weggelaten of begrensd; er komt nooit iets uit dat niet in de lijst of de grenzen past.</summary>
    public static ProjectSjabloonAiVoorstel Verwerk(JsonNode input, ProjectSjabloonAiContext? ctx)
    {
        var res = new ProjectSjabloonAiVoorstel();

        // Sjabloon: enkel een id uit de lijst
        ProjectSjabloonAiKeuze? gekozen = null;
        if (ctx != null && input["sjabloonId"] is JsonValue sv && sv.TryGetValue<int>(out var sid))
        {
            gekozen = ctx.Keuzes.FirstOrDefault(k => k.Id == sid);
            if (gekozen == null) res.Aannames.Add("De AI koos een sjabloon dat niet bestaat: kies zelf een projectsjabloon.");
        }
        res.SjabloonId = gekozen?.Id;
        res.SjabloonReden = Tekst(input["sjabloonReden"], 300);

        int verd = gekozen?.Bouwlagen ?? 1;
        var verdGezegd = false;
        if (input["verdiepingen"] is JsonValue vv)
        {
            if (vv.TryGetValue<int>(out var vi)) { verd = vi; verdGezegd = true; }
            else if (vv.TryGetValue<double>(out var vd)) { verd = (int)Math.Round(vd); verdGezegd = true; }
        }
        verd = Math.Clamp(verd, 1, 20);

        var dak = input["dak"] is JsonValue dv && dv.TryGetValue<string>(out var ds) ? ds?.Trim().ToLowerInvariant() : null;
        dak = dak is "hellend" or "plat" ? dak : null;
        bool? kelder = input["kelder"] is JsonValue kv && kv.TryGetValue<bool>(out var kb) ? kb : null;

        var terrassen = new List<int>();
        if (input["terrassen"] is JsonArray ta)
            terrassen = ta.Select(x => x is JsonValue tv && tv.TryGetValue<int>(out var ti) ? ti : 0).Where(n => n >= 1 && n <= verd).Distinct().OrderBy(n => n).ToList();

        res.Antwoorden = new ProjectSjabloonAntwoorden
        {
            Verdiepingen = verd,
            Kelder = kelder == true,
            Dak = dak,
            Terrassen = terrassen,
            EenPlanningVoorAlles = input["eenPlanningVoorAlles"] is JsonValue pv && pv.TryGetValue<bool>(out var pb) && pb,
        };

        // Wat de AI niet wist, mag niet stil een waarde krijgen: de aanname staat er altijd bij.
        res.Aannames.AddRange(Lijst(input["aannames"], 8, 200));
        void Aanname(string tekst) { if (!res.Aannames.Any(a => a.Contains(tekst.Split(':')[0], StringComparison.OrdinalIgnoreCase))) res.Aannames.Add(tekst); }
        if (kelder == null) Aanname("Kelder niet vermeld: geen kelder aangenomen.");
        if (dak == null) Aanname("Dak niet vermeld: geen keuze voor hellend of plat.");
        if (!verdGezegd) Aanname($"Aantal bouwlagen niet vermeld: {verd} aangenomen.");

        res.Vragen = Lijst(input["vragen"], 3, 200);
        res.Toelichting = Tekst(input["toelichting"], 400);
        res.Ok = true;
        return res;
    }

    // ── Privacy: niets van wat persoonlijk kan zijn mag naar de AI ──

    private static readonly (string Soort, Regex Patroon)[] Privacy =
    {
        ("een e-mailadres", new Regex(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.Compiled)),
        ("een telefoonnummer", new Regex(@"(?<![\d.])(\+|00)?\d[\d\s./-]{7,}\d(?![\d])", RegexOptions.Compiled)),
        ("een ondernemings- of btw-nummer", new Regex(@"\b(BE)?\s?0\d{3}[.\s]?\d{3}[.\s]?\d{3}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("een adres", new Regex(@"\b\p{L}+(straat|laan|weg|plein|dreef|steenweg|baan|lei|singel|kaai|markt|dijk|gracht|hof|park)\s+\d+\w?\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("een bedrag", new Regex(@"(€|\beur(o)?\b)\s?\d|\d\s?(€|\beuro\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    };

    /// <summary>Zoekt in de tekst wat persoonlijk kan zijn (e-mail, telefoon, adres, bedrag, ondernemingsnummer). Namen van mensen kunnen we niet herkennen.</summary>
    public static List<string> ControleerPrivacy(string? tekst)
    {
        var gevonden = new List<string>();
        if (string.IsNullOrWhiteSpace(tekst)) return gevonden;
        foreach (var (soort, patroon) in Privacy) if (patroon.IsMatch(tekst)) gevonden.Add(soort);
        return gevonden;
    }
}
