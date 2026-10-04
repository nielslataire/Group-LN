using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using CPMCore.Services.Peppol;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using ServiceCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using SmartBreadcrumbs.Nodes;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

namespace CPMCore.Controllers
{
    /// <summary>Factuur opmaken: definitief maken, aanmaken, ontwerp bewaren/bewerken, lijnen samenstellen, opzoekingen voor het formulier. Opgesplitst uit InvoicesController.cs (okt. 2026, structureren) - views in Views/Invoices/Editor/. Zelfde partial class: alle private velden/services van InvoicesController.cs blijven gewoon bruikbaar.</summary>
    public partial class InvoicesController
    {
        //VAN DRAFT NAAR DEFINITIEF
        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Issue(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(issuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te nummeren.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            // ISSUE FLOW (van draft naar definitief + verzending via Octopus)
            // Volgorde:
            // 1) Verzend eerst via Octopus met de voorziene nummering en payload.
            // 2) Zet pas daarna definitief in eigen database (IssueDraftAsync).
            // 3) Verstuur klassieke e-mail op basis van de definitief opgeslagen databasegegevens.
            // Opmerking: we forceren in de PDF geen proforma-label tijdens issue.
            var invoice = await _db.Invoices
               .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (invoice == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            if (InvoiceStatusExtensions.FromId(invoice.StatusId) == InvoiceStatus.Generating)
            {
                AddMessage("error", "Deze factuur wordt momenteel gegenereerd. Probeer later opnieuw.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            var previousStatusId = invoice.StatusId;
            invoice.StatusId = (byte)InvoiceStatus.Generating;
            await _db.SaveChangesAsync(ct);
            try
            {
                var issueDate = DateOnly.FromDateTime(DateTime.Today);
                await SendInvoiceToOctopusAsync(id, ct, forceFinalPdf: true, skipClassicEmail: true, forcedIssueDate: issueDate);
                var publicId = await _cmd.IssueDraftAsync(id, issueDate: issueDate, ct: ct);

                await TrySendClassicEmailAfterIssueAsync(id, ct);
                if (!string.IsNullOrWhiteSpace(publicId))
                    AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
                else
                    AddMessage("success", "Factuur definitief gemaakt.", "Factuur");
            }
            catch (InvalidOperationException ex)
            {
                await RestoreDraftStateAsync(id, previousStatusId, ct);
                _logger.LogWarning(ex, "Issue invoice {InvoiceId} blocked", id);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                await RestoreDraftStateAsync(id, previousStatusId, ct);
                _logger.LogError(ex, "Issue invoice {InvoiceId} failed", id);
                AddMessage("error", "Factuur kon niet definitief gemaakt worden.", "Factuur");
            }

            return RedirectToAction(nameof(Index), new { issuerCompanyId });
        }

        // CREATE (GET)
        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Create(int? issuerId = null, int? duplicateInvoiceId = null, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om facturen aan te maken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            InvoiceDetailBO? duplicateDetail = null;
            if (duplicateInvoiceId.HasValue && duplicateInvoiceId.Value > 0)
            {
                duplicateDetail = await _invoices.GetDetailAsync(duplicateInvoiceId.Value, ct);
                if (duplicateDetail == null)
                {
                    AddMessage("error", "Factuur niet gevonden om te dupliceren.", "Factuur");
                }
            }
            //coachmarks
            ViewData["CoachmarkPageKey"] = "Invoices.Create";

            // haal actieve issuers op, gefilterd op schrijfrechten
            var allIssuersBo = await _ics.ListActiveIssuersAsync(ct);
            var issuersBo = writeScope.HasAllIssuers
                ? allIssuersBo
                : allIssuersBo.Where(i => writeScope.AllowedIssuerIds.Contains(i.Id)).ToList();

            // gekozen issuer (param of eerste toegestane)
            var selectedIssuerId = duplicateDetail?.IssuerCompanyId
                ?? issuerId
                ?? issuersBo.FirstOrDefault()?.Id
                ?? 0;

            // als gekozen issuer niet toegestaan is, val terug op eerste toegestane
            if (!writeScope.HasAllIssuers && selectedIssuerId > 0 && !writeScope.AllowedIssuerIds.Contains(selectedIssuerId))
                selectedIssuerId = issuersBo.FirstOrDefault()?.Id ?? 0;
            var termsBo = selectedIssuerId > 0
                ? await _ics.ListPaymentTermsAsync(selectedIssuerId, ct)
                : Array.Empty<PaymentTermBO>();

            var VatsBo = selectedIssuerId > 0
               ? await _ics.ListVatTypeAsync(selectedIssuerId, ct)
               : Array.Empty<VatTypeBO>();

            var accountsBo = selectedIssuerId > 0
                ? await _bank.ListByIssuerAsync(selectedIssuerId, ct)
                : Array.Empty<IssuerBankAccountBO>();

            var defaultAccountId = accountsBo
                .FirstOrDefault(x => x.IsDefault)?.Id
                ?? accountsBo.Select(x => (int?)x.Id).FirstOrDefault();

            // default betaaltermijn afleiden uit issuer
            int? selectedTermId = issuersBo
                .FirstOrDefault(x => x.Id == selectedIssuerId)?
                .DefaultPaymentTermId;

            // default vattype afleiden uit issuer
            int? selectedVatId = issuersBo
                .FirstOrDefault(x => x.Id == selectedIssuerId)?
                .DefaultVatTypeId;


            int? selectedAccountId = defaultAccountId;
            if (duplicateDetail != null && !string.IsNullOrWhiteSpace(duplicateDetail.BankAccount))
            {
                selectedAccountId = accountsBo
                    .FirstOrDefault(a => string.Equals(a.Iban, duplicateDetail.BankAccount, StringComparison.OrdinalIgnoreCase))
                    ?.Id ?? selectedAccountId;
            }

            var vm = new InvoiceComposeVM
            {
                IssuerCompanyId = selectedIssuerId,
                PaymentTermId = duplicateDetail?.PaymentTermId ?? selectedTermId,
                VatTypeId = duplicateDetail?.Lines?.FirstOrDefault()?.VatTypeId ?? selectedVatId,
                IssuerBankAccountId = selectedAccountId,
                // Bij dupliceren altijd vandaag als datum voorstellen, niet de datum van de
                // originele factuur.
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),

                Issuers = issuersBo
                    .Select(i => new IssuerItemVM(i.Id, i.Name, i.DefaultPaymentTermId, i.DefaultVatTypeId))
                    .ToList(),

                PaymentTerms = termsBo
                    .Select(t => new PaymentTermItemVM(t.Id, t.Name, t.Days, t.TermType, t.DisplayMode, t.DisplayText))
                    .ToList(),

                VatTypes = VatsBo
                     .Select(t => new VatTypeVM
                     {
                         Id = t.Id,
                         BasePercentage = t.BasePercentage,
                         Code = t.Code,
                         Description = t.Description,
                         Type = t.Type,
                         DefaultSellBookingAccountNr = t.DefaultSellBookingAccountNr,
                         InvoiceMention = t.InvoiceMention
                     })
                    .ToList(),

                IssuerBankAccounts = accountsBo
                    .Select(a => new SelectListItem
                    {
                        Value = a.Id.ToString(),
                        Text = string.IsNullOrWhiteSpace(a.DisplayName)
                            ? a.Iban
                            : $"{a.DisplayName} ({a.Iban})",
                        Selected = selectedAccountId.HasValue && a.Id == selectedAccountId.Value
                    })
                    .ToList()
            };

            if (duplicateDetail != null)
            {
                vm.Mode = duplicateDetail.InvoiceMode ?? InvoiceMode.Free;
                vm.HeaderDescription = NormalizeMultiline(duplicateDetail.HeaderText);
                vm.DetailDescription = NormalizeMultiline(duplicateDetail.DetailText);
                vm.FooterDescription = NormalizeMultiline(duplicateDetail.ExtraInfo);
                vm.ProjectId = duplicateDetail.ProjectId;
                vm.SupplierContractId = duplicateDetail.SupplierContractId;
                var duplicateIsCreditNote = DetermineCreditNote(duplicateDetail.IsCreditNote, duplicateDetail.StatusName, duplicateDetail.TotalInclVat);
                vm.IsCreditNote = duplicateIsCreditNote;
                vm.IsPrepaid = duplicateDetail.IsPrepaid;
                vm.Lines = MapLinesForCompose(duplicateDetail.Lines, duplicateIsCreditNote);

                if (duplicateDetail.CompanyId.HasValue)
                {
                    vm.PartyType = InvoicePartyType.Supplier;
                    vm.PartyId = duplicateDetail.CompanyId;
                }
                else if (duplicateDetail.ClientType.HasValue && duplicateDetail.ClientId.HasValue)
                {
                    vm.PartyType = duplicateDetail.ClientType.Value switch
                    {
                        1 => InvoicePartyType.ClientAccount,
                        2 => InvoicePartyType.ClientContact,
                        _ => vm.PartyType
                    };
                    vm.PartyId = duplicateDetail.ClientId;
                }

                var initialJson = BuildDuplicateInitialJson(duplicateDetail, vm);
                ViewBag.DuplicateInvoiceJson = initialJson;
            }

            if (selectedIssuerId > 0)
                await SetIssuerViewBagsAsync(selectedIssuerId, ct);
            SetCreateBreadcrumb(selectedIssuerId, ViewBag.CompanyName as string);
            SetPageHeader("bx bx-receipt", "Nieuwe factuur");

            return View(vm);
        }

        // PARTY LOOKUP (AJAX Select2)
        [HttpGet]
        public async Task<IActionResult> PartyLookup(string? term, int take = 20, CancellationToken ct = default)
        {
            var rows = await _lookup.SearchPartiesAsync(term ?? "", take, ct);

            var results = rows.Select(x =>
            {
                var display = string.IsNullOrWhiteSpace(x.DisplayName) ? x.Name : x.DisplayName;

                return new
                {
                    id = x.Type switch
                    {
                        InvoicePartyType.ClientAccount => $"ca:{x.Id}",
                        InvoicePartyType.ClientContact => $"cc:{x.Id}",
                        InvoicePartyType.Supplier => $"su:{x.Id}",
                        _ => $"x:{x.Id}"
                    },
                    text = display,
                    display,
                    name = x.Name,
                    hint = x.Hint,
                    type = x.Type.ToString()
                };
            });

            return Json(new { results });
        }

        // PROJECT LOOKUP (AJAX)
        [HttpGet]
        public async Task<IActionResult> ProjectLookup(string? term, int? clientId, int take = 20, CancellationToken ct = default)
        {
            try
            {
                int? resolvedClientId = null;
                if (clientId.HasValue && clientId.Value > 0)
                {
                    var contactAccountId = await _db.ClientContacts
                        .Where(c => c.Id == clientId.Value)
                        .Select(c => (int?)c.ClientAccountId)
                        .FirstOrDefaultAsync(ct);

                    resolvedClientId = contactAccountId ?? clientId.Value;
                }

                var rows = await _ps.SearchProjectsAsync(term ?? "", resolvedClientId, take, ct);
                var results = rows.Select(x => new { id = x.Id, text = x.Name });
                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProjectLookup failed");
                return Json(new { results = Array.Empty<object>() });
            }
        }

        // SUPPLIER CONTRACT LOOKUP (AJAX)
        [HttpGet]
        public async Task<IActionResult> SupplierContractLookup(string? term, int? supplierCompanyId, int take = 20, CancellationToken ct = default)
        {
            try
            {
                if (supplierCompanyId is null || supplierCompanyId <= 0)
                    return Json(new { results = Array.Empty<object>() });

                var rows = await _ps.SearchSupplierContractsAsync(term ?? "", supplierCompanyId, take, ct);
                var results = rows.Select(x => new { id = x.Id, text = x.Name });
                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SupplierContractLookup failed");
                return Json(new { results = Array.Empty<object>() });
            }
        }

        // STAGE GROUP LOOKUP (AJAX) – groepen voor klant (gebaseerd op diens units)
        [HttpGet]
        public async Task<IActionResult> StageGroupLookup(int? clientId, string? term, int take = 20, CancellationToken ct = default)
        {
            try
            {
                if (clientId is null || clientId <= 0)
                    return Json(new { results = Array.Empty<object>() });

                // deed-guard: alleen resultaten als DateDeedOfSale niet null is
                var hasDeed = await _ps.HasDeedOfSaleAsync(clientId.Value, ct);
                if (!hasDeed)
                    return Json(new { results = Array.Empty<object>() });

                var rows = await _ps.SearchStageGroupsForClientAsync(clientId.Value, term ?? "", take, ct);
                var results = rows.Select(x => new { id = x.Id, text = x.Name });
                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StageGroupLookup failed");
                return Json(new { results = Array.Empty<object>() });
            }
        }

        // STAGES LOOKUP (AJAX) – invocable stages per group
        [HttpGet]
        public async Task<IActionResult> StageLookup(int? groupId, string? term, int take = 50, CancellationToken ct = default)
        {
            try
            {
                if (groupId is null || groupId <= 0)
                    return Json(new { results = Array.Empty<object>() });

                var rows = await _ps.SearchStagesAsync(groupId.Value, term ?? "", take, ct);
                var results = rows.Select(x => new { id = x.Id, text = x.Name });
                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StageLookup failed");
                return Json(new { results = Array.Empty<object>() });
            }
        }

        // GET UNIT STAGES (JSON, blijft ongewijzigd – handig voor andere UI's)
        [HttpGet]
        public async Task<IActionResult> ClientUnitStages(int clientId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0) return Json(new { results = Array.Empty<object>() });

                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId, false, null, ct);
                var grouped = rows
                    .GroupBy(x => new { x.UnitId, x.UnitName })
                    .Select(g => new {
                        unitId = g.Key.UnitId,
                        unitName = g.Key.UnitName,
                        stages = g.Select(s => new {
                            id = s.StageId,
                            name = s.StageName,
                            groupId = s.GroupId,
                            groupName = s.GroupName
                        }).OrderBy(s => s.id).ToList()
                    })
                    .OrderBy(x => x.unitName)
                    .ToList();

                return Json(new { results = grouped });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClientUnitStages failed");
                return Json(new { results = Array.Empty<object>() });
            }
        }

