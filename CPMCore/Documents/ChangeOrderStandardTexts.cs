using System;
using System.Globalization;

namespace CPMCore.Documents
{
    /// <summary>Vaste teksten voor offertes en wijzigingsopdrachten.</summary>
    public static class ChangeOrderStandardTexts
    {
        private const string QuotePrefix = "Deze offerte is geldig tot ";
        private const string QuoteSuffix = ". Indien u akkoord bent met de offerte geef ons dan een bericht en dan maken wij de wijzigingsopdracht op die u voor akkoord dient te ondertekenen.";

        /// <summary>Standaardvoorwaarden van een offerte, opgemaakt uit de vervaldatum (Niels, 2026-10-06).</summary>
        public static string QuoteConditions(DateOnly expiration) =>
            QuotePrefix + expiration.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + QuoteSuffix;

        /// <summary>Leeg of nog exact de standaardtekst (met om het even welke datum): mag dan mee veranderen met de vervaldatum.</summary>
        public static bool IsQuoteStandardOrEmpty(string? text) =>
            string.IsNullOrWhiteSpace(text)
            || (text.StartsWith(QuotePrefix, StringComparison.Ordinal) && text.EndsWith(QuoteSuffix, StringComparison.Ordinal)
                && text.Length == QuotePrefix.Length + 10 + QuoteSuffix.Length);
    }
}
