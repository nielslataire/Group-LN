using BOCore;
using CPMCore.Models.Invoicing;
using CPMCore.Services.Peppol;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore;
using ServiceCore.Invoicing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>
    /// gl-v2 voor "Nieuwe factuur" (Invoices/CreateV2, design-handoff punt 47) en de schermen "nummeren en verzenden" en "boeken"
    /// (punt 48, voortgangsoverlay op Invoices/Index en op CreateV2). Enkel NIEUWE acties die JSON teruggeven: de bestaande acties
    /// (Save, Issue, BookInvoices, ResendOctopus) blijven bestaan voor het klassieke scherm en delen met deze acties de methodes
    /// IssueInvoiceCoreAsync en BookInvoicesCoreAsync, zodat nummeren en boeken op één plaats staan.
    /// </summary>
    public partial class InvoicesController
    {
        private JsonResult FlowError(string message) => Json(new { ok = false, error = message });

        // ───────────── Nieuwe factuur: concept bewaren (47d "Opslaan als draft" en "Opslaan en nummeren") ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> SaveDraftV2(InvoiceComposeVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(vm.IssuerCompanyId))
                return FlowError("Je hebt geen rechten om facturen voor dit facturatiebedrijf aan te maken.");

            try
            {
                var bo = await BuildInvoiceDraftBoAsync(vm, ct);
                // Altijd eerst een concept: nummeren gebeurt daarna apart (IssueV2), zodat het scherm elke stap kan tonen.
                var (invoiceId, _) = await _cmd.CreateWithLinesAsync(bo, issueNow: false, ct);
                await SavePdfAppendixAsync(invoiceId, vm.PdfAppendix, ct);
                return Json(new { ok = true, invoiceId, issuerCompanyId = vm.IssuerCompanyId });
            }
            catch (InvalidOperationException ex)
            {
                return FlowError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveDraftV2 failed for issuer {IssuerCompanyId}", vm.IssuerCompanyId);
                return FlowError("De factuur kon niet bewaard worden.");
            }
        }

        // ───────────── Nummeren (48c–f) ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> IssueV2(int id, string? issueDate = null, CancellationToken ct = default)
        {
            var invoiceIssuerId = await _db.Invoices.AsNoTracking().Where(i => i.Id == id).Select(i => (int?)i.IssuerCompanyId).FirstOrDefaultAsync(ct);
            if (invoiceIssuerId == null)
                return FlowError("Factuur niet gevonden.");

            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(invoiceIssuerId.Value))
                return FlowError("Je hebt geen rechten om facturen voor dit facturatiebedrijf te nummeren.");

            // Uit de lijst: vandaag (zoals Issue). Vanuit "Nieuwe factuur": de gekozen factuurdatum (zoals het klassieke "Start als factuur").
            var date = DateOnly.FromDateTime(DateTime.Today);
            if (!string.IsNullOrWhiteSpace(issueDate) && DateOnly.TryParseExact(issueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                date = parsed;

            var result = await IssueInvoiceCoreAsync(id, date, ct);
            if (!result.Ok)
                return FlowError(result.Error ?? "Factuur kon niet definitief gemaakt worden.");

            return Json(await BuildIssueSummaryAsync(id, result, ct));
        }

        // Peppol opnieuw proberen (48f): dezelfde stap als ResendOctopus, maar met een JSON-antwoord.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> ResendPeppolV2(int id, CancellationToken ct = default)
        {
            var invoiceIssuerId = await _db.Invoices.AsNoTracking().Where(i => i.Id == id).Select(i => (int?)i.IssuerCompanyId).FirstOrDefaultAsync(ct);
            if (invoiceIssuerId == null)
                return FlowError("Factuur niet gevonden.");

            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(invoiceIssuerId.Value))
                return FlowError("Je hebt geen rechten om facturen voor dit facturatiebedrijf te versturen.");

            try
            {
                await SendInvoiceToOctopusAsync(id, ct, sendOnly: true, forceSendStep: true);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Octopus resend blocked for invoice {InvoiceId}", id);
                return FlowError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Octopus resend failed for invoice {InvoiceId}", id);
                return FlowError("Factuur kon niet opnieuw via Octopus verzonden worden.");
            }

            return Json(await BuildIssueSummaryAsync(id, new IssueFlowResult { Ok = true }, ct));
        }

        /// <summary>De stappen van het resultaatscherm (48e/48f), uit wat er echt in de database staat na het nummeren.</summary>
        private async Task<object> BuildIssueSummaryAsync(int id, IssueFlowResult result, CancellationToken ct)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            var state = (detail?.OctopusDeliveryState ?? string.Empty).Trim().ToUpperInvariant();
            var peppol = state switch
            {
                "DELIVERED" => "delivered",
                "ERROR" or "PEPPOL_SEND_FAILED" => "failed",
                "PEPPOL_SENDING_IN_PROGRESS" => "pending",
                _ => "none"
            };

            string? peppolId = null;
            var lookupId = detail?.ClientVatNumber ?? detail?.ClientEnterpriseNumber;
            if (peppol != "none" && !string.IsNullOrWhiteSpace(lookupId))
            {
                try { peppolId = (await _peppolDirectory.FindParticipantAsync(lookupId, ct))?.ParticipantId; }
                catch (Exception ex) { _logger.LogWarning(ex, "Peppol-controle mislukt voor factuur {InvoiceId}", id); }
            }

            return new
            {
                ok = true,
                invoiceId = id,
                issuerCompanyId = detail?.IssuerCompanyId,
                publicId = string.IsNullOrWhiteSpace(detail?.PublicId) ? result.PublicId : detail!.PublicId,
                structuredMessage = detail?.StructuredMessage,
                clientName = detail?.ClientName,
                clientEmail = detail?.ClientEmail,
                total = detail?.TotalInclVat,
                dueDate = detail?.ExpirationDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                peppol,
                peppolId,
                peppolComment = detail?.OctopusDeliveryComment,
                email = result.EmailSent ? "sent" : result.EmailFailed ? "failed" : "none",
                // Het doorsturen naar Octopus is onderdeel van het nummeren: lukt het niet, dan is er geen nummer.
                octopus = true
            };
        }

        // ───────────── Boeken (48a/b) ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> BookInvoicesV2(int issuerCompanyId, int[] invoiceIds, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(issuerCompanyId))
                return FlowError("Je hebt geen rechten om facturen voor dit facturatiebedrijf te boeken.");

            var result = await BookInvoicesCoreAsync(issuerCompanyId, invoiceIds ?? Array.Empty<int>(), ct);
            if (!result.Ok)
                return FlowError(result.Error ?? "Facturen konden niet geboekt worden.");

            return Json(new { ok = true, from = result.From, until = result.Until, count = result.Count, requested = invoiceIds?.Length ?? 0 });
        }

        // ───────────── Nieuwe factuur: lijnen als JSON (47c "Uit openstaande posten" en "Uit offerte") ─────────────
        [HttpGet]
        public async Task<IActionResult> ComposeStageLinesJson(int clientId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0) return Json(new { lines = Array.Empty<object>() });
                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId, false, null, ct);
                if (rows == null) return Json(new { lines = Array.Empty<object>() });
                var lines = rows.Where(r => r.Invoicable && r.CalculatedAmount > 0m)
                    .OrderBy(r => r.UnitName).ThenBy(r => r.GroupName).ThenBy(r => r.StageId)
                    .Select(r => new
                    {
                        stageId = r.StageId,
                        unitId = r.UnitId,
                        text = r.StageName,
                        unitName = r.UnitName,
                        groupName = r.GroupName,
                        amount = r.CalculatedAmount,
                        vat = 21m
                    }).ToList();
                return Json(new { lines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ComposeStageLinesJson failed");
                return Json(new { lines = Array.Empty<object>(), error = "Kon de openstaande posten niet laden." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ComposeChangeOrderLinesJson(int clientId, int? projectId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0) return Json(new { lines = Array.Empty<object>() });
                var rows = await _ps.GetApprovedChangeOrdersForClientAsync(clientId, projectId, ct);
                if (rows == null) return Json(new { lines = Array.Empty<object>() });
                var lines = rows.Select(r => new
                {
                    changeOrderId = r.ChangeOrderId,
                    detailId = r.ChangeOrderDetailId,
                    unitId = r.UnitId,
                    text = r.Title,
                    unitName = r.UnitName,
                    groupName = r.ChangeOrderDescription ?? $"Wijzigingsopdracht #{r.ChangeOrderId}",
                    baseAmount = r.BaseAmountExcl,
                    vat = r.VatPercentage
                }).ToList();
                return Json(new { lines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ComposeChangeOrderLinesJson failed");
                return Json(new { lines = Array.Empty<object>(), error = "Kon de wijzigingsopdrachten niet laden." });
            }
        }

        // ───────────── Nieuwe factuur: klantkaart (47a) ─────────────
        /// <summary>Recente klanten/leveranciers van dit facturatiebedrijf (de laatste facturen), voor de snelkeuze onder het zoekveld.</summary>
        [HttpGet]
        public async Task<IActionResult> RecentPartiesV2(int issuerId, int take = 4, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAccess || (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(issuerId)))
                return Json(new { results = Array.Empty<object>() });

            var rows = await _db.Invoices.AsNoTracking()
                .Where(i => i.IssuerCompanyId == issuerId && i.ClientName != null && (i.ClientId != null || i.CompanyId != null))
                .OrderByDescending(i => i.Id)
                .Select(i => new { i.ClientType, i.ClientId, i.CompanyId, i.ClientName })
                .Take(200)
                .ToListAsync(ct);

            var results = rows
                .Select(r => r.CompanyId != null
                    ? new { id = "su:" + r.CompanyId, text = r.ClientName!, type = nameof(InvoicePartyType.Supplier) }
                    : r.ClientType == 2
                        ? new { id = "cc:" + r.ClientId, text = r.ClientName!, type = nameof(InvoicePartyType.ClientContact) }
                        : new { id = "ca:" + r.ClientId, text = r.ClientName!, type = nameof(InvoicePartyType.ClientAccount) })
                .GroupBy(r => r.id).Select(g => g.First())
                .Take(Math.Clamp(take, 1, 10))
                .ToList();
            return Json(new { results });
        }

        /// <summary>E-mailadres en Peppol-status van een klant/leverancier voor de klantkaart (47a) en de verzendkanalen (47b).</summary>
        [HttpGet]
        public async Task<IActionResult> PartyDeliveryV2(string id, CancellationToken ct = default)
        {
            var parts = (id ?? string.Empty).Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var numId)) return Json(null);

            string? email = null, vat = null, enterprise = null;
            var requiresDigital = false;
            switch (parts[0].ToLowerInvariant())
            {
                case "ca":
                    var ca = await _db.ClientAccount.AsNoTracking().Where(x => x.Id == numId)
                        .Select(x => new { Email = x.InvoiceEmail != null && x.InvoiceEmail != "" ? x.InvoiceEmail : x.Email, x.Vatnumber, x.RequiresDigitalInvoice })
                        .FirstOrDefaultAsync(ct);
                    if (ca != null) (email, vat, requiresDigital) = (ca.Email, ca.Vatnumber, ca.RequiresDigitalInvoice);
                    break;
                case "cc":
                    var cc = await _db.ClientContacts.AsNoTracking().Where(x => x.Id == numId)
                        .Select(x => new { Email = x.InvoiceEmail != null && x.InvoiceEmail != "" ? x.InvoiceEmail : x.Email, x.Vatnumber, x.RequiresDigitalInvoice })
                        .FirstOrDefaultAsync(ct);
                    if (cc != null) (email, vat, requiresDigital) = (cc.Email, cc.Vatnumber, cc.RequiresDigitalInvoice);
                    break;
                case "su":
                    var su = await _db.CompanyInfo.AsNoTracking().Where(x => x.CompanyId == numId)
                        .Select(x => new { Email = x.InvoiceEmail != null && x.InvoiceEmail != "" ? x.InvoiceEmail : x.Email, x.VatNumber, x.Ondernemingsnummer, x.RequiresDigitalInvoice })
                        .FirstOrDefaultAsync(ct);
                    if (su != null) (email, vat, enterprise, requiresDigital) = (su.Email, su.VatNumber, su.Ondernemingsnummer, su.RequiresDigitalInvoice);
                    break;
                default:
                    return Json(null);
            }

            // Zelfde controle als op het verzendscherm (CreateSendViewModelAsync): openbare Peppol-directory, enkel informatief.
            string? peppolId = null;
            var lookupId = !string.IsNullOrWhiteSpace(vat) ? vat : enterprise;
            if (!string.IsNullOrWhiteSpace(lookupId))
            {
                try
                {
                    var participant = await _peppolDirectory.FindParticipantAsync(lookupId!, ct);
                    if (participant == null && !string.IsNullOrWhiteSpace(enterprise) && enterprise != lookupId)
                        participant = await _peppolDirectory.FindParticipantAsync(enterprise!, ct);
                    peppolId = participant?.ParticipantId;
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Peppol-controle mislukt voor {PartyId}", id); }
            }

            return Json(new { email, peppolId, requiresDigital });
        }

        /// <summary>Gegevens van het facturatiebedrijf voor het live voorbeeld op CreateV2 (logo, huiskleur, adres, contact): dezelfde bronnen als de pdf-kop.</summary>
        [HttpGet]
        public async Task<IActionResult> IssuerSheetV2(int issuerId, CancellationToken ct = default)
        {
            if (issuerId <= 0) return Json(null);
            var i = await _db.IssuerCompany.AsNoTracking().Include(x => x.CompanyLegalForm).FirstOrDefaultAsync(x => x.Id == issuerId, ct);
            if (i == null) return Json(null);
            string? logo = null;
            if (i.LogoBytes != null && i.LogoBytes.Length > 0)
            {
                var b = i.LogoBytes;
                var mime = b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 ? "image/jpeg" : b.Length > 3 && b[0] == 0x3C ? "image/svg+xml" : "image/png";
                logo = "data:" + mime + ";base64," + Convert.ToBase64String(b);
            }
            return Json(new
            {
                name = i.Name,
                legalName = string.IsNullOrWhiteSpace(i.LegalName) ? i.Name : i.LegalName,
                legalForm = i.CompanyLegalForm?.Abbreviation,
                addressLine1 = i.AddressLine1,
                addressLine2 = i.AddressLine2,
                postalCode = i.PostalCode,
                city = i.City,
                phone = i.Phone,
                email = i.Email,
                website = i.Website,
                vat = i.VatNumber,
                enterpriseNumber = i.EnterpriseNumber,
                color = i.BrandPrimaryColor,
                logo
            });
        }

        // ───────────── Concept bewerken (EditDraft in de gl-v2-layout) ─────────────
        /// <summary>Zelfde controles en stappen als UpdateDraft (rechten, enkel een concept, BuildInvoiceDraftBoAsync, UpdateDraftAsync, bijlage), met een JSON-antwoord voor het gl-v2-scherm.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> UpdateDraftV2(InvoiceDraftEditVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAccess)
                return FlowError("Je hebt geen rechten om facturen te bewerken.");
            if (vm == null || vm.InvoiceId <= 0)
                return FlowError("Factuur niet gevonden.");

            var detail = await _invoices.GetDetailAsync(vm.InvoiceId, ct);
            if (detail == null)
                return FlowError("Factuur niet gevonden.");
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId))
                return FlowError("Je hebt geen rechten om facturen voor dit facturatiebedrijf te bewerken.");
            if (!IsDraftStatus(detail.StatusName))
                return FlowError("Deze factuur is niet langer een concept en kan niet volledig bewerkt worden.");

            try
            {
                var bo = await BuildInvoiceDraftBoAsync(vm, ct);
                await _cmd.UpdateDraftAsync(vm.InvoiceId, bo, ct);
                await SavePdfAppendixAsync(vm.InvoiceId, vm.PdfAppendix, ct, vm.RemovePdfAppendix);
                return Json(new { ok = true, invoiceId = vm.InvoiceId, issuerCompanyId = detail.IssuerCompanyId, returnUrl = DetermineReturnUrl(vm.ReturnUrl) });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update draft invoice {InvoiceId} blocked", vm.InvoiceId);
                return FlowError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update draft invoice {InvoiceId} failed", vm.InvoiceId);
                return FlowError("Conceptfactuur kon niet bijgewerkt worden.");
            }
        }

        // ───────────── Nieuwe klant vanuit de factuur (47e–g) ─────────────
        /// <summary>Peppol-controle op een ondernemingsnummer (zelfde openbare directory als op het verzendscherm).</summary>
        [HttpGet]
        public async Task<IActionResult> PeppolCheckV2(string number, CancellationToken ct = default)
        {
            var digits = new string((number ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length < 9) return Json(new { found = false });
            var id = "BE" + (digits.Length == 9 ? "0" + digits : digits);
            try
            {
                var participant = await _peppolDirectory.FindParticipantAsync(id, ct);
                return Json(new { found = participant != null, participantId = participant?.ParticipantId, name = participant?.Name });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Peppol-controle mislukt voor {Number}", number);
                return Json(new { found = false, failed = true });
            }
        }

        /// <summary>Landen voor het postcodezoekveld van de nieuwe-klantmodal.</summary>
        [HttpGet]
        public async Task<IActionResult> NewPartyCountriesV2(CancellationToken ct = default)
        {
            var countries = await _db.Country.AsNoTracking().Where(c => c.Selectable).OrderBy(c => c.LandNaam)
                .Select(c => new { id = c.Id, name = c.LandNaam, iso = c.LandIsocode }).ToListAsync(ct);
            return Json(new { countries });
        }

        // ───────────── Zoeklijsten voor het projectbrede combo-component (GlV2/_GlV2Combo, punt 34) ─────────────
        // Het combo POST {term, countryId} en verwacht [{id, text, sub}]. Het "countryId" is hier de klant of leverancier uit stap 1
        // (data-country-source), zodat het project of contract bij die partij past. Zelfde bronnen als PartyLookup, ProjectLookup en SupplierContractLookup.
        [HttpPost]
        public async Task<IActionResult> PartyComboV2(string? term, CancellationToken ct = default)
        {
            var rows = await _lookup.SearchPartiesAsync(term ?? "", 20, ct);
            var results = rows.Select(x => new
            {
                id = x.Type switch
                {
                    InvoicePartyType.ClientAccount => $"ca:{x.Id}",
                    InvoicePartyType.ClientContact => $"cc:{x.Id}",
                    InvoicePartyType.Supplier => $"su:{x.Id}",
                    _ => $"x:{x.Id}"
                },
                text = string.IsNullOrWhiteSpace(x.DisplayName) ? x.Name : x.DisplayName,
                sub = x.Hint
            });
            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> ProjectComboV2(string? term, int? countryId, CancellationToken ct = default)
        {
            try
            {
                int? resolvedClientId = null;
                if (countryId.HasValue && countryId.Value > 0)
                {
                    var contactAccountId = await _db.ClientContacts.Where(c => c.Id == countryId.Value).Select(c => (int?)c.ClientAccountId).FirstOrDefaultAsync(ct);
                    resolvedClientId = contactAccountId ?? countryId.Value;
                }
                var rows = await _ps.SearchProjectsAsync(term ?? "", resolvedClientId, 20, ct);
                return Json(rows.Select(x => new { id = x.Id, text = x.Name }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProjectComboV2 failed");
                return Json(Array.Empty<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ContractComboV2(string? term, int? countryId, CancellationToken ct = default)
        {
            try
            {
                if (countryId is null || countryId <= 0) return Json(Array.Empty<object>());
                var rows = await _ps.SearchSupplierContractsAsync(term ?? "", countryId, 20, ct);
                return Json(rows.Select(x => new { id = x.Id, text = x.Name }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ContractComboV2 failed");
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjectNameV2(int id, CancellationToken ct = default)
        {
            var name = await _db.Project.AsNoTracking().Where(p => p.ProjectId == id).Select(p => p.ProjectName).FirstOrDefaultAsync(ct);
            return Json(new { id, name });
        }
    }
}