        // ISSUER DEFAULTS (AJAX)

        [HttpGet]
        public async Task<IActionResult> IssuerDefaults(int issuerId, CancellationToken ct = default)
        {
            // Haal issuers via service
            var issuers = await _ics.ListActiveIssuersAsync(ct);

            var issuer = issuers.FirstOrDefault(x => x.Id == issuerId);

            // Als je liever 404 terugstuurt als de issuer niet bestaat:
            // if (issuer == null) return NotFound();

            return Json(new
            {
                defaultPaymentTermId = issuer?.DefaultPaymentTermId,
                defaultVatTypeId = issuer?.DefaultVatTypeId
            });
        }

        [HttpGet]
        public async Task<IActionResult> IssuerBankAccounts(int issuerId, CancellationToken ct = default)
        {
            if (issuerId <= 0)
                return Json(new { results = Array.Empty<object>(), defaultId = (int?)null });

            var accounts = await _bank.ListByIssuerAsync(issuerId, ct);

            var defaultAccount = accounts.FirstOrDefault(a => a.IsDefault) ?? accounts.FirstOrDefault();

            var results = accounts.Select(a => new
            {
                id = a.Id,
                text = string.IsNullOrWhiteSpace(a.DisplayName)
                    ? a.Iban
                    : $"{a.DisplayName} ({a.Iban})",
                isDefault = a.IsDefault
            }).ToList();

            return Json(new
            {
                results,
                defaultId = defaultAccount?.Id
            });
        }


        // ISSUER ADDRESS (voor live preview)
        [HttpGet]
        public async Task<IActionResult> IssuerAddress(int issuerId, CancellationToken ct = default)
        {
            if (issuerId <= 0) return Json(null);
            var issuer = await _ics.GetAsync(issuerId, ct);
            if (issuer == null) return Json(null);
            return Json(new
            {
                name = issuer.Name,
                addressLine1 = issuer.AddressLine1,
                addressLine2 = issuer.AddressLine2,
                postalCode = issuer.PostalCode,
                city = issuer.City,
                countryCode = issuer.CountryCode,
                enterpriseNumber = issuer.EnterpriseNumber
            });
        }

        // PARTY ADDRESS (voor live preview)
        [HttpGet]
        public async Task<IActionResult> PartyDetails(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return Json(null);
            var parts = id.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var numId)) return Json(null);
            var prefix = parts[0].ToLowerInvariant();

