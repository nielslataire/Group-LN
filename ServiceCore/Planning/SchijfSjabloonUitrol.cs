using System.Globalization;
using FacadeCore;

namespace ServiceCore.Planning;

/// <summary>Eén schijf zoals ze in het sjabloon staat (invoer voor het uitrekenen).</summary>
public sealed record SchijfInvoer(string Naam, string? PlanningCode, decimal? Vast, decimal Gewicht, bool PerVerdieping, string? VoorwaardeVariabele = null, string? VoorwaardeWaarde = null);

/// <summary>
/// Rekent een schijvensjabloon uit tot een lijst schijven met percentages die samen 100 % zijn:
/// eerst de vaste percentages, de rest naar gewicht, de afrondingsrest op de laatste schijf met een gewicht.
/// Een schijf per verdieping wordt gelijk verdeeld over de verdiepingen. Onder het minimum geeft een waarschuwing, geen blokkade.
/// </summary>
public static class SchijfSjabloonUitrol
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
    public const string Plaatshouder = "{verdieping}";

    private sealed class Item
    {
        public string Naam = "";
        public string? Code;
        public int? Volgnr;
        public decimal? Vast;
        public decimal Gewicht;
    }

    /// <summary>1ste, 2de, 3de ... (8ste).</summary>
    public static string Rangtelwoord(int n) => n + (n == 1 || n == 8 ? "ste" : "de");

    private static string Naam(string naam, int? verdieping)
    {
        var tekst = verdieping == null
            ? naam.Replace(Plaatshouder, "", StringComparison.OrdinalIgnoreCase)
            : naam.Contains(Plaatshouder, StringComparison.OrdinalIgnoreCase)
                ? naam.Replace(Plaatshouder, Rangtelwoord(verdieping.Value) + " verdieping", StringComparison.OrdinalIgnoreCase)
                : naam + " " + Rangtelwoord(verdieping.Value) + " verdieping";
        return System.Text.RegularExpressions.Regex.Replace(tekst, @"\s{2,}", " ").Trim();
    }

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
            if (s.PerVerdieping && n > 1)
            {
                // Het totaal van het blok wordt gelijk verdeeld; de afrondingsrest komt op de laatste verdieping.
                decimal? vastDeel = s.Vast == null ? null : Math.Round(s.Vast.Value / n, 2);
                for (var i = 1; i <= n; i++)
                {
                    decimal? vast = vastDeel;
                    if (s.Vast != null && i == n) vast = Math.Round(s.Vast.Value - vastDeel!.Value * (n - 1), 2);
                    items.Add(new Item { Naam = Naam(s.Naam, i), Code = s.PlanningCode, Volgnr = i, Vast = vast, Gewicht = gewicht / n });
                }
            }
            else
                items.Add(new Item { Naam = Naam(s.Naam, null), Code = s.PlanningCode, Volgnr = s.PerVerdieping ? 1 : null, Vast = s.Vast, Gewicht = gewicht });
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
                var som = gewogen.Sum(i => i.Gewicht);
                decimal verdeeld = 0m;
                for (var k = 0; k < gewogen.Count; k++)
                {
                    var p = k == gewogen.Count - 1 ? rest - verdeeld : Math.Round(rest * gewogen[k].Gewicht / som, 2);
                    percentages[gewogen[k]] = p;
                    verdeeld += p;
                }
            }
        }

        foreach (var i in items)
        {
            var p = percentages[i];
            uit.Schijven.Add(new SchijfVoorstel { Naam = i.Naam, Percentage = p, PlanningCode = i.Code, PlanningVolgnr = i.Volgnr, IsVast = i.Vast != null });
            if (p < minimum) uit.Waarschuwingen.Add($"“{i.Naam}” krijgt {p.ToString("0.##", Nl)} %, onder het minimum van {minimum.ToString("0.##", Nl)} %.");
        }
        uit.Totaal = uit.Schijven.Sum(s => s.Percentage);
        if (Math.Abs(uit.Totaal - 100m) > 0.005m)
            uit.Waarschuwingen.Add($"Het totaal is {uit.Totaal.ToString("0.##", Nl)} % in plaats van 100 %. Pas een percentage aan.");
        return uit;
    }
}
