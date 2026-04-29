using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.IncomingInvoices
{
    /// <summary>
    /// Stelt een kostenrekening / grootboekrekening voor op basis van context.
    ///
    /// Prioriteit:
    ///   1. Leverancierregel met DefaultAccountingCode (meest specifiek)
    ///   2. Omschrijvingspatronen (architect → erelonen, nutsbedrijf → nutsvoorzieningen, ...)
    ///   3. Algemene kost als fallback indien geen projectmatch
    /// </summary>
    public class AccountingSuggestionService : IAccountingSuggestionService
    {
        private readonly cpmRunningContext _db;
        private readonly ILogger<AccountingSuggestionService> _logger;

        public AccountingSuggestionService(cpmRunningContext db, ILogger<AccountingSuggestionService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<AccountingSuggestion> SuggestAsync(
            int? companyId,
            int? projectId,
            int? issuerCompanyId,
            string description,
            bool isGeneralCostContext,
            CancellationToken ct = default)
        {
            // ── 1. Leverancierregel met expliciete rekening ────────────────────
            if (companyId.HasValue || issuerCompanyId.HasValue)
            {
                var rule = await _db.InvoiceSupplierRule
                    .Where(r => r.IsActive
                             && !string.IsNullOrEmpty(r.DefaultAccountingCode)
                             && (r.CompanyId == null || r.CompanyId == companyId)
                             && (r.IssuerCompanyId == null || r.IssuerCompanyId == issuerCompanyId))
                    .OrderByDescending(r => r.CompanyId != null)   // meest specifieke regel eerst
                    .FirstOrDefaultAsync(ct);

                if (rule != null)
                {
                    return new AccountingSuggestion
                    {
                        AccountingCode = rule.DefaultAccountingCode,
                        Description = BuildDescription(rule.DefaultAccountingCode),
                        Source = "SupplierRule",
                        IsGeneralCost = rule.IsGeneralCost || !projectId.HasValue,
                        Confidence = "High"
                    };
                }
            }

            // ── 2. Omschrijvingspatronen ──────────────────────────────────────
            var fromDescription = MatchByDescription(description, projectId.HasValue);
            if (fromDescription != null)
                return fromDescription;

            // ── 3. Fallback: algemene kost ────────────────────────────────────
            return new AccountingSuggestion
            {
                AccountingCode = projectId.HasValue ? "604000" : "610000",
                Description = projectId.HasValue ? "Projectgebonden kosten" : "Algemene bedrijfskosten",
                Source = "Fallback",
                IsGeneralCost = !projectId.HasValue || isGeneralCostContext,
                Confidence = "Low"
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static AccountingSuggestion MatchByDescription(string description, bool hasProject)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var desc = description.ToLowerInvariant();

            if (ContainsAny(desc, "architect", "ontwerp", "studie", "ingenieur", "bureau", "ereloon"))
                return Make("604100", "Erelonen en studiekosten", hasProject);

            if (ContainsAny(desc, "elektriciteit", "gas", "water", "nuts", "engie", "fluvius", "pidpa"))
                return Make("605000", "Nutsvoorzieningen", hasProject);

            if (ContainsAny(desc, "verzekering", "assurance", "polis"))
                return Make("635000", hasProject ? "Projectverzekering" : "Verzekeringen", hasProject);

            if (ContainsAny(desc, "notaris", "akte", "registratie", "kadaster"))
                return Make("634000", "Notariskosten", hasProject);

            if (ContainsAny(desc, "huur", "verhuur", "kraan", "stelling", "scaffold", "container"))
                return Make("612000", "Huur materieel", hasProject);

            if (ContainsAny(desc, "keuken", "sanitair", "bad", "douche", "badkamer"))
                return Make("604200", "Afwerking — sanitair / keuken", hasProject);

            if (ContainsAny(desc, "vloe", "parket", "tegel", "vloer"))
                return Make("604300", "Afwerking — vloer", hasProject);

            if (ContainsAny(desc, "ruwbouw", "fundering", "beton", "staal", "metselwerk"))
                return Make("604400", "Ruwbouwwerken", hasProject);

            if (ContainsAny(desc, "elektric", "hvac", "sanitair leidingen", "verwarming", "klimaat"))
                return Make("604500", "Technische installaties", hasProject);

            if (ContainsAny(desc, "marketing", "reclame", "publiciteit", "affiche", "brochure"))
                return Make("614000", "Marketing en publiciteit", hasProject);

            if (ContainsAny(desc, "transport", "levering", "vracht", "logistiek"))
                return Make("612100", "Transport en levering", hasProject);

            return null;
        }

        private static AccountingSuggestion Make(string code, string description, bool hasProject) =>
            new AccountingSuggestion
            {
                AccountingCode = code,
                Description = description,
                Source = "Description",
                IsGeneralCost = !hasProject,
                Confidence = "Medium"
            };

        private static bool ContainsAny(string text, params string[] keywords) =>
            keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

        private static string BuildDescription(string code) =>
            code switch
            {
                "604000" => "Projectgebonden kosten",
                "604100" => "Erelonen en studiekosten",
                "605000" => "Nutsvoorzieningen",
                "610000" => "Algemene bedrijfskosten",
                "612000" => "Huur materieel",
                "614000" => "Marketing en publiciteit",
                "634000" => "Notariskosten",
                "635000" => "Verzekeringen",
                _ => code
            };
    }
}