            if (prefix == "ca")
            {
                var ca = await _db.ClientAccount.AsNoTracking()
                    .Where(x => x.Id == numId)
                    .Select(x => new {
                        name = !string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName : x.Name,
                        street = x.InvoiceAddress == true ? x.InvoiceStreet : x.Street,
                        houseNumber = x.InvoiceAddress == true ? x.InvoiceHousenumber : x.Housenumber,
                        busNumber = x.InvoiceAddress == true ? x.InvoiceBusnumber : x.Busnumber,
                        postalCode = x.InvoiceAddress == true
                            ? (x.InvoicePostalCode != null ? x.InvoicePostalCode.Postcode : null)
                            : (x.PostalCode != null ? x.PostalCode.Postcode : null),
                        city = x.InvoiceAddress == true
                            ? (x.InvoicePostalCode != null ? x.InvoicePostalCode.Gemeente : null)
                            : (x.PostalCode != null ? x.PostalCode.Gemeente : null),
                        vatNumber = x.Vatnumber,
                        invoiceExtra = x.InvoiceExtra
                    })
                    .FirstOrDefaultAsync(ct);
                return Json(ca);
            }

            if (prefix == "cc")
            {
                var cc = await _db.ClientContacts.AsNoTracking()
                    .Where(x => x.Id == numId)
                    .Select(x => new {
                        name = !string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName : (x.Forename + " " + x.Name).Trim(),
                        street = x.InvoiceAddress == true ? x.InvoiceStreet : x.Street,
                        houseNumber = x.InvoiceAddress == true ? x.InvoiceHousenumber : x.Housenumber,
                        busNumber = x.InvoiceAddress == true ? x.InvoiceBusnumber : x.Busnumber,
                        postalCode = x.InvoiceAddress == true
                            ? (x.InvoicePostalCode != null ? x.InvoicePostalCode.Postcode : null)
                            : (x.PostalCode != null ? x.PostalCode.Postcode : null),
                        city = x.InvoiceAddress == true
                            ? (x.InvoicePostalCode != null ? x.InvoicePostalCode.Gemeente : null)
                            : (x.PostalCode != null ? x.PostalCode.Gemeente : null),
                        vatNumber = x.Vatnumber,
                        invoiceExtra = x.ClientAccount != null ? x.ClientAccount.InvoiceExtra : null
                    })
                    .FirstOrDefaultAsync(ct);
                return Json(cc);
            }

            if (prefix == "su")
            {
                var su = await _db.CompanyInfo.AsNoTracking()
                    .Where(x => x.CompanyId == numId)
                    .Select(x => new {
                        name = x.BedrijfsNaam,
                        street = x.Straat,
                        houseNumber = x.Huisnummer,
                        busNumber = x.Busnummer,
                        postalCode = x.PostCode != null ? x.PostCode.Postcode : x.Postcode,
                        city = x.PostCode != null ? x.PostCode.Gemeente : x.Gemeente,
                        vatNumber = x.VatNumber ?? x.Ondernemingsnummer
                    })
                    .FirstOrDefaultAsync(ct);
                return Json(su);
            }

