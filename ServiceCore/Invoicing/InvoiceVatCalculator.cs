using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore.Invoicing
{
    /// <summary>
    /// Btw wordt berekend per tarief op de maatstaf van heffing (som van de nettobedragen
    /// van alle lijnen met dat tarief), niet als som van per lijn afgeronde bedragen.
    /// Zo is het btw-totaal altijd gelijk aan tarief × maatstaf (EN 16931 / Peppol BR-CO-17)
    /// en sluit het aan bij de Octopus-boeking en de EPC-QR-code.
    /// Per-lijn bedragen worden cumulatief toegewezen zodat hun som exact het
    /// tarieftotaal oplevert; een afrondingsverschil van 1 cent schuift daardoor
    /// naar de laatste lijn binnen het tarief in plaats van het totaal te vervuilen.
    /// </summary>
    public static class InvoiceVatCalculator
    {
        /// <summary>Btw-totaal over alle lijnen: per tarief afgerond op de gesommeerde maatstaf.</summary>
        public static decimal CalculateTotalVat(IEnumerable<(decimal Net, decimal Rate)> lines)
        {
            if (lines == null) return 0m;

            return lines
                .GroupBy(l => l.Rate)
                .Sum(g => RoundVat(g.Sum(l => l.Net), g.Key));
        }

        /// <summary>
        /// Wijs per lijn een btw-bedrag toe (zelfde volgorde als de input) zodat de som per
        /// tarief exact gelijk is aan het op de maatstaf berekende tarieftotaal.
        /// </summary>
        public static decimal[] AllocateLineVat(IReadOnlyList<(decimal Net, decimal Rate)> lines)
        {
            if (lines == null) return Array.Empty<decimal>();

            var result = new decimal[lines.Count];
            var running = new Dictionary<decimal, (decimal Base, decimal Allocated)>();

            for (var i = 0; i < lines.Count; i++)
            {
                var (net, rate) = lines[i];
                running.TryGetValue(rate, out var state);

                var newBase = state.Base + net;
                var target = RoundVat(newBase, rate);
                result[i] = target - state.Allocated;
                running[rate] = (newBase, target);
            }

            return result;
        }

        private static decimal RoundVat(decimal taxable, decimal rate) =>
            Math.Round(taxable * (rate / 100m), 2, MidpointRounding.AwayFromZero);
    }
}
