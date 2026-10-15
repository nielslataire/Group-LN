using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ServiceCore.Budget
{
    /// <summary>Eén aankoop (akte) van grond binnen een budget.</summary>
    public class GrondAankoop
    {
        public string O { get; set; }
        /// <summary>Datum van de akte (yyyy-MM-dd), optioneel.</summary>
        public string D { get; set; }
        public decimal P { get; set; }
    }

    /// <summary>
    /// Notarieel ereloon voor de aankoop van grond: Tarief van de honoraria der notarissen (KB 16/12/1950), officieuze consolidatie 9 februari 2026
    /// (na indexering 1/1/2026), art. 17 nr. 81, "verkoop uit de hand": schaal J (de aankoper is een ontwikkelaar, dus niet het verlaagde schaal Jbis voor
    /// een eigen en enige woning). Opeenvolgende reeksen: elke schijf is een stuk van de prijs, geen cumulatieve grens.
    /// Een akte krijgt een vast gedeelte van 285 EUR (minus 86 EUR als de prijs niet hoger is dan 20.000 EUR), plus het evenredige deel, plus 21 % btw.
    /// De bijkomende kosten (vacaties, kadaster, hypotheekkantoor, uitgaven) staan er niet in: die komen per akte als apart bedrag.
    /// </summary>
    public static class NotarisTarief
    {
        /// <summary>(grootte van de reeks, percentage als fractie). De laatste reeks (surplus) is onbegrensd.</summary>
        public static readonly (decimal Grootte, decimal Perc)[] Reeksen =
        {
            (7_500m,   0.0250m),
            (10_000m,  0.0250m),
            (12_500m,  0.0175m),
            (15_495m,  0.0171m),
            (18_595m,  0.0114m),
            (186_005m, 0.0057m),
            (249_905m, 0.0020m),
            (decimal.MaxValue, 0.0020m),   // surplus
        };

        public const decimal Vast = 285m;
        public const decimal VastVermindering = 86m;      // als de prijs <= 20.000 EUR
        public const decimal VerminderingGrens = 20_000m;
        public const decimal Btw = 0.21m;
        /// <summary>Standaard bijkomende kosten per akte (vacaties, kadaster, hypotheekkantoor, uitgaven); per budget aanpasbaar.</summary>
        public const decimal AktekostenStandaard = 1500m;

        /// <summary>Ereloon zonder btw voor één akte.</summary>
        public static decimal Ereloon(decimal aankoopprijs)
        {
            if (aankoopprijs <= 0m) return 0m;
            var basis = Math.Round(aankoopprijs, 0, MidpointRounding.AwayFromZero);   // art. 4: van euro tot euro, halve euro's naar boven
            decimal totaal = Vast - (basis <= VerminderingGrens ? VastVermindering : 0m), vorige = 0m, grens = 0m;
            foreach (var (grootte, perc) in Reeksen)
            {
                grens = grootte == decimal.MaxValue ? decimal.MaxValue : grens + grootte;
                if (basis <= vorige) break;
                totaal += (Math.Min(basis, grens) - vorige) * perc;
                vorige = grens;
            }
            return Math.Round(totaal, 2);
        }

        /// <summary>Ereloon inclusief 21 % btw voor één akte.</summary>
        public static decimal EerelonInclBtw(decimal aankoopprijs) => Math.Round(Ereloon(aankoopprijs) * (1m + Btw), 2);

        /// <summary>De aankopen uit de opgeslagen JSON; zonder (geldige) rijen telt de totale aankoopprijs als één akte.</summary>
        public static List<GrondAankoop> Aankopen(string json, decimal totaalFallback)
        {
            List<GrondAankoop> lijst = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try { lijst = JsonSerializer.Deserialize<List<GrondAankoop>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch (JsonException) { lijst = null; }
            }
            lijst = (lijst ?? new List<GrondAankoop>()).Where(a => a != null && a.P > 0m).ToList();
            if (lijst.Count == 0 && totaalFallback > 0m) lijst.Add(new GrondAankoop { O = "Aankoop", P = totaalFallback });
            return lijst;
        }

        public static string NaarJson(IEnumerable<GrondAankoop> lijst) => JsonSerializer.Serialize(lijst.Select(a => new { o = a.O ?? "", d = a.D ?? "", p = a.P }));

        /// <summary>De schaal als JSON voor de live berekening op de Parameters-pagina.</summary>
        public static string SchaalJson()
        {
            var inv = CultureInfo.InvariantCulture;
            var reeksen = string.Join(",", Reeksen.Select(r => "[" + (r.Grootte == decimal.MaxValue ? "null" : r.Grootte.ToString(inv)) + "," + r.Perc.ToString(inv) + "]"));
            return "{\"vast\":" + Vast.ToString(inv) + ",\"red\":" + VastVermindering.ToString(inv) + ",\"redTot\":" + VerminderingGrens.ToString(inv) + ",\"btw\":" + Btw.ToString(inv) + ",\"s\":[" + reeksen + "]}";
        }
    }
}
