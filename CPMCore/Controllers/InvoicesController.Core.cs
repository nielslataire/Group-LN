using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Service;
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
    /// <summary>Facturenlijst, boeken, detail, PDF/UBL/Octopus-download, verwijderen. Opgesplitst uit InvoicesController.cs (okt. 2026, structureren) - views in Views/Invoices/Core/. Zelfde partial class: alle private velden/services van InvoicesController.cs blijven gewoon bruikbaar.</summary>
    public partial class InvoicesController
    {

        // LIST
        public async Task<IActionResult> Index(int issuerCompanyId)
        {
            var readScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Read, HttpContext.RequestAborted);
            if (!readScope.HasAccess || (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(issuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, HttpContext.RequestAborted);
            var deleteScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Delete, HttpContext.RequestAborted);
            var bos = await _invoices.GetByCompanyAsync(issuerCompanyId);
            var bookyears = await _db.OctopusBookyears
                .Where(b => b.IssuerCompanyId == issuerCompanyId)
                .OrderByDescending(b => b.EndDate)
                .ToListAsync();

            string? ResolveBookyearLabel(DateOnly invoiceDate)
            {
                var invoiceDateTime = invoiceDate.ToDateTime(TimeOnly.MinValue);
                var match = bookyears.FirstOrDefault(b => invoiceDateTime >= b.StartDate && invoiceDateTime <= b.EndDate);
                return match != null ? FormatBookyearLabel(match.StartDate, match.EndDate) : invoiceDate.Year.ToString();
            }
            var vms = bos
            .Select(x =>
            {
                var isCreditNote = x.IsCreditNote
                   || (!string.IsNullOrWhiteSpace(x.StatusName) && x.StatusName.Contains("credit", StringComparison.OrdinalIgnoreCase))
                   || (x.GrossTotal.HasValue && x.GrossTotal.Value < 0m);
                var parts = ParseInvoicePublicId(x.PublicId);
                var sortValue = BuildInvoiceSortValue(x.InvoiceDate, parts.Number, parts.Month, parts.Year, x.Id);
                var digitallySent = IsOctopusStepCompleted(x.OctopusWorkflowState, OctopusWorkflowStateSent);
                var isIndividual = !x.IsSupplier && !x.HasCompanyName;
                var requiresDigital = x.IsSupplier || x.RequiresDigitalInvoice;
                var skipDigitalSend = isIndividual && (!requiresDigital || !x.HasEmail);
                //var needsPrint = skipDigitalSend || (!digitallySent && !x.HasEmail && !isIndividual);
                var needsPrint = true;

                return new InvoiceListItemVM
                {
                    Id = x.Id,
                    PublicId = x.PublicId,
                    ClientName = x.ClientName,
                    InvoiceDate = x.InvoiceDate,
                    Status = TranslateStatus(x.StatusId, x.StatusName),
                    GrossTotal = x.GrossTotal,
                    NetTotal = x.NetTotal,
                    Balance = x.Balance,
                    InvoiceNumber = parts.Number,
                    InvoiceMonth = parts.Month,
                    InvoiceYear = parts.Year,
                    IsCreditNote = isCreditNote,
                    InvoiceSortValue = sortValue,
                    RequiresDigitalInvoice = x.RequiresDigitalInvoice,
                    HasEmail = x.HasEmail,
                    ClientType = x.ClientType,
                    IsSupplier = x.IsSupplier,
                    HasCompanyName = x.HasCompanyName,
                    OctopusWorkflowState = x.OctopusWorkflowState,
                    DigitallySent = digitallySent,
                    DigitalSendSkipped = skipDigitalSend,
                    ShowPrintButton = needsPrint,
                    ProjectName = x.ProjectName,
                    SendMethod = DetermineSendMethod(x.OctopusDeliveryState, x.HasEmailLog, needsPrint),
                    BookyearLabel = ResolveBookyearLabel(x.InvoiceDate),
                    OctopusBookyearId = x.OctopusBookyearId,
                    OctopusJournalKey = x.OctopusJournalKey,
                    OctopusDocumentSequenceNr = x.OctopusDocumentSequenceNr,
                    OctopusBookedAt = x.OctopusBookedAt
                };
            })
                .OrderByDescending(x => x.InvoiceSortValue)
                .ThenByDescending(x => x.Id)
                .ToList();

            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerCompanyId);
            ViewBag.CompanyId = issuerCompanyId;
            ViewBag.CanWriteInvoices = writeScope.HasAccess && (writeScope.HasAllIssuers || writeScope.AllowedIssuerIds.Contains(issuerCompanyId));
            ViewBag.CanDeleteInvoices = deleteScope.HasAccess && (deleteScope.HasAllIssuers || deleteScope.AllowedIssuerIds.Contains(issuerCompanyId));
            ViewBag.DefaultBookyearLabel = bookyears.FirstOrDefault() is { } latestBookyear
              ? FormatBookyearLabel(latestBookyear.StartDate, latestBookyear.EndDate)
              : vms.Select(v => v.BookyearLabel).FirstOrDefault();
            SetIndexBreadcrumb(issuerCompanyId, ViewBag.CompanyName as string);
            SetPageHeader("bx bx-receipt", $"Facturen - {ViewBag.CompanyName}");
            // gl-v2 layout-pilot: zelfde data/query hierboven, enkel de view wisselt (design-handoff/).
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "IndexV2" : "Index", vms);
        }

        // BOEK FACTUREN

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> BookInvoices(int issuerCompanyId, int[] invoiceIds, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(issuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te boeken.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            var issuer = await _ics.GetAsync(issuerCompanyId, ct);
            if (issuer == null || string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
            {
                AddMessage("error", "Facturen kunnen niet geboekt worden omdat er geen Octopus-dossier is gekoppeld.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            if (invoiceIds == null || invoiceIds.Length == 0)
            {
                AddMessage("error", "Selecteer minstens één factuur om door te boeken.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            var bookedStatusId = (byte)InvoiceStatus.Booked;

            var selectedInvoices = await _db.Invoices
                .Where(i => i.IssuerCompanyId == issuerCompanyId && invoiceIds.Contains(i.Id))
                .ToListAsync(ct);

            var eligibleInvoices = selectedInvoices
                .Where(i => i.OctopusDocumentSequenceNr != null
                    && i.OctopusBookyearId != null
                    && !string.IsNullOrWhiteSpace(i.OctopusJournalKey))
                .ToList();

            if (eligibleInvoices.Count == 0)
            {
                AddMessage("error", "Er zijn geen geldige facturen geselecteerd om door te boeken.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            // Voorkom dat facturen uit verschillende boekjaren of dagboeken tegelijk doorgeboeked worden.
            // Octopus boekt per boekjaar+dagboek; cross-boekjaar selecties zouden facturen stilzwijgend negeren.
            var distinctBookyearJournals = eligibleInvoices
                .Select(i => new { i.OctopusBookyearId, i.OctopusJournalKey })
                .Distinct()
                .ToList();

            if (distinctBookyearJournals.Count > 1)
            {
                AddMessage("error", "De geselecteerde facturen vallen in verschillende boekjaren of dagboeken. Boek per boekjaar afzonderlijk.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            var targetInvoice = eligibleInvoices
                .OrderByDescending(i => i.OctopusDocumentSequenceNr)
                .First();

            if (InvoiceStatusExtensions.FromId(targetInvoice.StatusId) == InvoiceStatus.Generating)
            {
                AddMessage("error", "Deze factuur wordt momenteel gegenereerd. Probeer later opnieuw.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            if (targetInvoice.OctopusBookyearId is null
                || targetInvoice.OctopusDocumentSequenceNr is null
                || string.IsNullOrWhiteSpace(targetInvoice.OctopusJournalKey))
            {
                AddMessage("error", "Geen geldig boekjaar of dagboek gevonden bij de geselecteerde factuur.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            var unbookedInvoices = await _db.Invoices
                .Where(i => i.IssuerCompanyId == issuerCompanyId
                    && i.OctopusBookyearId == targetInvoice.OctopusBookyearId
                    && i.OctopusJournalKey == targetInvoice.OctopusJournalKey
                    && i.OctopusDocumentSequenceNr != null
                    && i.OctopusDocumentSequenceNr <= targetInvoice.OctopusDocumentSequenceNr
                    && i.OctopusBookedAt == null)
                .ToListAsync(ct);

            var selectedIds = eligibleInvoices.Select(i => i.Id).ToHashSet();
            if (unbookedInvoices.Any(i => !selectedIds.Contains(i.Id)))
            {
                AddMessage("error", "Selecteer eerst alle voorgaande facturen om door te boeken.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            var previousStatusId = targetInvoice.StatusId;
            targetInvoice.StatusId = (byte)InvoiceStatus.Generating;
            await _db.SaveChangesAsync(ct);

            try
            {
                var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(issuer.Id, issuer.OctopusDossierNumber, ct);

                var response = await _octopusClient.BookInvoiceAsync(
                    dossierToken.Token,
                    issuer.OctopusDossierNumber,
                    targetInvoice.OctopusBookyearId.Value,
                    targetInvoice.OctopusJournalKey!,
                    targetInvoice.OctopusDocumentSequenceNr.Value,
                    ct: ct);

                var bookingStatus = response?.BookingStatusList?.FirstOrDefault();
                if (response is null || !response.Success || bookingStatus is null || !bookingStatus.Success)
                {
                    var statusMessage = bookingStatus?.Comment;
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(statusMessage)
                        ? "Octopus gaf geen bevestiging terug bij het doorboeken van de facturen."
                        : statusMessage);
                }

                var bookedFrom = bookingStatus.DocumentKey?.DocumentSequenceNr
                      ?? targetInvoice.OctopusDocumentSequenceNr
                      ?? 0;
                var bookedUntil = targetInvoice.OctopusDocumentSequenceNr ?? bookedFrom;
                var bookedAt = DateTime.UtcNow;
                var bookedBy = ResolveUserName() ?? string.Empty;

                var invoicesToUpdate = await _db.Invoices
                    .Where(i => i.IssuerCompanyId == issuerCompanyId
                        && i.OctopusBookyearId == targetInvoice.OctopusBookyearId
                        && i.OctopusJournalKey == targetInvoice.OctopusJournalKey
                        && i.OctopusDocumentSequenceNr != null
                        && i.OctopusDocumentSequenceNr >= bookedFrom
                        && i.OctopusDocumentSequenceNr <= bookedUntil
                        && i.OctopusBookedAt == null)
                    .ToListAsync(ct);

                var generatingId = (byte)InvoiceStatus.Generating;
                if (invoicesToUpdate.Any(i => i.StatusId == generatingId && i.Id != targetInvoice.Id))
                {
                    AddMessage("error", "Eén of meerdere facturen worden momenteel gegenereerd. Probeer later opnieuw.", "Factuur");
                    targetInvoice.StatusId = previousStatusId;
                    await _db.SaveChangesAsync(ct);
                    return RedirectToAction(nameof(Index), new { issuerCompanyId });
                }


                foreach (var invoice in invoicesToUpdate)
                {
                    invoice.OctopusBookedAt = bookedAt;
                    invoice.OctopusBookedBy = bookedBy;
                    invoice.StatusId = bookedStatusId;
                }

                var sequenceSeriesId = targetInvoice.SeriesId
                      ?? await _db.InvoiceSeries
                          .Where(s => s.IssuerCompanyId == issuerCompanyId && s.IsActive)
                          .OrderBy(s => s.Id)
                          .Select(s => (int?)s.Id)
                          .FirstOrDefaultAsync(ct);

                if (sequenceSeriesId.HasValue)
                {
                    var sequenceToUpdate = await _db.InvoiceSequence
                        .Include(s => s.Bookyear)
                        .Include(s => s.Journal)
                        .FirstOrDefaultAsync(s =>
                            s.SeriesId == sequenceSeriesId.Value
                           && s.Bookyear != null
                            && s.Bookyear.BookyearKeyId == targetInvoice.OctopusBookyearId
                            && s.Journal != null
                            && s.Journal.JournalKey == targetInvoice.OctopusJournalKey,
                            ct);

                    if (sequenceToUpdate != null && bookedUntil > sequenceToUpdate.CurrentNumber)
                    {
                        sequenceToUpdate.CurrentNumber = bookedUntil;
                    }
                }


                await _db.SaveChangesAsync(ct);

                AddMessage("success", $"Facturen van nummer {bookedFrom} tot en met nummer {bookedUntil} geboekt.", "Factuur");
            }
            catch (Exception ex)
            {
                targetInvoice.StatusId = previousStatusId;
                await _db.SaveChangesAsync(ct);
                _logger.LogError(ex, "Octopus booking failed for issuer {IssuerCompanyId}", issuerCompanyId);
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Facturen konden niet geboekt worden."
                    : ex.Message;
                AddMessage("error", message, "Factuur");
            }

            return RedirectToAction(nameof(Index), new { issuerCompanyId });
        }

        //DETAIL FACTUUR
        [HttpGet]
        public async Task<IActionResult> Detail(int id, int? issuerCompanyId = null, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }

            var readScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess || (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze factuur te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            var deleteScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Delete, ct);
            ViewBag.CanWriteInvoices = writeScope.HasAccess && (writeScope.HasAllIssuers || writeScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId));
            ViewBag.CanDeleteInvoices = deleteScope.HasAccess && (deleteScope.HasAllIssuers || deleteScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId));

            await TryUpdateOctopusDeliveryStateAsync(detail, ct);

            var vm = MapDetail(detail);
            var emailLogs = await _communication.GetEmailLogsAsync(detail.Id, ct);
            var logItems = emailLogs
                .Select(log => new InvoiceEmailLogItemVM
                {
                    SentAt = log.SentAt,
                    To = log.ToAddress,
                    Cc = log.CcAddress,
                    Subject = log.Subject,
                    Status = log.Status
                })
                .ToList();
            vm.EmailLogs = logItems;
            vm.LastEmailSentAt = logItems.FirstOrDefault()?.SentAt;
            var issuerId = issuerCompanyId ?? detail.IssuerCompanyId;

            if (issuerId > 0)
            {
                ViewBag.CompanyId = issuerId;
                ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerId, ct);
            }
            var companyDisplay = (ViewBag.CompanyName as string) ?? vm.Issuer.LegalName ?? vm.Issuer.Name;
            var detailTitle = BuildInvoiceDetailBreadcrumbTitle(detail);
            SetDetailBreadcrumb(issuerId, companyDisplay);
            SetPageHeader("bx bx-receipt", detailTitle);

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailV2" : "Detail", vm);
        }

        //PDF EXPORT VAN FACTUUR
        [HttpGet]
        public async Task<IActionResult> Pdf(int id, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
                return NotFound();

            var readScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess || (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId)))
                return Forbid();

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
                return NotFound();

            var vatTypes = await _ics.ListVatTypeAsync(detail.IssuerCompanyId, ct);
            var dto = detail.ToInvoiceDto(vatTypes);
            var bytes = await RenderInvoicePdfWithAppendixAsync(detail.Id, dto, issuer, ct);
            var fileName = BuildInvoicePdfFileName(detail, dto);

            return File(bytes, "application/pdf", fileName);
        }

        [AllowAnonymous]
        [HttpGet("Invoices/OctopusDownload/{id:int}")]
        public async Task<IActionResult> OctopusDownload(int id, string token, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized();
            }

            string payload;
            try
            {
                payload = _downloadLinkProtector.Unprotect(token);
            }
            catch
            {
                return Unauthorized();
            }

            var parts = payload.Split('|');
            if (parts.Length < 3 || !int.TryParse(parts[0], out var tokenId) || tokenId != id)
            {
                return Unauthorized();
            }

            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(detail.PublicId) && !string.Equals(parts[1], detail.PublicId, StringComparison.Ordinal))
            {
                return Unauthorized();
            }

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
                return NotFound();

            var vatTypes = await _ics.ListVatTypeAsync(detail.IssuerCompanyId, ct);
            var dto = detail.ToInvoiceDto(vatTypes);
            dto.PublicId ??= detail.PublicId;
            var bytes = _pdf.Render(dto, issuer);
            var fileName = BuildInvoicePdfFileName(detail, dto);

            return File(bytes, "application/pdf", fileName);
        }

        //UBL EXPORT VAN FACTUUR
        [HttpGet]
        public async Task<IActionResult> Ubl(int id, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
                return NotFound();

            var readScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess || (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId)))
                return Forbid();

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
                return NotFound();

            var document = _ublBuilder.Build(detail, issuer);
            var fileName = string.IsNullOrWhiteSpace(document.FileName)
                ? $"{detail.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture)}.xml"
                : document.FileName;
            var bytes = Encoding.UTF8.GetBytes(document.Xml);

            return File(bytes, "application/xml", fileName);
        }


        // DELETE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Delete(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            var deleteScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Delete, ct);
            if (!deleteScope.HasAllIssuers && !deleteScope.AllowedIssuerIds.Contains(issuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te verwijderen.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            try
            {
                await _cmd.DeleteAsync(id, ct);
                AddMessage("success", "Factuur verwijderd.", "Factuur");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Delete invoice {InvoiceId} blocked", id);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete invoice {InvoiceId} failed", id);
                AddMessage("error", "Factuur kon niet verwijderd worden.", "Factuur");
            }

            return RedirectToAction(nameof(Index), new { issuerCompanyId });
        }

        // Gedeeld door ModalDelete (legacy Index.cshtml, magnific-popup) en ModalDeleteV2 (gl-v2
        // layout-pilot, Bootstrap-modal — optie 4j TYPE 1) — enkel de view die de VM rendert
        // verschilt tussen de twee, de rechten-/opzoeklogica is identiek.
        private async Task<(InvoiceDeleteConfirmVM? Vm, IActionResult? Error)> ResolveInvoiceDeleteConfirmAsync(int id, int issuerCompanyId, CancellationToken ct)
        {
            var deleteScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Delete, ct);
            if (!deleteScope.HasAllIssuers && !deleteScope.AllowedIssuerIds.Contains(issuerCompanyId))
                return (null, Content("<div class='p-3 text-danger'>Je hebt geen rechten om facturen voor dit facturatiebedrijf te verwijderen.</div>", "text/html"));

            var rows = await _invoices.GetByCompanyAsync(issuerCompanyId, ct);
            var row = rows.FirstOrDefault(r => r.Id == id);
            if (row == null)
                return (null, Content("<div class='p-3 text-danger'>Factuur niet gevonden.</div>", "text/html"));

            var vm = new InvoiceDeleteConfirmVM
            {
                Id = row.Id,
                IssuerCompanyId = issuerCompanyId,
                DisplayId = string.IsNullOrWhiteSpace(row.PublicId) ? $"[{row.Id}]" : row.PublicId,
                ClientName = row.ClientName,
                InvoiceDate = row.InvoiceDate,
                Status = TranslateStatus(row.StatusId, row.StatusName)
            };
            return (vm, null);
        }

        [HttpGet]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Invoicing)]
        public async Task<IActionResult> ModalDelete(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            var (vm, error) = await ResolveInvoiceDeleteConfirmAsync(id, issuerCompanyId, ct);
            if (error != null) return error;
            return PartialView("Modals/_ModalDeleteInvoice", vm);
        }

        // gl-v2 layout-pilot (design-handoff optie 4j TYPE 1) — eigen partial/markup, niet gedeeld
        // met Index.cshtml's magnific-popup/.modal-block-versie hierboven, zodat de legacy pagina
        // ongemoeid blijft. Zie Views/Invoices/Modals/_ModalDeleteInvoiceV2.cshtml.
        [HttpGet]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Invoicing)]
        public async Task<IActionResult> ModalDeleteV2(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            var (vm, error) = await ResolveInvoiceDeleteConfirmAsync(id, issuerCompanyId, ct);
            if (error != null) return error;
            return PartialView("Modals/_ModalDeleteInvoiceV2", vm);
        }

    }
}
