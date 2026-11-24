using System.Globalization;
using FacadeCore;

namespace ServiceCore.Planning;

/// <summary>Eén schijf zoals ze in het sjabloon staat (invoer voor het uitrekenen).</summary>
public sealed record SchijfInvoer(string Naam, string? PlanningCode, decimal? Vast, decimal Gewicht, bool PerVerdieping, string? VoorwaardeVariabele = null, string? VoorwaardeWaarde = null, int? BouwstaatId = null, byte BouwstaatRegel = 0);

/// <summary>
/// Rekent een schijvensjabloon uit tot een lijst schijven met percentages die samen 100 % zijn:
/// eerst de vaste percentages, de rest naar gewicht, de afrondingsrest op de laatste schijf met een gewicht.
/// Een schijf per verdieping wordt gelijk verdeeld over de verdiepingen. Onder het minimum geeft een waarschuwing, geen blokkade.
/// </summary>
public static class SchijfSjabloonUitrol
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
    public const string Plaatshouder = "{verdieping}";
    /// <summary>Schijven met een gewicht krijgen een percentage in stappen van 5 %.</summary>
    public const decimal Stap = 5m;

    private sealed class Item
    {
        public string Naam = "";
        public string? Code;
        public int? Volgnr;
        public decimal? Vast;
        public decimal Gewicht;
        public int? BouwstaatId;
        public byte BouwstaatRegel;
    }

    /// <summary>1ste, 2de, 3de ... (8ste).</summary>
    public static string Rangtelwoord(int n) => n + (n == 1 || n == 8 ? "ste" : "de");

    private static string Naam(string naam, int? verdieping)
    {
        var tekst = verdieping == null
            ? naam.Replace(Plaatshouder, "", StringComparison.OrdinalIgnoreCase)
            : naam.Contains(Plaatshouder, StringComparison.OrdinalIgnoreCase)
                ? naam.Replace(Plaatshouder, PlanningSjabloonUitrol.BouwlaagNaam(verdieping.Value), StringComparison.OrdinalIgnoreCase)
                : naam + " " + PlanningSjabloonUitrol.BouwlaagNaam(verdieping.Value);
        return System.Text.RegularExpressions.Regex.Replace(tekst, @"\s{2,}", " ").Trim();
    }

    /// <summary>Voorwaarde "laag:boven": de schijf geldt voor elke bouwlaag boven het gelijkvloers (1ste verdieping, 2de verdieping, ...).</summary>
    public static bool IsBovenGelijkvloers(SchijfInvoer s)
        => string.Equals((s.VoorwaardeVariabele ?? "").Trim(), "laag", StringComparison.OrdinalIgnoreCase) && string.Equals((s.VoorwaardeWaarde ?? "").Trim(), "boven", StringComparison.OrdinalIgnoreCase);

    /// <summary>Zelfde regels als bij de blokken van een planningssjabloon: kelder ja/nee, dak hellend/plat. Geen voorwaarde = altijd.</summary>
    public static bool VoorwaardeKlopt(SchijfInvoer s, bool kelder, string? dak)
    {
        if (string.IsNullOrWhiteSpace(s.VoorwaardeVariabele)) return true;
        var waarde = (s.VoorwaardeWaarde ?? "").Trim().ToLowerInvariant();
        return s.VoorwaardeVariabele.Trim().ToLowerInvariant() switch
        {
            "kelder" => waarde is "ja" or "true" ? kelder : (waarde is "nee" or "false" ? !kelder : kelder),
            "dak" => string.Equals(dak ?? "", waarde, StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }

    public static SchijfVoorbeeld Rol(decimal minimum, IReadOnlyList<SchijfInvoer> schijven, int verdiepingen, bool kelder = false, string? dak = null)
    {
        var uit = new SchijfVoorbeeld();
        var n = Math.Max(1, verdiepingen);
        var items = new List<Item>();

        foreach (var s in schijven.Where(x => VoorwaardeKlopt(x, kelder, dak)))
        {
            var gewicht = s.Gewicht > 0 ? s.Gewicht : 1m;
            if (IsBovenGelijkvloers(s))
            {
                // Eén schijf per bouwlaag boven het gelijkvloers (gewicht of vast percentage gelden per bouwlaag, niet verdeeld). Het volgnummer is de plaats
                // van de taak met deze code in de planning: met een kelder staat die code ook op het gelijkvloers (eerste plaat boven de kelder), zonder kelder niet.
                for (var i = 2; i <= n; i++)
                    items.Add(new Item { Naam = Naam(s.Naam, i), Code = s.PlanningCode, Volgnr = kelder ? i : i - 1, Vast = s.Vast, Gewicht = gewicht, BouwstaatId = s.BouwstaatId, BouwstaatRegel = s.BouwstaatRegel });
                continue;
            }
            if (s.PerVerdieping && n > 1)
            {
                // Het totaal van het blok wordt gelijk verdeeld; de afrondingsrest komt op de laatste verdieping.
                decimal? vastDeel = s.Vast == null ? null : Math.Round(s.Vast.Value / n, 2);
                for (var i = 1; i <= n; i++)
                {
                    decimal? vast = vastDeel;
                    if (s.Vast != null && i == n) vast = Math.Round(s.Vast.Value - vastDeel!.Value * (n - 1), 2);
                    items.Add(new Item { Naam = Naam(s.Naam, i), Code = s.PlanningCode, Volgnr = i, Vast = vast, Gewicht = gewicht / n, BouwstaatId = s.BouwstaatId, BouwstaatRegel = s.BouwstaatRegel });
                }
            }
            else
                items.Add(new Item { Naam = Naam(s.Naam, null), Code = s.PlanningCode, Volgnr = s.PerVerdieping ? 1 : null, Vast = s.Vast, Gewicht = gewicht, BouwstaatId = s.BouwstaatId, BouwstaatRegel = s.BouwstaatRegel });
        }

        var vasteSom = items.Where(i => i.Vast != null).Sum(i => i.Vast!.Value);
        var gewogen = items.Where(i => i.Vast == null).ToList();
        var percentages = new Dictionary<Item, decimal>();
        foreach (var i in items.Where(i => i.Vast != null)) percentages[i] = i.Vast!.Value;

        if (gewogen.Count > 0)
        {
            var rest = 100m - vasteSom;
            if (rest <= 0m)
            {
                uit.Waarschuwingen.Add($"De vaste percentages zijn samen {vasteSom.ToString("0.##", Nl)} %: er blijft niets over voor de schijven met een gewicht.");
                foreach (var i in gewogen) percentages[i] = 0m;
            }
            else
            {
                // Per stap van 5 % (en minstens één stap per schijf): de rest wordt in stappen verdeeld naar gewicht, de grootste afrondingsrest eerst.
                // Zijn de vaste percentages geen veelvoud van 5, dan komt wat overblijft op de laatste schijf met een gewicht.
                var som = gewogen.Sum(i => i.Gewicht);
                var stappen = (int)Math.Floor(rest / Stap);
                var ideaal = gewogen.Select(i => (double)(rest * i.Gewicht / som / Stap)).ToList();
                var aantal = ideaal.Select(x => (int)Math.Floor(x)).ToList();
                var nog = stappen - aantal.Sum();
                foreach (var k in Enumerable.Range(0, gewogen.Count).OrderByDescending(k => ideaal[k] - aantal[k]).ThenBy(k => k).Take(Math.Max(0, nog))) aantal[k]++;
                // Elke schijf krijgt minstens één stap: die komt van de schijf met de meeste stappen.
                for (var k = 0; k < gewogen.Count; k++)
                    while (aantal[k] < 1 && aantal.Max() > 1) { aantal[aantal.IndexOf(aantal.Max())]--; aantal[k]++; }
                decimal verdeeld = 0m;
                for (var k = 0; k < gewogen.Count; k++)
                {
                    var p = k == gewogen.Count - 1 ? rest - verdeeld : aantal[k] * Stap;
                    percentages[gewogen[k]] = p;
                    verdeeld += p;
                }
            }
        }

        foreach (var i in items)
        {
            var p = percentages[i];
            uit.Schijven.Add(new SchijfVoorstel { Naam = i.Naam, Percentage = p, PlanningCode = i.Code, PlanningVolgnr = i.Volgnr, IsVast = i.Vast != null, BouwstaatId = i.BouwstaatId, BouwstaatRegel = i.BouwstaatRegel });
            if (p < minimum) uit.Waarschuwingen.Add($"“{i.Naam}” krijgt {p.ToString("0.##", Nl)} %, onder het minimum van {minimum.ToString("0.##", Nl)} %.");
        }
        uit.Totaal = uit.Schijven.Sum(s => s.Percentage);
        if (Math.Abs(uit.Totaal - 100m) > 0.005m)
            uit.Waarschuwingen.Add($"Het totaal is {uit.Totaal.ToString("0.##", Nl)} % in plaats van 100 %. Pas een percentage aan.");
        return uit;
    }
}
