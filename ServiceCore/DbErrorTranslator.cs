using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ServiceCore
{
    /// <summary>Vertaalt een rauwe databasefout (SqlException binnen DbUpdateException) naar een Nederlandse
    /// boodschap die een gebruiker kan lezen. Bedoeld voor elke "verwijderen/opslaan"-service die anders
    /// een onverwerkte exception laat ontsnappen (bv. "The DELETE statement conflicted with the REFERENCE
    /// constraint FK_Invoices_ClientAccount").</summary>
    public static class DbErrorTranslator
    {
        private static readonly Dictionary<string, string> TableLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Invoices"] = "facturen",
            ["InvoicesDetails"] = "factuurregels",
            ["Units"] = "eenheden",
            ["ClientContacts"] = "contactpersonen of mede-eigenaars",
            ["ClientAccount"] = "klanten",
            ["ChangeOrder"] = "wijzigingsopdrachten",
            ["ChangeOrderPaymentTerm"] = "betaalschijven van wijzigingsopdrachten",
            ["InvoicingPaymentStages"] = "betalingsschijven",
            ["InvoicingPaymentGroup"] = "betalingsgroepen",
            ["UnitPaymentStageReached"] = "bereikte betalingsschijven",
            ["SigningCase"] = "ondertekendossiers",
            ["Project"] = "projecten",
            ["ProjectDocs"] = "documenten",
            ["DocumentLinks"] = "documentkoppelingen",
        };

        public static string ToFriendlyMessage(Exception ex, string action = "verwijderd")
        {
            var raw = ex?.GetBaseException().Message ?? "";

            if (raw.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(raw, "table \"(?:dbo\\.)?(?<t>[^\"]+)\"", RegexOptions.IgnoreCase);
                var table = m.Success ? m.Groups["t"].Value : null;
                var label = table != null && TableLabels.TryGetValue(table, out var l) ? l : (table != null ? $"gegevens in “{table}”" : "andere gegevens");
                return $"Dit kan niet {action} worden: er zijn nog {label} aan gekoppeld. Verwijder of ontkoppel die eerst.";
            }
            if (raw.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) || raw.Contains("UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase))
                return "Dit bestaat al: een waarde die uniek moet zijn komt dubbel voor.";
            if (raw.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase))
                return "Dit verwijst naar gegevens die niet (meer) bestaan. Laad de pagina opnieuw en probeer nogmaals.";
            if (raw.Contains("String or binary data would be truncated", StringComparison.OrdinalIgnoreCase))
                return "Een van de ingevulde waarden is te lang.";
            if (raw.Contains("deadlock", StringComparison.OrdinalIgnoreCase) || raw.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "De database was bezet. Probeer het over enkele seconden opnieuw.";
            return "De bewerking kon niet worden uitgevoerd door een databasefout. Probeer opnieuw of contacteer de administrator.";
        }
    }
}
