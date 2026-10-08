using System;
using System.Globalization;

namespace CPMCore.Models.Budget
{
    /// <summary>Eén getalveld van de gl-v2-budgetflow (label, waarde, eenheid-blok rechts, optioneel prefix). Gerenderd door <c>V2/_BwNum</c>.
    /// Waarden gaan nl-BE (komma) naar de browser; <c>FlexibleDecimalModelBinder</c> leest komma én punt terug.</summary>
    public class BwNumVm
    {
        public string Name { get; set; } = "";
        public string Id { get; set; }
        public string Label { get; set; } = "";
        public decimal? Value { get; set; }
        public string Unit { get; set; }
        public string Prefix { get; set; }
        public int Decimals { get; set; } = 2;
        public string Help { get; set; }
        public string Placeholder { get; set; } = "0";
        public bool Required { get; set; }
        public bool Integer => Decimals == 0;
        /// <summary>Extra attributen op de input (bv. data-bw-live).</summary>
        public string Attrs { get; set; }

        public string Text => Value.HasValue ? Value.Value.ToString("F" + Decimals, new CultureInfo("nl-BE")) : "";
        public string ElementId => Id ?? Name.Replace(".", "_").Replace("[", "_").Replace("]", "_");

        public static BwNumVm Maak(string name, string label, decimal? value, string unit = null, int decimals = 2, string prefix = null, string help = null)
            => new() { Name = name, Label = label, Value = value, Unit = unit, Decimals = decimals, Prefix = prefix, Help = help };
    }
}