            return Json(null);
        }

        // LIJNEN VOOR SCHIJVEN AANMAKEN
        [HttpGet]
        public async Task<IActionResult> ComposeStageLines(int clientId, int? projectId, int? invoiceId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0)
                    return PartialView("_StageLinesTable", new List<InvoiceLineVM>());

                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId, false, invoiceId, ct);
                if (rows is null || rows.Count == 0)
                    return PartialView("_StageLinesTable", new List<InvoiceLineVM>());

                const decimal defaultVat = 21m;
                const decimal ownerPct = 100m; // TODO: co-owner aandeel later

                var lines = rows
                    .Where(r => r.Invoicable && r.CalculatedAmount > 0m)
                    .OrderBy(r => r.UnitName).ThenBy(r => r.GroupName).ThenBy(r => r.StageId)
                    .Select(r => new InvoiceLineVM
                    {
                        IsSelected = false,
                        Text = r.StageName,
                        UnitName = r.UnitName,
                        StagePercentage = r.StagePercentage,
                        Price = r.CalculatedAmount,
                        VatPercentage = defaultVat,
                        UnitId = r.UnitId,
                        PaymentStageId = r.StageId,
                        LineType = "Stages",
                        GroupName = r.GroupName,
                        UtilityCost = false,

                        // 🔽 nieuw: metadata voor header
                        UnitType = r.UnitType,
                        ProjectName = r.ProjectName,
                        ProjectStreet = r.ProjectStreet,
                        ProjectHouseNumber = r.ProjectHouseNumber,
                        ProjectCity = r.ProjectCity,
                        UnitStreet = r.UnitStreet,
                        UnitHouseNumber = r.UnitHouseNumber,
                        UnitConstructionTotal = r.UnitConstructionTotal,
                        OwnerPercentage = ownerPct
                    })
                    .ToList();

                return PartialView("_StageLinesTable", lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ComposeStageLines failed");
                return PartialView("_StageLinesTable", new List<InvoiceLineVM>());
            }
        }

        // LIJNEN VOOR WIJZEGINGSOPDRACHTEN AANMAKEN 

        // CHANGE ORDERS – regels opbouwen voor compose
        [HttpGet]
        public async Task<IActionResult> ComposeChangeOrderLines(int clientId, int? projectId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0)
                    return Content("<div class='text-muted small'>Kies eerst een klant.</div>", "text/html");

                var rows = await _ps.GetApprovedChangeOrdersForClientAsync(clientId, projectId, ct);
                if (rows == null || rows.Count == 0)
                    return Content("<div class='text-warning small'>Geen wijzigingsopdrachten gevonden om te factureren.</div>", "text/html");

                // Map naar VM – initieel 100% → prijs vooraf invullen
                var list = rows.Select(r =>
                {
                    var initialPct = 100m;
                    var initialCalc = Math.Round(r.BaseAmountExcl * (initialPct / 100m), 2, MidpointRounding.AwayFromZero);

                    return new InvoiceLineVM
                    {
                        IsSelected = false,
                        Text = r.Title,
                        UnitPrice = r.UnitPrice,
                        Number = r.Number,
                        Price = initialCalc,               
                        VatPercentage = r.VatPercentage,
                        LineType = "ChangeOrders",
                        GroupName = "Wijzigingsopdrachten",
                        ChangeOrderId = r.ChangeOrderId,          
                        ChangeOrderDetailId = r.ChangeOrderDetailId,
                        UnitId = r.UnitId,
                        StagePercentage = initialPct, 

                        // (optionele context)
                        UnitName = r.UnitName,
                        ProjectName = r.ProjectName
                    };
                }).ToList();

                // Data voor client-side berekeningen/groepering
                // - base bedrag per DETAIL
                ViewData["coBaseMap"] = rows.ToDictionary(x => x.ChangeOrderDetailId, x => x.BaseAmountExcl);
                // - naam per CO (bv. koptekst)
                ViewData["coNameMap"] = rows
                    .GroupBy(x => x.ChangeOrderId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().ChangeOrderDescription ?? $"Wijzigingsopdracht #{g.Key}"
                    );
                // - detail-ids per CO (voor master-actie)
                ViewData["coGroupMap"] = rows
                    .GroupBy(x => x.ChangeOrderId)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ChangeOrderDetailId).ToList());
                // coId -> jaar (bijv. DateAgreement.Year; anders Date.Year; anders current year)
                ViewData["coYearMap"] = rows
                    .GroupBy(r => r.ChangeOrderId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.Date?.Year).FirstOrDefault() ?? DateTime.Now.Year
                    );
                return PartialView("_ChangeOrderLinesTable", list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeOrderLinesTable failed");
                return Content("<div class='text-danger small'>Kon wijzigingsopdrachten niet laden.</div>", "text/html");
            }
        }




        // CREATE DRAFT (POST) – eenvoudige conceptfactuur aanmaken
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> CreateDraft(InvoiceComposeVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(vm.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf aan te maken.", "Geen toegang");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }

            if (vm.IssuerCompanyId <= 0 || vm.PartyType is null || vm.PartyId is null)
            {
                AddMessage("error", "Kies een facturatiebedrijf en afnemer.", "Factuur");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }

            var bo = new InvoiceDraftBO
            {
                IssuerCompanyId = vm.IssuerCompanyId,
                InvoiceDate = vm.InvoiceDate,
                IsCreditNote = vm.IsCreditNote,
                IsPrepaid = vm.IsPrepaid
            };

            switch (vm.PartyType.Value)
            {
                case InvoicePartyType.ClientAccount:
                    bo.ClientType = 1; bo.ClientId = vm.PartyId; bo.CompanyId = null; break;
                case InvoicePartyType.ClientContact:
                    bo.ClientType = 2; bo.ClientId = vm.PartyId; bo.CompanyId = null; break;
                case InvoicePartyType.Supplier:
                    bo.CompanyId = vm.PartyId; bo.ClientType = null; bo.ClientId = null; break;
            }

            var id = await _cmd.CreateDraftAsync(bo, ct);
            TempData["wantIssueNow"] = (vm.StartAs == StartStatus.Invoice);
            AddMessage("success", "Conceptfactuur aangemaakt.", "Factuur");
            return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
        }

        private async Task<InvoiceDraftBO> BuildInvoiceDraftBoAsync(InvoiceComposeVM vm, CancellationToken ct)
        {
            if (vm == null)
                throw new ArgumentNullException(nameof(vm));

            if (vm.IssuerCompanyId <= 0 || vm.PartyId is null || vm.PartyType is null)
                throw new InvalidOperationException("Vul issuer en afnemer in.");

            var usingStageLines = vm.Mode == InvoiceMode.Stages && vm.Lines != null && vm.Lines.Any();
            if (vm.Mode == InvoiceMode.Stages && !usingStageLines)
            {
                if (vm.PartyType != InvoicePartyType.ClientAccount && vm.PartyType != InvoicePartyType.ClientContact)
                    throw new InvalidOperationException("Schijvenfacturatie is enkel voor klanten.");

                if (vm.StageIds == null || vm.StageIds.Count == 0)
                    throw new InvalidOperationException("Kies minstens één schijf.");

                var ok = await _ps.AreStagesValidForClientAsync(vm.PartyId.Value, vm.StageIds, ct);
                if (!ok)
                    throw new InvalidOperationException("Een of meer gekozen schijven horen niet bij deze klant of zijn niet factureerbaar.");
            }

            var bo = new InvoiceDraftBO
            {
                IssuerCompanyId = vm.IssuerCompanyId,
                InvoiceDate = vm.InvoiceDate,
                Mode = vm.Mode,
                HeaderDescription = vm.HeaderDescription,
                DetailDescription = vm.DetailDescription,
                ProjectId = vm.ProjectId,
                SupplierContractId = vm.SupplierContractId,
                PaymentGroupId = vm.PaymentGroupId,
                IssuerBankAccountId = vm.IssuerBankAccountId,
                PaymentTermId = vm.PaymentTermId,
                FooterDescription = vm.FooterDescription,
                SelectedVatTypeId = vm.VatTypeId,
                IsPrepaid = vm.IsPrepaid
            };

            bo.IsCreditNote = vm.IsCreditNote;

            if (vm.PartyType == InvoicePartyType.Supplier)
                (bo.CompanyId, bo.ClientType, bo.ClientId) = (vm.PartyId, null, null);
            else
                (bo.CompanyId, bo.ClientType, bo.ClientId) = (null, (int?)vm.PartyType, vm.PartyId);

            if (vm.Mode == InvoiceMode.Free)
            {
                bo.Lines = vm.Lines?.Select(l => new InvoiceLineBO
                {
                    Text = l.Text,
                    Price = l.Price,
                    Quantity = l.Quantity > 0 ? l.Quantity : 1m,
                    UnitPrice = l.UnitPrice != 0 ? l.UnitPrice : (decimal?)null,
                    VatPercentage = l.VatPercentage,
                    VatTypeId = l.VatTypeId ?? vm.VatTypeId,
                    VatCode = l.VatCode,
                    DiscountPercent = l.DiscountPercent,
                    DiscountAmount = l.DiscountAmount,
                    UnitId = l.UnitId,
                    PaymentStageId = l.PaymentStageId,
                    LineType = l.LineType,
                    GroupName = l.GroupName,
                    UtilityCost = l.UtilityCost
                }).ToList() ?? new List<InvoiceLineBO>();
            }
            else if (vm.Mode == InvoiceMode.Stages && usingStageLines)
            {
                var selected = vm.Lines!.Where(x => x.IsSelected).ToList();
                if (selected.Count == 0)
                    throw new InvalidOperationException("Kies minstens één schijf-lijn.");

                bo.Lines = selected.Select(l => new InvoiceLineBO
                {
                    Text = l.Text,
                    Price = l.Price,
                    VatPercentage = l.VatPercentage,
                    VatTypeId = l.VatTypeId ?? vm.VatTypeId,
                    VatCode = l.VatCode,
                    DiscountPercent = l.DiscountPercent,
                    DiscountAmount = l.DiscountAmount,
                    UnitId = l.UnitId,
                    PaymentStageId = l.PaymentStageId,
                    LineType = l.LineType ?? "Stages",
                    GroupName = l.GroupName,
                    UtilityCost = l.UtilityCost
                }).ToList();

                bo.StageIds = new List<int>();
            }
            else if (vm.Mode == InvoiceMode.ChangeOrders)
            {
                if (vm.PartyType != InvoicePartyType.ClientAccount && vm.PartyType != InvoicePartyType.ClientContact)
                    throw new InvalidOperationException("Wijzigingsopdrachten zijn enkel voor klanten.");

                var selected = (vm.Lines ?? Enumerable.Empty<InvoiceLineVM>())
                    .Where(l => l.IsSelected && l.ChangeOrderDetailId.HasValue)
                    .ToList();

                if (selected.Count == 0)
                    throw new InvalidOperationException("Kies minstens één wijzigingsopdracht.");

                var clientId = vm.PartyId!.Value;
                var allRows = await _ps.GetApprovedChangeOrdersForClientAsync(clientId, vm.ProjectId, ct);
                var byDetail = allRows.ToDictionary(x => x.ChangeOrderDetailId, x => x);

                var dup = selected.Select(s => s.ChangeOrderDetailId!.Value)
                    .GroupBy(id => id)
                    .FirstOrDefault(g => g.Count() > 1);
                if (dup != null)
                    throw new InvalidOperationException("Dezelfde wijzigingsopdracht werd meermaals geselecteerd.");

                const decimal tol = 0.005m;
                foreach (var l in selected)
                {
                    var detailId = l.ChangeOrderDetailId!.Value;

                    if (!byDetail.TryGetValue(detailId, out var src))
                        throw new InvalidOperationException("Een wijzigingsopdracht is niet (meer) factureerbaar.");

                    var pct = l.StagePercentage;
                    if (pct < 0m || pct > 100m)
                        throw new InvalidOperationException($"Percentage moet tussen 0 en 100 liggen (detail {detailId}).");

                    var remaining = src.BaseAmountExcl;
                    var expected = Math.Round(remaining * (pct / 100m), 2, MidpointRounding.AwayFromZero);

                    if (expected != 0m && Math.Sign(expected) != Math.Sign(remaining))
                        throw new InvalidOperationException($"Teken van het bedrag komt niet overeen met het resterende saldo (detail {detailId}).");

                    if (Math.Abs(expected) - Math.Abs(remaining) > tol)
                        throw new InvalidOperationException($"Gevraagde fractie overschrijdt het resterende saldo (detail {detailId}).");
                }

                var boLines = new List<InvoiceLineBO>();
                foreach (var l in selected)
                {
                    var detailId = l.ChangeOrderDetailId!.Value;
                    if (!byDetail.TryGetValue(detailId, out var row))
                        continue;

                    var pct = Math.Clamp(l.StagePercentage, 0m, 100m);
                    var calc = Math.Round(row.BaseAmountExcl * (pct / 100m), 2, MidpointRounding.AwayFromZero);

                    if (calc == 0m)
                        continue;

                    boLines.Add(new InvoiceLineBO
                    {
                        Text = row.Title,
                        Price = calc,
                        VatPercentage = row.VatPercentage,
                        VatTypeId = vm.VatTypeId,
                        LineType = "ChangeOrders",
                        GroupName = "Wijzigingsopdrachten",
                        ChangeOrderDetailId = detailId,
                        UnitId = row.UnitId
                    });
                }

                if (boLines.Count == 0)
                    throw new InvalidOperationException("Geen geldige bedragen om te boeken (controleer percentages).");

                bo.Lines = boLines;
            }
            else
            {
                bo.StageIds = vm.StageIds?.ToList() ?? new List<int>();
                bo.Lines = new List<InvoiceLineBO>();
            }

            return bo;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Save(InvoiceComposeVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(vm.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf aan te maken.", "Geen toegang");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }

            try
            {
                var bo = await BuildInvoiceDraftBoAsync(vm, ct);
                var issueNow = vm.StartAs == StartStatus.Invoice;
              

                if (!issueNow)
                {
                    var (invoiceId, publicId) = await _cmd.CreateWithLinesAsync(bo, issueNow: false, ct);
                    await SavePdfAppendixAsync(invoiceId, vm.PdfAppendix, ct);

                    if (publicId != null)
                        AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
                    else
                        AddMessage("success", "Conceptfactuur opgeslagen.", "Factuur");

                    return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
                }

                var (id, _) = await _cmd.CreateWithLinesAsync(bo, issueNow: false, ct);
                await SavePdfAppendixAsync(id, vm.PdfAppendix, ct);
                try
                {
                    await SendInvoiceToOctopusAsync(id, ct, forceFinalPdf: true, skipClassicEmail: true, forcedIssueDate: vm.InvoiceDate);
                }
                catch (InvalidOperationException ex)
                {
                    await RestoreDraftStateAsync(id, (byte)InvoiceStatus.Draft, ct);
                    _logger.LogWarning(ex, "Octopus sync blocked for invoice {InvoiceId}", id);
                    AddMessage("error", ex.Message, "Factuur");
                    return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
                }
                catch (Exception ex)
                {
                    await RestoreDraftStateAsync(id, (byte)InvoiceStatus.Draft, ct);
                    _logger.LogError(ex, "Octopus sync failed for invoice {InvoiceId}", id);
                    AddMessage("error", "Factuur kon niet naar Octopus verstuurd worden.", "Factuur");
                    return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
                }

                string issuedPublicId;
                try
                {
                    issuedPublicId = await _cmd.IssueDraftAsync(id, issueDate: vm.InvoiceDate, ct: ct);
                }
                catch
                {
                    await RestoreDraftStateAsync(id, (byte)InvoiceStatus.Draft, ct);
                    throw;
                }
                await TrySendClassicEmailAfterIssueAsync(id, ct);

                if (!string.IsNullOrWhiteSpace(issuedPublicId))
                    AddMessage("success", $"Factuur uitgegeven: {issuedPublicId}", "Factuur");
                else
                    AddMessage("success", "Factuur uitgegeven.", "Factuur");

                return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
            }
            catch (InvalidOperationException ex)
            {
                AddMessage("error", ex.Message, "Factuur");
                return await Create(vm.IssuerCompanyId, ct: ct);
            }
        }

        private async Task SavePdfAppendixAsync(int invoiceId, IFormFile? pdfAppendix, CancellationToken ct, bool removeExisting = false)
        {
            if ((pdfAppendix == null || pdfAppendix.Length == 0) && !removeExisting)
                return;

            var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId, ct)
                ?? throw new InvalidOperationException("Factuur niet gevonden.");

            if (removeExisting)
            {
                invoice.PdfAppendixFileName = null;
                invoice.PdfAppendixContent = null;
            }

            if (pdfAppendix != null && pdfAppendix.Length > 0)
            {
                if (!string.Equals(pdfAppendix.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                    && !pdfAppendix.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("De bijlage moet een PDF-bestand zijn.");
                }

                await using var ms = new MemoryStream();
                await pdfAppendix.CopyToAsync(ms, ct);
                var bytes = ms.ToArray();
                if (bytes.Length == 0)
                    throw new InvalidOperationException("De PDF-bijlage is leeg.");

                invoice.PdfAppendixFileName = Path.GetFileName(pdfAppendix.FileName);
                invoice.PdfAppendixContent = bytes;
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task<byte[]> RenderInvoicePdfWithAppendixAsync(int invoiceId, InvoiceDto dto, IssuerCompanyBO issuer, CancellationToken ct)
        {
            var basePdf = _pdf.Render(dto, issuer);
            var appendix = await _db.Invoices
                .AsNoTracking()
                .Where(x => x.Id == invoiceId)
                .Select(x => x.PdfAppendixContent)
                .FirstOrDefaultAsync(ct);

            if (appendix == null || appendix.Length == 0)
                return basePdf;

            return MergePdfDocuments(basePdf, appendix);
        }

        private static byte[] MergePdfDocuments(byte[] invoicePdf, byte[] appendixPdf)
        {
            using var output = new PdfDocument();
            AppendPages(output, invoicePdf);
            AppendPages(output, appendixPdf);

            using var stream = new MemoryStream();
            output.Save(stream, false);
            return stream.ToArray();
        }

        private static void AppendPages(PdfDocument target, byte[] sourcePdf)
        {
            using var sourceStream = new MemoryStream(sourcePdf, writable: false);
            using var source = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
            for (var i = 0; i < source.PageCount; i++)
            {
                target.AddPage(source.Pages[i]);
            }
        }

        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Edit(int id, int? issuerCompanyId = null, string? returnUrl = null, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om facturen te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            if (await IsInvoiceGeneratingAsync(id, ct))
            {
                AddMessage("error", "Deze factuur wordt momenteel gegenereerd. Probeer later opnieuw.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = detail.IssuerCompanyId });
            }
            var resolvedReturnUrl = DetermineReturnUrl(returnUrl);
            if (resolvedReturnUrl == null)
                resolvedReturnUrl = DetermineReturnUrl(Request.Headers["Referer"].ToString());

            if (IsDraftStatus(detail.StatusName))
            {
                var draftVm = await BuildDraftEditViewModelAsync(detail, null, resolvedReturnUrl, ct);
                await ConfigureEditContextAsync(detail, ct);
                SetPageHeader("bx bx-receipt", $"Factuur bewerken - {draftVm.DisplayId}");
                return View("EditDraft", draftVm);
            }

            var vm = MapEdit(detail);
            vm.ReturnUrl = resolvedReturnUrl;
            await ConfigureEditContextAsync(detail, ct);
            SetPageHeader("bx bx-receipt", string.IsNullOrWhiteSpace(vm.IssuerName) ? vm.DisplayId : $"{vm.IssuerName} - {vm.DisplayId}");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Edit(InvoiceEditVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om facturen te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            if (vm == null)
                return RedirectToAction(nameof(Index));

            if (await IsInvoiceGeneratingAsync(vm.InvoiceId, ct))
            {
                AddMessage("error", "Deze factuur wordt momenteel gegenereerd. Probeer later opnieuw.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
            }

            var detail = await _invoices.GetDetailAsync(vm.InvoiceId, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
            }
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = detail.IssuerCompanyId });
            }

            var resolvedReturnUrl = DetermineReturnUrl(vm.ReturnUrl);
            if (resolvedReturnUrl == null)
                resolvedReturnUrl = DetermineReturnUrl(Request.Headers["Referer"].ToString());

            if (IsDraftStatus(detail.StatusName))
                return RedirectToAction(nameof(Edit), new { id = vm.InvoiceId, issuerCompanyId = detail.IssuerCompanyId, returnUrl = resolvedReturnUrl });

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildEditViewModel(detail, vm);
                invalidVm.ReturnUrl = resolvedReturnUrl;
                await ConfigureEditContextAsync(detail, ct);
                SetPageHeader("bx bx-receipt", string.IsNullOrWhiteSpace(invalidVm.IssuerName) ? invalidVm.DisplayId : $"{invalidVm.IssuerName} - {invalidVm.DisplayId}");
                return View(invalidVm);
            }

            var update = new InvoiceUpdateBO
            {
                InvoiceId = vm.InvoiceId,
                HeaderDescription = vm.HeaderDescription ?? string.Empty,
                DetailDescription = vm.DetailDescription ?? string.Empty,
                FooterDescription = vm.FooterDescription ?? string.Empty,
                BankAccount = vm.BankAccount ?? string.Empty,
                ExpirationDate = vm.ExpirationDate,
                IsDraft = IsDraftStatus(detail.StatusName)
            };

            if (vm.Lines != null && vm.Lines.Count > 0)
            {
                foreach (var line in vm.Lines)
                {
                    if (line == null || line.LineId <= 0)
                        continue;

                    update.Lines.Add(new InvoiceLineUpdateBO
                    {
                        LineId = line.LineId,
                        Text = line.Text ?? string.Empty
                    });
                }
            }

            try
            {
                await _cmd.UpdateAsync(update, ct);
                AddMessage("success", "Factuur bijgewerkt.", "Factuur");
                if (!string.IsNullOrEmpty(resolvedReturnUrl))
                    return LocalRedirect(resolvedReturnUrl);

                return RedirectToAction(nameof(Detail), new { id = vm.InvoiceId, issuerCompanyId = detail.IssuerCompanyId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update invoice {InvoiceId} blocked", vm.InvoiceId);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update invoice {InvoiceId} failed", vm.InvoiceId);
                AddMessage("error", "Factuur kon niet bijgewerkt worden.", "Factuur");
            }

            var hydrated = BuildEditViewModel(detail, vm);
            hydrated.ReturnUrl = resolvedReturnUrl;
            await ConfigureEditContextAsync(detail, ct);
            SetPageHeader("bx bx-receipt", string.IsNullOrWhiteSpace(hydrated.IssuerName) ? hydrated.DisplayId : $"{hydrated.IssuerName} - {hydrated.DisplayId}");
            return View(hydrated);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> UpdateDraft(InvoiceDraftEditVM vm, CancellationToken ct)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om facturen te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            if (vm == null || vm.InvoiceId <= 0)
                return RedirectToAction(nameof(Index));

            var detail = await _invoices.GetDetailAsync(vm.InvoiceId, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
            }
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = detail.IssuerCompanyId });
            }


            var resolvedReturnUrl = DetermineReturnUrl(vm.ReturnUrl);
            if (resolvedReturnUrl == null)
                resolvedReturnUrl = DetermineReturnUrl(Request.Headers["Referer"].ToString());

            if (!IsDraftStatus(detail.StatusName))
            {
                AddMessage("error", "Deze factuur is niet langer een concept en kan niet volledig bewerkt worden.", "Factuur");
                if (!string.IsNullOrEmpty(resolvedReturnUrl))
                    return LocalRedirect(resolvedReturnUrl);

                return RedirectToAction(nameof(Detail), new { id = vm.InvoiceId, issuerCompanyId = detail.IssuerCompanyId });
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildDraftEditViewModelAsync(detail, vm, resolvedReturnUrl, ct);
                await ConfigureEditContextAsync(detail, ct);
                SetPageHeader("bx bx-receipt", $"Factuur bewerken - {invalidVm.DisplayId}");
                return View("EditDraft", invalidVm);
            }

            try
            {
                var bo = await BuildInvoiceDraftBoAsync(vm, ct);
                await _cmd.UpdateDraftAsync(vm.InvoiceId, bo, ct);
                await SavePdfAppendixAsync(vm.InvoiceId, vm.PdfAppendix, ct, vm.RemovePdfAppendix);
                AddMessage("success", "Conceptfactuur bijgewerkt.", "Factuur");
                if (!string.IsNullOrEmpty(resolvedReturnUrl))
                    return LocalRedirect(resolvedReturnUrl);

                return RedirectToAction(nameof(Detail), new { id = vm.InvoiceId, issuerCompanyId = detail.IssuerCompanyId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update draft invoice {InvoiceId} blocked", vm.InvoiceId);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update draft invoice {InvoiceId} failed", vm.InvoiceId);
                AddMessage("error", "Conceptfactuur kon niet bijgewerkt worden.", "Factuur");
            }

            var hydratedDraft = await BuildDraftEditViewModelAsync(detail, vm, resolvedReturnUrl, ct);
            await ConfigureEditContextAsync(detail, ct);
            SetPageHeader("bx bx-receipt", $"Factuur bewerken - {hydratedDraft.DisplayId}");
            return View("EditDraft", hydratedDraft);
        }

        // ========== helper ==========
        private async Task<InvoiceDraftEditVM> BuildDraftEditViewModelAsync(InvoiceDetailBO detail, InvoiceDraftEditVM? posted, string? returnUrl, CancellationToken ct)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));

            var issuersBo = await _ics.ListActiveIssuersAsync(ct);
            var termsBo = await _ics.ListPaymentTermsAsync(detail.IssuerCompanyId, ct);


            var selectedIssuerId = posted?.IssuerCompanyId > 0 ? posted!.IssuerCompanyId : detail.IssuerCompanyId;
            var accountsBo = selectedIssuerId > 0
                ? await _bank.ListByIssuerAsync(selectedIssuerId, ct)
                : Array.Empty<IssuerBankAccountBO>();

            var vatBo = selectedIssuerId > 0
                ? await _ics.ListVatTypeAsync(selectedIssuerId, ct)
                : Array.Empty<VatTypeBO>();

            var draftIsCreditNote = posted?.IsCreditNote ?? DetermineCreditNote(detail.IsCreditNote, detail.StatusName, detail.TotalInclVat);

            var vm = new InvoiceDraftEditVM
            {
                InvoiceId = detail.Id,
                IssuerCompanyId = selectedIssuerId,
                IssuerName = detail.IssuerLegalName ?? detail.IssuerName,
                Status = TranslateStatus(detail.StatusName),
                PublicId = string.IsNullOrWhiteSpace(detail.PublicId) ? null : detail.PublicId,
                InvoiceDate = posted?.InvoiceDate ?? detail.InvoiceDate,
                ExpirationDate = detail.ExpirationDate,
                StartAs = StartStatus.Draft,
                Mode = posted?.Mode ?? detail.InvoiceMode ?? InvoiceMode.Free,
                HeaderDescription = posted?.HeaderDescription ?? NormalizeMultiline(detail.HeaderText),
                DetailDescription = posted?.DetailDescription ?? NormalizeMultiline(detail.DetailText),
                FooterDescription = posted?.FooterDescription ?? NormalizeMultiline(detail.ExtraInfo),
                IsCreditNote = draftIsCreditNote,
                IsPrepaid = posted?.IsPrepaid ?? detail.IsPrepaid,
                PaymentTermId = posted?.PaymentTermId ?? detail.PaymentTermId,
                ProjectId = posted?.ProjectId ?? detail.ProjectId,
                SupplierContractId = posted?.SupplierContractId ?? detail.SupplierContractId,
                PaymentGroupId = posted?.PaymentGroupId,
                VatTypeId = posted?.VatTypeId,
                IssuerBankAccountId = posted?.IssuerBankAccountId,
                PartyId = posted?.PartyId,
                PartyType = posted?.PartyType,
                PartyDisplayName = posted?.PartyDisplayName ?? detail.ClientName,
                PartyLookupValue = posted?.PartyLookupValue,
                Lines = posted?.Lines != null ? posted.Lines.ToList() : MapLinesForCompose(detail.Lines, draftIsCreditNote),
                StageIds = posted?.StageIds != null && posted.StageIds.Count > 0
                    ? new List<int>(posted.StageIds)
                    : detail.Lines?
                        .Where(l => l.PaymentStageId.HasValue)
                        .Select(l => l.PaymentStageId!.Value)
                        .Distinct()
                        .ToList() ?? new List<int>(),
                TotalExclVat = RoundCurrency(detail.TotalExclVat),
                TotalVat = RoundCurrency(detail.TotalVat),
                TotalInclVat = RoundCurrency(detail.TotalInclVat),
                BankAccountIban = detail.BankAccount,
                CurrentPdfAppendixFileName = detail.PdfAppendixFileName,
                RemovePdfAppendix = posted?.RemovePdfAppendix ?? false,
                VatTypes = vatBo.Select(t => new VatTypeVM
                {
                    Id = t.Id,
                    BasePercentage = t.BasePercentage,
                    Code = t.Code,
                    Description = t.Description,
                    Type = t.Type,
                    DefaultSellBookingAccountNr = t.DefaultSellBookingAccountNr,
                    InvoiceMention = t.InvoiceMention
                }).ToList(),
                Issuers = issuersBo.Select(i => new IssuerItemVM(i.Id, i.Name, i.DefaultPaymentTermId, i.DefaultVatTypeId)).ToList(),
                PaymentTerms = termsBo
                    .Select(t => new PaymentTermItemVM(t.Id, t.Name, t.Days, t.TermType, t.DisplayMode, t.DisplayText))
                    .ToList()
            };

            if (vm.PartyId is null || vm.PartyType is null)
            {
                if (detail.CompanyId.HasValue)
                {
                    vm.PartyId = detail.CompanyId;
                    vm.PartyType = InvoicePartyType.Supplier;
                }
                else if (detail.ClientType.HasValue && detail.ClientId.HasValue)
                {
                    vm.PartyId = detail.ClientId;
                    vm.PartyType = detail.ClientType.Value switch
                    {
                        1 => InvoicePartyType.ClientAccount,
                        2 => InvoicePartyType.ClientContact,
                        _ => vm.PartyType
                    };
                }
            }

            vm.PartyLookupValue ??= BuildPartyLookupValue(vm.PartyType, vm.PartyId);
            if (string.IsNullOrWhiteSpace(vm.PartyDisplayName))
                vm.PartyDisplayName = detail.ClientName;

            if (!vm.VatTypeId.HasValue && vm.VatTypes.Any())
            {
                var firstLine = vm.Lines?.FirstOrDefault();
                if (firstLine != null)
                {
                    var match = vm.VatTypes.FirstOrDefault(v => Math.Abs(v.BasePercentage - firstLine.VatPercentage) < 0.001m);
                    if (match != null)
                        vm.VatTypeId = match.Id;
                }
            }

            if (!vm.IssuerBankAccountId.HasValue && !string.IsNullOrWhiteSpace(detail.BankAccount))
            {
                var matchAccount = accountsBo.FirstOrDefault(a => string.Equals(a.Iban, detail.BankAccount, StringComparison.OrdinalIgnoreCase));
                if (matchAccount != null)
                    vm.IssuerBankAccountId = matchAccount.Id;
            }

            vm.IssuerBankAccounts = accountsBo
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(a.DisplayName) ? a.Iban : $"{a.DisplayName} ({a.Iban})",
                    Selected = vm.IssuerBankAccountId.HasValue && vm.IssuerBankAccountId.Value == a.Id
                })
                .ToList();

            vm.ReturnUrl = DetermineReturnUrl(posted?.ReturnUrl ?? returnUrl);
            return vm;
        }

        private static List<InvoiceLineVM> MapLinesForCompose(IEnumerable<InvoiceLineBO> lines, bool isCreditNote)
        {
            if (lines == null)
                return new List<InvoiceLineVM>();

            return lines.Select(l => new InvoiceLineVM
            {
                Text = l.Text ?? string.Empty,
                // Price staat in de DB al getekend (bij een creditnota is elke lijn met -1 vermenigvuldigd,
                // zie CreateWithLinesAsync/UpdateDraftAsync * sign). De compose-UI verwacht het ingevoerde
                // bedrag zoals de gebruiker het typte en past het teken zelf opnieuw toe op basis van de
                // IsCreditNote-toggle, dus hier moeten we exact diezelfde vermenigvuldiging ongedaan maken
                // (niet zomaar Math.Abs — anders verliest een bewust negatieve lijn op een gewone (niet-
                // credit) factuur, bv. een kortingslijn, zijn minteken bij het bewerken/dupliceren).
                Price = isCreditNote ? -l.Price : l.Price,
                VatPercentage = l.VatPercentage,
                VatTypeId = l.VatTypeId,
                VatCode = l.VatCode,
                DiscountPercent = l.DiscountPercent,
                DiscountAmount = l.DiscountAmount,
                UnitId = l.UnitId,
                PaymentStageId = l.PaymentStageId,
                LineType = l.LineType,
                GroupName = l.GroupName,
                UtilityCost = l.UtilityCost,
                ChangeOrderDetailId = l.ChangeOrderDetailId,
                IsSelected = true
            }).ToList();
        }

        private string BuildDuplicateInitialJson(InvoiceDetailBO detail, InvoiceComposeVM vm)
        {
            var initialLines = vm.Lines?.Select(line =>
            {
                var matchedVatType = vm.VatTypes?.FirstOrDefault(v => v.Id == line.VatTypeId)
                    ?? vm.VatTypes?.FirstOrDefault(v => string.Equals(v.Code, line.VatCode, StringComparison.OrdinalIgnoreCase))
                    ?? vm.VatTypes?.FirstOrDefault(v => Math.Abs(v.BasePercentage - line.VatPercentage) < 0.001m);

                return new
                {
                    text = line.Text,
                    price = line.Price,
                    quantity = line.Quantity,
                    // MapLinesForCompose vult UnitPrice nooit in (blijft dus 0, niet null) voor een
                    // gedupliceerde lijn. invoices.freelines.js (loadInitialRows) geeft echter
                    // voorrang aan unitPrice zodra die != null is, dus een letterlijke 0 hier zorgt
                    // ervoor dat het bedrag van elke lijn als 0,00 verschijnt i.p.v. de echte prijs.
                    unitPrice = line.UnitPrice != 0 ? line.UnitPrice : (decimal?)null,
                    vatPercentage = line.VatPercentage,
                    vatTypeId = matchedVatType?.Id ?? line.VatTypeId,
                    vatCode = line.VatCode ?? matchedVatType?.Code,
                    discountPercent = line.DiscountPercent,
                    discountAmount = line.DiscountAmount,
                    paymentStageId = line.PaymentStageId,
                    lineType = line.LineType,
                    groupName = line.GroupName,
                    utilityCost = line.UtilityCost,
                    changeOrderDetailId = line.ChangeOrderDetailId,
                    isSelected = line.IsSelected,
                    stagePercentage = line.StagePercentage
                };
            });

            var partyValue = BuildPartyLookupValue(vm.PartyType, vm.PartyId);
            var initialData = new
            {
                invoiceDate = vm.InvoiceDate.ToString("dd/MM/yyyy"),
                paymentTermId = vm.PaymentTermId,
                vatTypeId = vm.VatTypeId,
                issuerBankAccountId = vm.IssuerBankAccountId,
                mode = (int)vm.Mode,
                party = new { value = partyValue, text = detail.ClientName, type = vm.PartyType?.ToString() },
                lines = initialLines
            };

            return System.Text.Json.JsonSerializer.Serialize(
                initialData,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
            );
        }

        private static string? BuildPartyLookupValue(InvoicePartyType? type, int? id)
        {
            if (!type.HasValue || !id.HasValue)
                return null;

            return type.Value switch
            {
                InvoicePartyType.ClientAccount => $"ca:{id.Value}",
                InvoicePartyType.ClientContact => $"cc:{id.Value}",
                InvoicePartyType.Supplier => $"su:{id.Value}",
                _ => null
            };
        }

        // Titel = wát het is (design-handoff punt 13 "Topbar met lange namen"): type + factuurnummer,
        // nooit de bedrijfsnaam — die staat al in het kruimelpad ("Facturen - {bedrijf}"). Was tot nu
        // toe enkel Edit se eigen titel/kruimelsegment; Detail en Send gebruikten een aparte, minder
        // complete `BuildInvoiceDisplayTitle` die de bedrijfsnaam wél herhaalde (en Draft/Creditnota
        // niet onderscheidde) — die helper is verwijderd, alle drie delen nu deze ene.
        private static string BuildInvoiceDetailBreadcrumbTitle(InvoiceDetailBO detail)
        {
            var typeLabel = IsDraftStatus(detail.StatusName)
                 ? "Draft"
                : detail.IsCreditNote
                    ? "Creditnota"
                    : "Factuur";

            var displayId = string.IsNullOrWhiteSpace(detail.PublicId)
                ? $"#{detail.Id}"
                : detail.PublicId.Trim();

            return $"{typeLabel} - {displayId}";
        }
        private string? ResolveUserName()
        {
            var userName = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(userName))
                return userName;

            userName = User.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrWhiteSpace(userName))
                return userName;

            userName = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(userName))
                return userName;

            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        private InvoiceDetailVM MapDetail(InvoiceDetailBO bo)
        {
            if (bo == null) throw new ArgumentNullException(nameof(bo));

            var vm = new InvoiceDetailVM
            {
                Id = bo.Id,
                PublicId = string.IsNullOrWhiteSpace(bo.PublicId) ? null : bo.PublicId,
                InvoiceDate = bo.InvoiceDate,
                ExpirationDate = bo.ExpirationDate,
                Status = TranslateStatus(bo.StatusName),
                PaymentTermDisplayMode = bo.PaymentTermDisplayMode ?? PaymentTermDisplayMode.DueDate,
                PaymentTermDisplayText = bo.PaymentTermDisplayText,
                BankAccount = bo.BankAccount,
                HeaderText = NormalizeMultiline(bo.HeaderText),
                DetailText = NormalizeMultiline(bo.DetailText),
                ExtraInfo = NormalizeMultiline(bo.ExtraInfo),
                TotalExclVat = RoundCurrency(bo.TotalExclVat),
                TotalVat = RoundCurrency(bo.TotalVat),
                TotalInclVat = RoundCurrency(bo.TotalInclVat),
                PaidAmount = bo.PaidAmount,
                Balance = bo.Balance,
                OctopusBookyearId = bo.OctopusBookyearId,
                OctopusJournalKey = bo.OctopusJournalKey,
                OctopusDocumentSequenceNr = bo.OctopusDocumentSequenceNr,
                OctopusWorkflowState = bo.OctopusWorkflowState,
                OctopusWorkflowUpdatedAt = bo.OctopusWorkflowUpdatedAt,
                OctopusDeliveryState = bo.OctopusDeliveryState,
                OctopusDeliveryComment = bo.OctopusDeliveryComment,
                OctopusDeliveryDateTime = bo.OctopusDeliveryDateTime,
                OctopusDeliveryUpdatedAt = bo.OctopusDeliveryUpdatedAt,
                OctopusBookedAt = bo.OctopusBookedAt,
                OctopusBookedBy = bo.OctopusBookedBy
            };

            vm.Issuer = new InvoicePartyVM
            {
                Name = bo.IssuerName,
                LegalName = bo.IssuerLegalName,
                VatNumber = bo.IssuerVatNumber,
                AddressLine1 = bo.IssuerAddressLine1,
                AddressLine2 = bo.IssuerAddressLine2,
                PostalCode = bo.IssuerPostalCode,
                City = bo.IssuerCity,
                Country = bo.IssuerCountryCode,
                Email = bo.IssuerEmail,
                Phone = bo.IssuerPhone
            };

            vm.Client = new InvoicePartyVM
            {
                Name = bo.ClientName,
                VatNumber = bo.ClientVatNumber,
                AddressLine1 = bo.ClientAddress,
                PostalCode = bo.ClientPostalCode,
                City = bo.ClientCity,
                Country = bo.ClientCountryName,
                Email = bo.ClientEmail
            };

            vm.Lines = MapLinesForEdit(bo.Lines).Select(ToDetailLine).ToList();
            vm.IsCreditNote = DetermineCreditNote(bo.IsCreditNote, bo.StatusName, bo.TotalInclVat);
            return vm;
        }
        private InvoiceEditVM MapEdit(InvoiceDetailBO detail)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));

            return new InvoiceEditVM
            {
                InvoiceId = detail.Id,
                IssuerCompanyId = detail.IssuerCompanyId,
                IssuerName = detail.IssuerLegalName ?? detail.IssuerName,
                PublicId = string.IsNullOrWhiteSpace(detail.PublicId) ? null : detail.PublicId,
                InvoiceDate = detail.InvoiceDate,
                ExpirationDate = detail.ExpirationDate,
                ClientName = detail.ClientName,
                Status = TranslateStatus(detail.StatusName),
                HeaderDescription = NormalizeMultiline(detail.HeaderText),
                DetailDescription = NormalizeMultiline(detail.DetailText),
                FooterDescription = NormalizeMultiline(detail.ExtraInfo),
                BankAccount = detail.BankAccount,
                TotalExclVat = RoundCurrency(detail.TotalExclVat),
                TotalVat = RoundCurrency(detail.TotalVat),
                TotalInclVat = RoundCurrency(detail.TotalInclVat),
                IsCreditNote = DetermineCreditNote(detail.IsCreditNote, detail.StatusName, detail.TotalInclVat),
                Lines = MapLinesForEdit(detail.Lines)
            };
        }

        private InvoiceEditVM BuildEditViewModel(InvoiceDetailBO detail, InvoiceEditVM posted)
        {
            var vm = MapEdit(detail);

            if (posted != null)
            {
                vm.HeaderDescription = posted.HeaderDescription;
                vm.DetailDescription = posted.DetailDescription;
                vm.FooterDescription = posted.FooterDescription;
                vm.BankAccount = posted.BankAccount;
                vm.ExpirationDate = posted.ExpirationDate;
                if (posted.Lines != null && posted.Lines.Count > 0 && vm.Lines != null && vm.Lines.Count > 0)
                {
                    var byId = vm.Lines.ToDictionary(l => l.LineId);
                    foreach (var line in posted.Lines)
                    {
                        if (line == null || line.LineId <= 0)
                            continue;

                        if (byId.TryGetValue(line.LineId, out var target))
                            target.Text = line.Text;
                    }
                }
            }

            vm.ReturnUrl = DetermineReturnUrl(posted?.ReturnUrl) ?? vm.ReturnUrl;
            return vm;
        }


        private async Task ConfigureEditContextAsync(InvoiceDetailBO detail, CancellationToken ct)
        {
            var issuerId = detail.IssuerCompanyId;
            if (issuerId > 0)
                await SetIssuerViewBagsAsync(issuerId, ct);
            else
                ViewBag.CompanyId = issuerId;

            var companyDisplay = (ViewBag.CompanyName as string)
                ?? detail.IssuerLegalName
                ?? detail.IssuerName;

            var detailTitle = BuildInvoiceDetailBreadcrumbTitle(detail);
            SetEditBreadcrumb(issuerId, companyDisplay, detail.Id, detailTitle);
            ViewData["Title"] = detailTitle;

            var vatBo = issuerId > 0
              ? await _ics.ListVatTypeAsync(issuerId, ct)
              : Array.Empty<VatTypeBO>();

            ViewBag.VatTypes = vatBo
                .Select(t => new VatTypeVM
                {
                    Id = t.Id,
                    BasePercentage = t.BasePercentage,
                    Code = t.Code,
                    Description = t.Description,
                    Type = t.Type,
                    DefaultSellBookingAccountNr = t.DefaultSellBookingAccountNr,
                    InvoiceMention = t.InvoiceMention
                })
                .ToList();
                
        }

        private static List<InvoiceLineEditVM> MapLinesForEdit(IEnumerable<InvoiceLineBO>? lines)
        {
            var list = (lines ?? Enumerable.Empty<InvoiceLineBO>()).ToList();

            // Btw per tarief op de maatstaf, cumulatief verdeeld over de lijnen zodat de
            // getoonde lijnbedragen exact optellen tot de factuurtotalen (zie InvoiceVatCalculator).
            var nets = list.Select(l =>
            {
                var discount = l.DiscountAmount
                    ?? (l.DiscountPercent.HasValue
                        ? Math.Round(l.Price * (l.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                        : 0m);
                return (Net: l.Price - discount, Rate: l.VatPercentage, Discount: discount);
            }).ToList();

            var vatAmounts = ServiceCore.Invoicing.InvoiceVatCalculator.AllocateLineVat(
                nets.Select(n => (n.Net, n.Rate)).ToList());

            return list.Select((line, i) => new InvoiceLineEditVM
            {
                LineId = line.Id,
                Text = line.Text ?? string.Empty,
                GroupName = line.GroupName,
                VatRate = line.VatPercentage,
                NetAmount = RoundCurrency(nets[i].Net),
                VatAmount = RoundCurrency(vatAmounts[i]),
                GrossAmount = RoundCurrency(nets[i].Net + vatAmounts[i]),
                DiscountAmount = nets[i].Discount != 0 ? RoundCurrency(nets[i].Discount) : (decimal?)null,
                DiscountPercent = line.DiscountPercent
            }).ToList();
        }

        private static InvoiceDetailLineVM ToDetailLine(InvoiceLineEditVM editLine)
        {
            return new InvoiceDetailLineVM
            {
                Text = editLine.Text,
                GroupName = editLine.GroupName,
                VatRate = editLine.VatRate,
                NetAmount = editLine.NetAmount,
                VatAmount = editLine.VatAmount,
                GrossAmount = editLine.GrossAmount,
                DiscountAmount = editLine.DiscountAmount,
                DiscountPercent = editLine.DiscountPercent
            };
        }


        private static string? CombineAddress(string? line1, string? line2)
        {
            if (string.IsNullOrWhiteSpace(line1))
                return line2;
            if (string.IsNullOrWhiteSpace(line2))
                return line1;
            return $"{line1}, {line2}";
        }
        private string? DetermineReturnUrl(string? returnUrl)
        {
            var normalized = NormalizeReturnUrl(returnUrl);
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            if (normalized.StartsWith("/Invoices/Edit", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("/Invoices/EditDraft", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return normalized;
        }

        private string? NormalizeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                return null;

            if (Url.IsLocalUrl(returnUrl))
                return returnUrl;

            if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute))
            {
                var path = absolute.PathAndQuery;
                if (Url.IsLocalUrl(path))
                    return path;
            }

            return null;
        }

    }
}
