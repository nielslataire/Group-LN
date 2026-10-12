using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ServiceCore.Budget
{
    /// <summary>Rekenregel van stap 1: oppervlakte funderingen (som van aantal x m² per type) en het grondwerk in m³
    /// = oppervlakte x (diepte fundering + aantal ondergrondse verdiepingen x diepte per verdieping) / 100. Het JS op de pagina rekent hetzelfde.</summary>
    public static class GrondwerkRekenregel
    {
        public const decimal StandaardDiepteFunderingCm = 40m;
        public const decimal StandaardDiepteOndergrondsCm = 300m;

        public class Lijn { public string Omschrijving { get; set; } public decimal Aantal { get; set; } public decimal Oppervlakte { get; set; } }

        public static List<Lijn> Lees(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<Lijn>();
            try { return JsonSerializer.Deserialize<List<Lijn>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Lijn>(); }
            catch { return new List<Lijn>(); }
        }

        public static decimal Som(IEnumerable<Lijn> lijnen) => Math.Round(lijnen.Sum(l => l.Aantal * l.Oppervlakte), 2);

        /// <summary>m3 = opp. funderingen x diepte fundering + opp. ondergrondse verdiepingen x diepte per verdieping (alles / 100). Zonder opgegeven oppervlakte
        /// voor de ondergrondse verdiepingen: aantal verdiepingen x oppervlakte funderingen.</summary>
        public static decimal M3(decimal oppFunderingen, decimal? diepteFunderingCm, decimal? oppOndergronds, int aantalOndergronds, decimal? diepteOndergrondsCm)
        {
            var opp2 = oppOndergronds ?? aantalOndergronds * oppFunderingen;
            return Math.Round((oppFunderingen * (diepteFunderingCm ?? StandaardDiepteFunderingCm) + opp2 * (diepteOndergrondsCm ?? StandaardDiepteOndergrondsCm)) / 100m, 2);
        }
    }
}
