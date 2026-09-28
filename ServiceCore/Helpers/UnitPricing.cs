using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore.Helpers
{
    /// <summary>De ENE rekenregel voor de constructieprijs van een eenheid, gelijk aan wat de publieke site
    /// (WWWCOPRO: Projects/Detail.vbhtml, de vanaf-prijs in BlogController, Service/ProjectService.vb) al doet:
    /// <list type="bullet">
    /// <item>Een eenheid ZONDER afwerkingen: grondwaarde + ALLE constructieprijzen samen.</item>
    /// <item>Een eenheid MET afwerkingen: elke afwerking is een volledig alternatief (afgewerkt, casco, …) —
    /// grondwaarde + de constructieprijzen van die ene afwerking. Constructieprijzen buiten een afwerking
    /// tellen dan niet mee (in de praktijk bestaan die naast afwerkingen niet).</item>
    /// </list>
    /// "Vanaf" = de goedkoopste afwerking. Vóór deze klasse telden de eenhedenlijst, de koppeldialoog en
    /// de projecttotalen (ProjectService.GetProjectSalesData) dit elk op hun eigen, onderling
    /// afwijkende manier — bv. alle afwerkingen samen opgeteld.</summary>
    public static class UnitPricing
    {
        public readonly struct Result
        {
            public Result(decimal from, decimal to, bool hasOptions, decimal standard)
            {
                From = from;
                To = to;
                HasOptions = hasOptions;
                Standard = standard;
            }

            /// <summary>Constructieprijs van de STANDAARDafwerking (UnitFinishingOption.IsDefault) — waarmee
            /// waarde-/budgetcijfers rekenen. Is er geen standaard aangeduid (of niet gevonden), dan valt dit terug
            /// op <see cref="From"/>. Zonder afwerkingen gelijk aan <see cref="From"/>.</summary>
            public decimal Standard { get; }

            /// <summary>Constructieprijs zonder grond: de goedkoopste afwerking, of de som van alle regels.</summary>
            public decimal From { get; }
            /// <summary>De duurste afwerking (gelijk aan <see cref="From"/> zonder afwerkingen).</summary>
            public decimal To { get; }
            public bool HasOptions { get; }
        }

        /// <param name="values">Per constructieprijsregel: het afwerking-id (null = geen afwerking) en het bedrag.</param>
        /// <param name="defaultOptionId">Het id van de standaardafwerking van deze eenheid, indien gekend.</param>
        public static Result Compute(IEnumerable<(int? OptionId, decimal Value)> values, int? defaultOptionId = null)
        {
            var list = (values ?? Enumerable.Empty<(int? OptionId, decimal Value)>()).ToList();
            var byOption = list
                .Where(v => v.OptionId.HasValue)
                .GroupBy(v => v.OptionId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.Value));

            if (byOption.Count > 0)
            {
                var min = byOption.Values.Min();
                var standard = defaultOptionId.HasValue && byOption.TryGetValue(defaultOptionId.Value, out var d) ? d : min;
                return new Result(min, byOption.Values.Max(), true, standard);
            }

            var total = list.Sum(v => v.Value);
            return new Result(total, total, false, total);
        }
    }
}
