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
    [Authorize(Policy = "Permission:Boekhouding")]
    public class InvoicesController : Controller
    {
        private const string ControllerName = "Invoices";
        private readonly IInvoiceQueryService _invoices;
        private readonly ICompanyQueryService _companies;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IPartyLookupService _lookup;
        private readonly IInvoiceCommandService _cmd;
        private readonly IProjectSupplierLookupService _ps;
        private readonly IIssuerCompanyService _ics;
        private readonly IIssuerBankAccountService _bank;
        private readonly IInvoicePdfService _pdf;
        private readonly IInvoiceCommunicationService _communication;
        private readonly IInvoiceUblBuilder _ublBuilder;
        private readonly IEmailSender _emailSender;
        private readonly TemplateInterpolator _templateInterpolator;
        private readonly IPeppolDirectoryClient _peppolDirectory;
        private readonly IPeppolSender _peppolSender;
        private readonly IOctopusApiClient _octopusClient;
        private readonly IOctopusTokenManager _octopusTokens;
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;
        private readonly IDataProtector _downloadLinkProtector;
        private static readonly HashSet<string> RefreshableOctopusStates = new(StringComparer.OrdinalIgnoreCase)
        {
            string.Empty,
            "NONE",
            "ERROR",
            "PEPPOL_SENDING_IN_PROGRESS",
            "PEPPOL_SEND_FAILED"
        };
        private const string OctopusWorkflowStateCreated = "CREATED";
        private const string OctopusWorkflowStateAttachmentUploaded = "ATTACHMENT_UPLOADED";
        private const string OctopusWorkflowStateSent = "SENT";
        private static readonly Dictionary<string, int> OctopusWorkflowOrder = new(StringComparer.OrdinalIgnoreCase)
        {
            { OctopusWorkflowStateCreated, 1 },
            { OctopusWorkflowStateAttachmentUploaded, 2 },
            { OctopusWorkflowStateSent, 3 }
        };

        public InvoicesController(
            IInvoiceQueryService invoices,
            ICompanyQueryService companies,
            ILogger<InvoicesController> logger,
            IPartyLookupService lookup,
            IInvoiceCommandService cmd,
            IProjectSupplierLookupService ps,
            IIssuerCompanyService ics,
            IIssuerBankAccountService bank,
            IInvoicePdfService pdf,
            IInvoiceCommunicationService communication,
            IInvoiceUblBuilder ublBuilder,
            IEmailSender emailSender,
            TemplateInterpolator templateInterpolator,
            IPeppolDirectoryClient peppolDirectory,
            IPeppolSender peppolSender,
            IOctopusApiClient octopusClient,
            IOctopusTokenManager octopusTokens,
            IDataProtectionProvider dataProtectionProvider,
            UnitOfWorkCore uow)
        {
            _invoices = invoices;
            _companies = companies;
            _logger = logger;
            _lookup = lookup;
            _cmd = cmd;
            _ps = ps;
            _ics = ics;
            _bank = bank;
            _pdf = pdf;
            _communication = communication;
            _ublBuilder = ublBuilder;
            _emailSender = emailSender;
            _templateInterpolator = templateInterpolator;
            _peppolDirectory = peppolDirectory;
            _peppolSender = peppolSender;
            _octopusClient = octopusClient;
            _octopusTokens = octopusTokens;
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
            _downloadLinkProtector = dataProtectionProvider.CreateProtector("OctopusInvoiceDownload");
        }

        // LIST
        public async Task<IActionResult> Index(int issuerCompanyId)
        {
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
            ViewBag.DefaultBookyearLabel = bookyears.FirstOrDefault() is { } latestBookyear
              ? FormatBookyearLabel(latestBookyear.StartDate, latestBookyear.EndDate)
              : vms.Select(v => v.BookyearLabel).FirstOrDefault();
            SetIndexBreadcrumb(issuerCompanyId, ViewBag.CompanyName as string);
            return View(vms);
        }

        // BOEK FACTUREN

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookInvoices(int issuerCompanyId, int[] invoiceIds, CancellationToken ct = default)
        {
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
                    ct);

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
            var detailTitle = BuildInvoiceDisplayTitle(companyDisplay, detail.PublicId, detail.Id);
            SetDetailBreadcrumb(issuerId, companyDisplay, detail.Id, detailTitle);

            return View(vm);
        }

        //PDF EXPORT VAN FACTUUR
        [HttpGet]
        public async Task<IActionResult> Pdf(int id, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
                return NotFound();

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
                return NotFound();

            var vatTypes = await _ics.ListVatTypeAsync(detail.IssuerCompanyId, ct);
            var dto = detail.ToInvoiceDto(vatTypes);
            var bytes = _pdf.Render(dto, issuer);
            var fileName = string.IsNullOrWhiteSpace(dto.PublicId)
                ? $"Factuur_{dto.Id}.pdf"
                : $"{dto.PublicId}.pdf";

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
            var fileName = string.IsNullOrWhiteSpace(dto.PublicId)
                ? $"Factuur_{dto.Id}.pdf"
                : $"{dto.PublicId}.pdf";

            return File(bytes, "application/pdf", fileName);
        }

        //UBL EXPORT VAN FACTUUR
        [HttpGet]
        public async Task<IActionResult> Ubl(int id, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
                return NotFound();

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

        //FACTUUR VERZENDEN (GET)
        [HttpGet]
        public async Task<IActionResult> Send(int id, int? issuerCompanyId = null, string? mode = null, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
            {
                AddMessage("error", "Factuur verstrekker niet gevonden.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }

            var formMode = ParseSendMode(mode);
            var vm = await CreateSendViewModelAsync(detail, issuer, includeDefaults: true, checkPeppol: true, formMode, ct);

            var issuerId = issuerCompanyId ?? issuer.Id;
            if (issuerId > 0)
            {
                await SetIssuerViewBagsAsync(issuerId, ct);
            }
            var companyDisplay = (ViewBag.CompanyName as string) ?? vm.IssuerName;
            var detailTitle = BuildInvoiceDisplayTitle(companyDisplay, detail.PublicId, detail.Id);
            SetSendBreadcrumb(issuerId, companyDisplay, detail.Id, detailTitle);
            return View(vm);
        }

        //FACTUUR VERZENDEN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(InvoiceSendVM form, CancellationToken ct = default)
        {
            var submitMode = Request?.Form?["submitMode"].ToString();
            var isCopyRequest = form.IsCopyRequest || string.Equals(submitMode, "copy", StringComparison.OrdinalIgnoreCase);
            form.IsCopyRequest = isCopyRequest;
            if (!ModelState.IsValid)
            {
                // fall through to populate later
            }

            var detail = await _invoices.GetDetailAsync(form.InvoiceId, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index));
            }

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
            {
                AddMessage("error", "Factuur verstrekker niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index));
            }

            var formMode = isCopyRequest ? InvoiceSendFormMode.Copy : InvoiceSendFormMode.Standard;
            if (isCopyRequest)
            {
                form.AttachPdf = true;
                form.AttachUbl = false;
                form.SendToPeppol = false;
            }

            var vm = await CreateSendViewModelAsync(detail, issuer, includeDefaults: false, checkPeppol: true, formMode, ct);
            vm.To = form.To;
            vm.Cc = form.Cc;
            vm.Subject = form.Subject;
            vm.Body = form.Body;
            vm.AttachPdf = form.AttachPdf;
            vm.AttachUbl = form.AttachUbl;
            vm.SendToPeppol = form.SendToPeppol && vm.CanSendViaPeppol;
            vm.IsCopyRequest = isCopyRequest;
            vm.ForcePdfOnly = isCopyRequest;

            await SetIssuerViewBagsAsync(vm.IssuerCompanyId, ct);
            var companyDisplay = (ViewBag.CompanyName as string) ?? vm.IssuerName;
            var detailTitle = BuildInvoiceDisplayTitle(companyDisplay, detail.PublicId, detail.Id);
            SetSendBreadcrumb(vm.IssuerCompanyId, companyDisplay, vm.InvoiceId, detailTitle);

            if (!ModelState.IsValid)
                return View(vm);

            if (string.IsNullOrWhiteSpace(vm.To))
            {
                ModelState.AddModelError(nameof(vm.To), "E-mailadres is verplicht.");
                return View(vm);
            }

            if (!vm.AttachPdf && !vm.AttachUbl)
            {
                ModelState.AddModelError(string.Empty, "Selecteer minstens één bijlage.");
                return View(vm);
            }

            var attachments = new List<EmailAttachment>();
            var vatTypes = await _ics.ListVatTypeAsync(detail.IssuerCompanyId, ct);
            var dto = detail.ToInvoiceDto(vatTypes);
            var ublDocument = _ublBuilder.Build(detail, issuer);

            if (vm.AttachPdf)
            {
                var pdfBytes = _pdf.Render(dto, issuer);
                var pdfName = string.IsNullOrWhiteSpace(dto.PublicId)
                    ? $"Factuur_{dto.Id}.pdf"
                    : $"{dto.PublicId}.pdf";
                attachments.Add(new EmailAttachment(pdfName, pdfBytes, "application/pdf"));
            }

            if (vm.AttachUbl)
            {
                attachments.Add(new EmailAttachment(
                    string.IsNullOrWhiteSpace(ublDocument.FileName) ? $"{dto.PublicId ?? dto.Id.ToString()}_invoice.xml" : ublDocument.FileName,
                    Encoding.UTF8.GetBytes(ublDocument.Xml),
                    "application/xml"));
            }
            var sentAtUtc = DateTime.UtcNow;
            try
            {
                await _emailSender.SendEmailAsync(
                     vm.To,
                     vm.Subject,
                     vm.Body,
                     attachments,
                     vm.Cc,
                     issuer.InvoiceSendEmail,
                     issuer.InvoiceSendEmail);
                await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
                {
                    InvoiceId = detail.Id,
                    ToAddress = vm.To,
                    CcAddress = vm.Cc,
                    Subject = vm.Subject,
                    ProviderId = Guid.NewGuid().ToString(),
                    SentAt = sentAtUtc,
                    Status = "Sent"
                }, ct);
                await _cmd.MarkAsSentAsync(detail.Id, sentAtUtc, ct);
                AddMessage("success", "E-mail verzonden.", "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send invoice email {InvoiceId} failed", detail.Id);
                AddMessage("error", "E-mail kon niet verzonden worden.", "Factuur");
                ModelState.AddModelError(string.Empty, "E-mail kon niet verzonden worden. Probeer opnieuw.");
                return View(vm);
            }

            await _communication.SaveInvoiceUblAsync(new InvoiceUblBO
            {
                InvoiceId = detail.Id,
                XmlContent = ublDocument.Xml,
                UblVersion = ublDocument.UblVersion,
                Profile = ublDocument.Profile,
                GeneratedAt = ublDocument.GeneratedAt,
                SentViaPeppol = false,
                PeppolDocId = null
            }, ct);

            if (vm.SendToPeppol && vm.CanSendViaPeppol)
            {
                var participantId = vm.PeppolParticipantId
                    ?? detail.ClientVatNumber
                    ?? detail.ClientEnterpriseNumber;

                if (!string.IsNullOrWhiteSpace(participantId))
                {
                    var result = await _peppolSender.SendAsync(participantId, ublDocument.Xml, ct);
                    if (result.Success)
                    {
                        await _communication.SaveInvoiceUblAsync(new InvoiceUblBO
                        {
                            InvoiceId = detail.Id,
                            XmlContent = ublDocument.Xml,
                            UblVersion = ublDocument.UblVersion,
                            Profile = ublDocument.Profile,
                            GeneratedAt = ublDocument.GeneratedAt,
                            SentViaPeppol = true,
                            PeppolDocId = result.DocumentId
                        }, ct);
                        AddMessage("success", "Factuur verzonden via Peppol.", "Factuur");
                    }
                    else
                    {
                        AddMessage("warning", string.IsNullOrWhiteSpace(result.Message) ? "Peppol verzending mislukt." : result.Message, "Factuur");
                    }
                }
                else
                {
                    AddMessage("warning", "Geen geldig Peppol-ID beschikbaar.", "Factuur");
                }
            }

            return RedirectToAction(nameof(Send), new { id = detail.Id, issuerCompanyId = vm.IssuerCompanyId, mode = isCopyRequest ? "copy" : null });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOctopus(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            try
            {
                await SendInvoiceToOctopusAsync(id, ct, sendOnly: true, forceSendStep: true);
                AddMessage("success", "Factuur opnieuw via Octopus verstuurd.", "Factuur");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Octopus resend blocked for invoice {InvoiceId}", id);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Octopus resend failed for invoice {InvoiceId}", id);
                AddMessage("error", "Factuur kon niet opnieuw via Octopus verzonden worden.", "Factuur");
            }

            return RedirectToAction(nameof(Send), new { id, issuerCompanyId });
        }

        // DELETE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int issuerCompanyId, CancellationToken ct = default)
        {
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

        [HttpGet]
        public async Task<IActionResult> ModalDelete(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            var rows = await _invoices.GetByCompanyAsync(issuerCompanyId, ct);
            var row = rows.FirstOrDefault(r => r.Id == id);
            if (row == null)
                return Content("<div class='p-3 text-danger'>Factuur niet gevonden.</div>", "text/html");

            var vm = new InvoiceDeleteConfirmVM
            {
                Id = row.Id,
                IssuerCompanyId = issuerCompanyId,
                DisplayId = string.IsNullOrWhiteSpace(row.PublicId) ? $"[{row.Id}]" : row.PublicId,
                ClientName = row.ClientName,
                InvoiceDate = row.InvoiceDate,
                Status = TranslateStatus(row.StatusId, row.StatusName)
            };

            return PartialView("Modals/_ModalDeleteInvoice", vm);
        }

        //VAN DRAFT NAAR DEFINITIEF
        [HttpGet]
        public async Task<IActionResult> Issue(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            // ISSUE FLOW (van draft naar definitief + verzending via Octopus)
            // Volgorde:
            // 1) Bouw + verstuur Octopus-payloads (met "definitieve" PDF tijdens issue).
            // 2) Zet draft om naar definitief (IssueDraftAsync).
            // Opmerking: we forceren in de PDF geen proforma-label tijdens issue,
            // zodat de klant nooit een proforma-factuur ontvangt bij "Issue".
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
                await SendInvoiceToOctopusAsync(id, ct, forceFinalPdf: true);
                var publicId = await _cmd.IssueDraftAsync(id, issueDate: DateOnly.FromDateTime(DateTime.Today), ct: ct);
                if (!string.IsNullOrWhiteSpace(publicId))
                    AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
                else
                    AddMessage("success", "Factuur definitief gemaakt.", "Factuur");
            }
            catch (InvalidOperationException ex)
            {
                invoice.StatusId = previousStatusId;
                await _db.SaveChangesAsync(ct);
                _logger.LogWarning(ex, "Issue invoice {InvoiceId} blocked", id);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                invoice.StatusId = previousStatusId;
                await _db.SaveChangesAsync(ct);
                _logger.LogError(ex, "Issue invoice {InvoiceId} failed", id);
                AddMessage("error", "Factuur kon niet definitief gemaakt worden.", "Factuur");
            }

            return RedirectToAction(nameof(Index), new { issuerCompanyId });
        }

        // CREATE (GET)
        [HttpGet]
        public async Task<IActionResult> Create(int? issuerId = null, int? duplicateInvoiceId = null, CancellationToken ct = default)
        {


            InvoiceDetailBO? duplicateDetail = null;
            if (duplicateInvoiceId.HasValue && duplicateInvoiceId.Value > 0)
            {
                duplicateDetail = await _invoices.GetDetailAsync(duplicateInvoiceId.Value, ct);
                if (duplicateDetail == null)
                {
                    AddMessage("error", "Factuur niet gevonden om te dupliceren.", "Factuur");
                }
            }

            // gekozen issuer (param of eerste actieve)
            var selectedIssuerId = duplicateDetail?.IssuerCompanyId
                ?? issuerId
                ?? (await _ics.GetFirstActiveIssuerIdAsync(ct))
                ?? 0;

            // haal alles via service
            var issuersBo = await _ics.ListActiveIssuersAsync(ct);
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
                InvoiceDate = duplicateDetail?.InvoiceDate ?? DateOnly.FromDateTime(DateTime.Today),

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
                vm.IsCreditNote = duplicateDetail.IsCreditNote;
                vm.IsPrepaid = duplicateDetail.IsPrepaid;
                vm.Lines = MapLinesForCompose(duplicateDetail.Lines);

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
            ViewData["Title"] = "Nieuwe factuur";

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
                var rows = await _ps.SearchProjectsAsync(term ?? "", clientId, take, ct);
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

                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId,false, ct);
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


        // LIJNEN VOOR SCHIJVEN AANMAKEN 
        [HttpGet]
        public async Task<IActionResult> ComposeStageLines(int clientId, int? projectId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0)
                    return PartialView("_StageLinesTable", new List<InvoiceLineVM>());

                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId,false, ct);
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
        public async Task<IActionResult> CreateDraft(InvoiceComposeVM vm, CancellationToken ct)
        {
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
        public async Task<IActionResult> Save(InvoiceComposeVM vm, CancellationToken ct)
        {
            try
            {
                var bo = await BuildInvoiceDraftBoAsync(vm, ct);
                var issueNow = vm.StartAs == StartStatus.Invoice;
              

                if (!issueNow)
                {
                    var (_, publicId) = await _cmd.CreateWithLinesAsync(bo, issueNow: false, ct);
                    if (publicId != null)
                        AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
                    else
                        AddMessage("success", "Conceptfactuur opgeslagen.", "Factuur");

                    return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
                }

                var (id, _) = await _cmd.CreateWithLinesAsync(bo, issueNow: false, ct);

                try
                {
                    await SendInvoiceToOctopusAsync(id, ct, forceFinalPdf: true);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Octopus sync blocked for invoice {InvoiceId}", id);
                    AddMessage("error", ex.Message, "Factuur");
                    return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Octopus sync failed for invoice {InvoiceId}", id);
                    AddMessage("error", "Factuur kon niet naar Octopus verstuurd worden.", "Factuur");
                    return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
                }

                var issuedPublicId = await _cmd.IssueDraftAsync(id, issueDate: DateOnly.FromDateTime(DateTime.Today), ct: ct);

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


        [HttpGet]
        public async Task<IActionResult> Edit(int id, int? issuerCompanyId = null, string? returnUrl = null, CancellationToken ct = default)
        {
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

            var resolvedReturnUrl = DetermineReturnUrl(returnUrl);
            if (resolvedReturnUrl == null)
                resolvedReturnUrl = DetermineReturnUrl(Request.Headers["Referer"].ToString());

            if (string.Equals(detail.StatusName, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                var draftVm = await BuildDraftEditViewModelAsync(detail, null, resolvedReturnUrl, ct);
                await ConfigureEditContextAsync(detail, ct);
                return View("EditDraft", draftVm);
            }

            var vm = MapEdit(detail);
            vm.ReturnUrl = resolvedReturnUrl;
            await ConfigureEditContextAsync(detail, ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InvoiceEditVM vm, CancellationToken ct)
        {
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

            var resolvedReturnUrl = DetermineReturnUrl(vm.ReturnUrl);
            if (resolvedReturnUrl == null)
                resolvedReturnUrl = DetermineReturnUrl(Request.Headers["Referer"].ToString());

            if (string.Equals(detail.StatusName, "Draft", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Edit), new { id = vm.InvoiceId, issuerCompanyId = detail.IssuerCompanyId, returnUrl = resolvedReturnUrl });

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildEditViewModel(detail, vm);
                invalidVm.ReturnUrl = resolvedReturnUrl;
                await ConfigureEditContextAsync(detail, ct);
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
                IsDraft = string.Equals(detail.StatusName, "Draft", StringComparison.OrdinalIgnoreCase)
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
            return View(hydrated);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDraft(InvoiceDraftEditVM vm, CancellationToken ct)
        {
            if (vm == null || vm.InvoiceId <= 0)
                return RedirectToAction(nameof(Index));

            var detail = await _invoices.GetDetailAsync(vm.InvoiceId, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = vm.IssuerCompanyId });
            }

            var resolvedReturnUrl = DetermineReturnUrl(vm.ReturnUrl);
            if (resolvedReturnUrl == null)
                resolvedReturnUrl = DetermineReturnUrl(Request.Headers["Referer"].ToString());

            if (!string.Equals(detail.StatusName, "Draft", StringComparison.OrdinalIgnoreCase))
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
                return View("EditDraft", invalidVm);
            }

            try
            {
                var bo = await BuildInvoiceDraftBoAsync(vm, ct);
                await _cmd.UpdateDraftAsync(vm.InvoiceId, bo, ct);
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
                IsCreditNote = posted?.IsCreditNote ?? DetermineCreditNote(detail.IsCreditNote, detail.StatusName, detail.TotalInclVat),
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
                Lines = posted?.Lines != null ? posted.Lines.ToList() : MapLinesForCompose(detail.Lines),
                StageIds = posted?.StageIds != null ? new List<int>(posted.StageIds) : new List<int>(),
                TotalExclVat = RoundCurrency(detail.TotalExclVat),
                TotalVat = RoundCurrency(detail.TotalVat),
                TotalInclVat = RoundCurrency(detail.TotalInclVat),
                BankAccountIban = detail.BankAccount,
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

        private static List<InvoiceLineVM> MapLinesForCompose(IEnumerable<InvoiceLineBO> lines)
        {
            if (lines == null)
                return new List<InvoiceLineVM>();

            return lines.Select(l => new InvoiceLineVM
            {
                Text = l.Text ?? string.Empty,
                Price = l.Price,
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

        private static string BuildInvoiceDetailBreadcrumbTitle(InvoiceDetailBO detail)
        {
            var typeLabel = string.Equals(detail.StatusName, "Draft", StringComparison.OrdinalIgnoreCase)
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

            vm.Lines = bo.Lines.Select(MapDetailLine).ToList();
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
                Lines = (detail.Lines ?? Enumerable.Empty<InvoiceLineBO>()).Select(MapLineForEdit).ToList()
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

        private static InvoiceLineEditVM MapLineForEdit(InvoiceLineBO line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            var discount = line.DiscountAmount
                ?? (line.DiscountPercent.HasValue
                    ? Math.Round(line.Price * (line.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                    : 0m);

            var net = line.Price - discount;
            var vat = Math.Round(net * (line.VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);
            var gross = net + vat;

            return new InvoiceLineEditVM
            {
                LineId = line.Id,
                Text = line.Text ?? string.Empty,
                GroupName = line.GroupName,
                VatRate = line.VatPercentage,
                NetAmount = RoundCurrency(net),
                VatAmount = RoundCurrency(vat),
                GrossAmount = RoundCurrency(gross),
                DiscountAmount = discount != 0 ? RoundCurrency(discount) : (decimal?)null,
                DiscountPercent = line.DiscountPercent
            };
        }

        private static InvoiceDetailLineVM MapDetailLine(InvoiceLineBO line)
        {
            var editLine = MapLineForEdit(line);
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

        private async Task<InvoiceSendVM> CreateSendViewModelAsync(InvoiceDetailBO detail, IssuerCompanyBO issuer, bool includeDefaults, bool checkPeppol, InvoiceSendFormMode mode, CancellationToken ct)
        {
            var vm = new InvoiceSendVM
            {
                InvoiceId = detail.Id,
                IssuerCompanyId = issuer.Id,
                PublicId = string.IsNullOrWhiteSpace(detail.PublicId) ? null : detail.PublicId,
                InvoiceDate = detail.InvoiceDate,
                ClientName = detail.ClientName ?? string.Empty,
                ClientVatNumber = detail.ClientVatNumber,
                ClientEnterpriseNumber = detail.ClientEnterpriseNumber,
                ClientEmail = detail.ClientEmail,
                RequiresDigitalInvoice = detail.RequiresDigitalInvoice,
                DefaultAttachUbl = detail.AttachUblByDefault,
                IsSupplier = detail.IsSupplier,
                TotalExclVat = RoundCurrency(detail.TotalExclVat),
                TotalVat = RoundCurrency(detail.TotalVat),
                TotalInclVat = RoundCurrency(detail.TotalInclVat),
                IssuerName = issuer.Name ?? string.Empty,
                IssuerEmail = issuer.Email,
                IssuerPeppolEnabled = issuer.PeppolEnabled,
                IssuerPeppolId = issuer.PeppolParticipantId,
                BankAccount = !string.IsNullOrWhiteSpace(detail.BankAccount)
                    ? detail.BankAccount
                    : issuer.DefaultBankAccountIban ?? issuer.EpcIban,
                Currency = !string.IsNullOrWhiteSpace(issuer.DefaultCurrency) ? issuer.DefaultCurrency : "EUR",
                AttachPdf = true,
                AttachUbl = detail.AttachUblByDefault
            };

            vm.IsCopyRequest = mode == InvoiceSendFormMode.Copy;
            vm.ForcePdfOnly = vm.IsCopyRequest;

            if (vm.IsCopyRequest)
            {
                vm.AttachPdf = true;
                vm.AttachUbl = false;
                vm.SendToPeppol = false;
            }

            if (includeDefaults)
            {
                vm.To = detail.ClientEmail?.Trim() ?? string.Empty;
                var templateModel = BuildEmailTemplateModel(detail, issuer, vm.BankAccount, vm.Currency);

                var subjectTemplate = issuer.EmailSubjectTemplate;
                if (!string.IsNullOrWhiteSpace(subjectTemplate))
                {
                    var rendered = _templateInterpolator.Interpolate(subjectTemplate, templateModel).Trim();
                    vm.Subject = !string.IsNullOrWhiteSpace(rendered)
                        ? rendered
                        : $"Factuur {vm.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    vm.Subject = $"Factuur {vm.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture)}";
                }

                var bodyTemplate = issuer.EmailBodyTemplate;
                if (detail.IsPrepaid && !string.IsNullOrWhiteSpace(issuer.EmailPaidBodyTemplate))
                {
                    bodyTemplate = issuer.EmailPaidBodyTemplate;
                }
                if (!string.IsNullOrWhiteSpace(bodyTemplate))
                {
                    var rendered = _templateInterpolator.Interpolate(bodyTemplate, templateModel).Trim();
                    vm.Body = !string.IsNullOrWhiteSpace(rendered)
                        ? rendered
                        : BuildDefaultEmailBody(detail, issuer, vm.Currency, vm.BankAccount);
                }
                else
                {
                    vm.Body = BuildDefaultEmailBody(detail, issuer, vm.Currency, vm.BankAccount);
                }
                if (vm.IsCopyRequest && !string.IsNullOrWhiteSpace(vm.Subject))
                {
                    vm.Subject = $"Kopie - {vm.Subject}";
                }
            }

            var logs = await _communication.GetEmailLogsAsync(detail.Id, ct);
            vm.EmailLogs = logs
                .Select(log => new InvoiceEmailLogItemVM
                {
                    SentAt = log.SentAt,
                    To = log.ToAddress,
                    Cc = log.CcAddress,
                    Subject = log.Subject,
                    Status = log.Status
                })
                .ToList();
            vm.LastEmailSentAt = logs.FirstOrDefault()?.SentAt;

            var ubl = await _communication.GetInvoiceUblAsync(detail.Id, ct);
            if (ubl != null)
            {
                vm.ExistingUblGeneratedAt = ubl.GeneratedAt;
                vm.ExistingUblSentViaPeppol = ubl.SentViaPeppol;
                vm.ExistingUblDocumentId = ubl.PeppolDocId;
                vm.ExistingUblProfile = ubl.Profile;
                vm.ExistingUblVersion = ubl.UblVersion;
            }

            var octopusProgress = GetOctopusWorkflowProgress(detail.OctopusWorkflowState);
            var hasClientEmail = !string.IsNullOrWhiteSpace(detail.ClientEmail);
            vm.CanRetryOctopusSend = detail.RequiresDigitalInvoice
                && hasClientEmail
                && octopusProgress.CreationCompleted
                && octopusProgress.UploadCompleted;

            if (checkPeppol)
            {
                if (issuer.PeppolEnabled && !string.IsNullOrWhiteSpace(issuer.PeppolParticipantId))
                {
                    var lookupId = detail.ClientVatNumber ?? detail.ClientEnterpriseNumber;
                    if (!string.IsNullOrWhiteSpace(lookupId))
                    {
                        var participant = await _peppolDirectory.FindParticipantAsync(lookupId, ct);
                        if (participant != null)
                        {
                            vm.PeppolAccountFound = true;
                            vm.PeppolParticipantId = participant.ParticipantId;
                            vm.PeppolStatusMessage = $"Peppol-account gevonden: {participant.Name ?? participant.ParticipantId}";
                        }
                        else
                        {
                            vm.PeppolStatusMessage = "Geen Peppol-account gevonden.";
                        }
                    }
                    else
                    {
                        vm.PeppolStatusMessage = "Geen ondernemings- of btw-nummer beschikbaar.";
                    }

                    vm.CanSendViaPeppol = vm.PeppolAccountFound;
                    if (includeDefaults && !vm.IsCopyRequest)
                        vm.SendToPeppol = vm.PeppolAccountFound;
                }
                else
                {
                    vm.PeppolStatusMessage = "Peppol is niet geactiveerd voor dit bedrijf.";
                    vm.CanSendViaPeppol = false;
                }
            }

            return vm;
        }

        private static InvoiceSendFormMode ParseSendMode(string? mode)
        {
            return string.Equals(mode, "copy", StringComparison.OrdinalIgnoreCase)
                ? InvoiceSendFormMode.Copy
                : InvoiceSendFormMode.Standard;
        }

        // Hoofdflow voor Octopus-issue + verzending.
        // Volgorde wijzigen? Pas de genummerde stappen hieronder aan.
        private async Task SendInvoiceToOctopusAsync(
            int invoiceId,
            CancellationToken ct,
            bool sendOnly = false,
            bool forceSendStep = false,
            bool forceFinalPdf = false)
        {
            try
            {
                // 1. Algemene gegevens van de factuur voorbereiden (gestructureerde mededeling, QR-code/public id enz.)
                var context = await BuildOctopusInvoiceContextAsync(invoiceId, ct, forceFinalPdf);
                var progress = GetOctopusWorkflowProgress(context.Invoice.OctopusWorkflowState);

                // 2. Dossiertoken ophalen en valideren
                var dossierToken = await EnsureDossierTokenAsync(context, ct);

                if (progress.SendCompleted && !forceSendStep)
                    return;

                ValidateSendOnlyPreconditions(sendOnly, progress);

                // 3. Relatie ophalen of aanmaken/updaten
                await EnsureOctopusRelationAsync(context, dossierToken, sendOnly, ct, progress);

                // 4. Factuur aanmaken in Octopus API (CreateInvoiceAsync)
                await CreateOctopusInvoiceAsync(context, dossierToken, ct, progress);

                // 5. Document uploaden naar Octopus (UploadInvoiceAttachmentAsync)
                var attachment = await UploadInvoiceAttachmentAsync(context, dossierToken, sendOnly, ct, progress);

                // 6. Factuur verzenden via Octopus (SendInvoiceAsync)
                var sendResponse = await SendInvoiceViaOctopusAsync(context, dossierToken, ct);

                if (context.SkipSendStep)
                    return;

                var emailAttachment = attachment;
                if (sendResponse?.Success == true)
                {
                    if (await UpdateInvoicePublicIdFromOctopusAsync(context, ct))
                    {
                        emailAttachment = await BuildAttachmentAsync(context, ct);
                    }
                }

                // 7. Klassieke mailverzending indien geen Peppol (PDF identiek aan upload)
                var classicSent = await SendClassicInvoiceEmailIfRequiredAsync(context, emailAttachment, ct);

                if (classicSent && !WasSentViaPeppol(sendResponse))
                {
                    await PersistOctopusDeliveryStateAsync(
                        context.Invoice.Id,
                        new OctopusDocumentDeliveryState
                        {
                            DeliveryState = "EMAILED",
                            Comment = "Verzonden via klassieke e-mail",
                            DeliveryDateTime = DateTime.UtcNow
                        },
                        ct);
                }

                // 8. Logboeken en verzendgeschiedenis bijwerken
                await LogOctopusSendAsync(context, sendResponse, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Octopus request failed for invoice {InvoiceId}", invoiceId);
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Octopus gaf een onbekende fout terug."
                    : ex.Message;
                throw new InvalidOperationException($"Octopus gaf een fout terug bij het boeken van de factuur: {message}", ex);
            }

        }

        private sealed class OctopusInvoiceContext
        {
            public OctopusInvoiceContext(
                Invoices invoice,
                IssuerCompany issuer,
                InvoiceDetailBO detail,
                CompanyInfo? company,
                DateOnly finalDate,
                int fiscalYear,
                int documentSequenceNr,
                int bookyearId,
                string journalKey,
                int periodNumber,
                string structuredOgm,
                string formattedPublicId,
                bool isIndividual,
                bool skipSendStep,
                string? clientEmail,
                string? clientCc,
                bool forceFinalPdf)
            {
                Invoice = invoice;
                Issuer = issuer;
                Detail = detail;
                Company = company;
                FinalDate = finalDate;
                FiscalYear = fiscalYear;
                DocumentSequenceNr = documentSequenceNr;
                BookyearId = bookyearId;
                JournalKey = journalKey;
                PeriodNumber = periodNumber;
                StructuredOgm = structuredOgm;
                FormattedPublicId = formattedPublicId;
                IsIndividual = isIndividual;

                SkipSendStep = skipSendStep;
                ClientEmail = clientEmail;
                ClientCc = clientCc;
                ForceFinalPdf = forceFinalPdf;
            }

            public Invoices Invoice { get; }
            public IssuerCompany Issuer { get; }
            public InvoiceDetailBO Detail { get; }
            public CompanyInfo? Company { get; }
            public DateOnly FinalDate { get; }
            public int FiscalYear { get; }
            public int DocumentSequenceNr { get; set; }
            public int BookyearId { get; set; }
            public string JournalKey { get; set; }
            public int PeriodNumber { get; }
            public string StructuredOgm { get; set; }
            public string FormattedPublicId { get; set; }
            public bool IsIndividual { get; }

            public bool SkipSendStep { get; }
            public string? ClientEmail { get; }
            public string? ClientCc { get; }
            public bool ForceFinalPdf { get; }
            public int? OctopusRelationId { get; set; }
            public List<Vattype> VatTypes { get; } = new();
            public string DossierNumber => Issuer.OctopusDossierNumber ?? string.Empty;
        }

        private sealed class OctopusWorkflowProgress
        {
            public bool CreationCompleted { get; set; }
            public bool UploadCompleted { get; set; }
            public bool SendCompleted { get; set; }
        }

        private sealed class OctopusInvoiceAttachment
        {
            public OctopusInvoiceAttachment(byte[] pdfBytes, string fileName, string invoiceNumber)
            {
                PdfBytes = pdfBytes;
                FileName = fileName;
                InvoiceNumber = invoiceNumber;
            }

            public byte[] PdfBytes { get; }
            public string FileName { get; }
            public string InvoiceNumber { get; }
        }

        // Stap 1: laad alle data die nodig is voor Octopus + PDF (incl. status/nummerreeks).
        private async Task<OctopusInvoiceContext> BuildOctopusInvoiceContextAsync(int invoiceId, CancellationToken ct, bool forceFinalPdf)
        {
            var invoice = await _db.Invoices
                .Include(i => i.InvoicesDetails)
                .Include(i => i.IssuerCompany)
                .Include(i => i.PostalCode)!
                    .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.ClientContacts)
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.PostalCode)!
                        .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.InvoicePostalCode)!
                        .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientContactsNavigation)!
                    .ThenInclude(c => c.PostalCode)!
                        .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientContactsNavigation)!
                    .ThenInclude(c => c.InvoicePostalCode)!
                        .ThenInclude(pc => pc.Country)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
                ?? throw new InvalidOperationException("Factuur niet gevonden.");

            var issuer = invoice.IssuerCompany
                ?? throw new InvalidOperationException("Factuur heeft geen gekoppeld facturatiebedrijf.");

            CompanyInfo? company = null;
            if (invoice.CompanyId.HasValue)
            {
                company = await _db.CompanyInfo
                    .Include(c => c.PostCode)!
                        .ThenInclude(pc => pc.Country)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == invoice.CompanyId.Value, ct);
            }

            if (string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
                throw new InvalidOperationException("Octopus dossiernummer ontbreekt. Vul dit in bij het facturatiebedrijf.");

            var detail = await _invoices.GetDetailAsync(invoiceId, ct)
                  ?? throw new InvalidOperationException("Factuurdetails niet gevonden.");

            var recipientEmails = BuildInvoiceRecipients(
                invoice,
                invoice.ClientIdClientAccountNavigation,
                invoice.ClientIdClientContactsNavigation,
                detail);
            var clientEmail = recipientEmails.FirstOrDefault();
            var clientCc = recipientEmails.Count > 1
                ? string.Join(';', recipientEmails.Skip(1))
                : null;
            var hasCompanyName = !string.IsNullOrWhiteSpace(invoice.ClientIdClientAccountNavigation?.CompanyName)
                || !string.IsNullOrWhiteSpace(invoice.ClientIdClientContactsNavigation?.CompanyName);
            var isIndividual = !invoice.CompanyId.HasValue && !hasCompanyName;
            var requiresDigitalInvoice = detail.RequiresDigitalInvoice;
            var hasClientEmail = !string.IsNullOrWhiteSpace(clientEmail);
            var skipSendStep = !requiresDigitalInvoice || !hasClientEmail;

            var finalDate = invoice.Date == default
                ? DateOnly.FromDateTime(DateTime.Today)
                : invoice.Date;

            var seriesId = await _db.InvoiceSeries
                .Where(s => s.IssuerCompanyId == issuer.Id && s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Geen actieve nummerreeks voor dit facturatiebedrijf.");

            var sequencesQuery = _db.InvoiceSequence
                 .Include(s => s.Bookyear)!
                 .ThenInclude(b => b.OctopusBookyearPeriods)
                 .Include(s => s.Journal)
                 .AsNoTracking()
                 .Where(s => s.SeriesId == seriesId);

            var sequence = await sequencesQuery
                .FirstOrDefaultAsync(s =>
                    s.Bookyear != null
                    && finalDate >= DateOnly.FromDateTime(s.Bookyear.StartDate)
                    && finalDate <= DateOnly.FromDateTime(s.Bookyear.EndDate), ct);

            if (sequence == null)
            {
                var calendarYear = finalDate.Year;
                sequence = await sequencesQuery
                    .FirstOrDefaultAsync(s => s.FiscalYear == calendarYear, ct);
            }

            if (sequence == null)
                throw new InvalidOperationException("Geen sequentie gevonden voor deze factuurdatum. Maak een nieuwe sequentie aan voor het juiste boekjaar.");

            if (sequence.Bookyear == null || sequence.Journal == null)
                throw new InvalidOperationException("Koppel een Octopus-boekjaar en dagboek aan de nummerreeks voor dit boekjaar.");

            var fiscalYear = sequence.FiscalYear;


            var nextNumber = (sequence.CurrentNumber == 0 ? 0 : sequence.CurrentNumber) + 1;
            var documentSequenceNr = invoice.OctopusDocumentSequenceNr ?? nextNumber;
            var bookyearId = invoice.OctopusBookyearId ?? sequence.Bookyear.BookyearKeyId;
            var journalKey = string.IsNullOrWhiteSpace(invoice.OctopusJournalKey)
                ? sequence.Journal.JournalKey
                : invoice.OctopusJournalKey;

            var periodNumber = sequence.Bookyear.OctopusBookyearPeriods
                .FirstOrDefault(p =>
                {
                    var start = DateOnly.FromDateTime(p.StartDate);
                    var end = DateOnly.FromDateTime(p.EndDate);
                    return finalDate >= start && finalDate <= end;
                })?.BookyearPeriodNr
                ?? 0;

            var structuredOgm = BuildStructuredOgm(invoice, fiscalYear, documentSequenceNr);

            // Zorg dat gestructureerde mededeling en QR-payload up-to-date zijn voor we Octopus aanspreken
            var updatedQr = BuildEpcQrPayload(
                issuer,
                invoice.BankAccount,
                structuredOgm,
                detail.TotalInclVat);
            var needsSave = false;
            if (!string.IsNullOrWhiteSpace(structuredOgm) && !string.Equals(invoice.StructuredCommOgm, structuredOgm, StringComparison.Ordinal))
            {
                invoice.StructuredCommOgm = structuredOgm;
                needsSave = true;
            }
            if (!string.IsNullOrWhiteSpace(updatedQr) && !string.Equals(invoice.QrEpcPayload, updatedQr, StringComparison.Ordinal))
            {
                invoice.QrEpcPayload = updatedQr;
                detail.QrPayLoad = updatedQr;
                needsSave = true;
            }
            if (needsSave)
            {
                await _db.SaveChangesAsync(ct);
            }

            // Zorg dat de detailweergave dezelfde gegevens heeft als de factuur zelf
            detail.StructuredMessage = string.IsNullOrWhiteSpace(structuredOgm)
                ? detail.StructuredMessage
                : structuredOgm;

            var formattedPublicId = string.IsNullOrWhiteSpace(invoice.PublicId)
                ? FormatInvoiceNumber(issuer.InvoiceNumberPattern, documentSequenceNr, finalDate)
                : invoice.PublicId;

            detail.PublicId ??= formattedPublicId;

            return new OctopusInvoiceContext(
                invoice,
                issuer,
                detail,
                company,
                finalDate,
                fiscalYear,
                documentSequenceNr,
                bookyearId,
                journalKey,
                periodNumber,
                structuredOgm,
                formattedPublicId,
                isIndividual,
                skipSendStep,
                clientEmail,
                clientCc,
                forceFinalPdf);
        }

        private static List<string> BuildInvoiceRecipients(
            Invoices invoice,
            ClientAccount? account,
            ClientContacts? contact,
            InvoiceDetailBO detail)
        {
            var recipients = new List<string>();

            if (invoice.ClientType == (int)InvoicePartyType.ClientContact && contact != null)
            {
                if (contact.RequiresDigitalInvoice)
                {
                    AddRecipient(recipients, contact.InvoiceEmail, contact.Email);
                }

                return recipients;
            }

            if (invoice.ClientType == (int)InvoicePartyType.ClientAccount && account != null)
            {
                if (account.RequiresDigitalInvoice)
                {
                    AddRecipient(recipients, account.InvoiceEmail, account.Email);
                }

                if (account.ClientContacts != null)
                {
                    foreach (var accountContact in account.ClientContacts.Where(c => !c.IsCoOwner && c.RequiresDigitalInvoice))
                    {
                        AddRecipient(recipients, accountContact.InvoiceEmail, accountContact.Email);
                    }
                }
            }

            if (recipients.Count == 0 && invoice.ClientType != (int)InvoicePartyType.ClientAccount)
            {
                AddRecipient(recipients, detail.ClientEmail, null);
            }

            return recipients;
        }

        private static void AddRecipient(List<string> recipients, string? invoiceEmail, string? email)
        {
            var candidate = ResolvePreferredEmail(invoiceEmail, email);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            if (recipients.Any(r => string.Equals(r, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            recipients.Add(candidate);
        }

        private static string? ResolvePreferredEmail(string? invoiceEmail, string? email)
        {
            if (!string.IsNullOrWhiteSpace(invoiceEmail))
            {
                return invoiceEmail.Trim();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Trim();
            }

            return null;
        }

        // Stap 2b: extra guardrails als we enkel het verzenddeel willen herhalen.
        private static void ValidateSendOnlyPreconditions(bool sendOnly, OctopusWorkflowProgress progress)
        {
            if (!sendOnly)
                return;

            if (!progress.CreationCompleted)
                throw new InvalidOperationException("Factuur moet eerst in Octopus aangemaakt worden.");

            if (!progress.UploadCompleted)
                throw new InvalidOperationException("Factuur moet eerst als bijlage naar Octopus geüpload worden.");
        }

        // Stap 2: dossiertoken ophalen zodat alle Octopus-calls geauthenticeerd zijn.
        private async Task<OctopusDossierTokenResult> EnsureDossierTokenAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            return await _octopusTokens.RefreshDossierTokenAsync(context.Issuer.Id, context.DossierNumber, ct);
        }

        // Stap 3: zorg dat de Octopus-relatie bestaat en koppel lokale relation IDs.
        private async Task EnsureOctopusRelationAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            bool sendOnly,
            CancellationToken ct,
            OctopusWorkflowProgress progress)
        {
            if (progress.CreationCompleted)
                return;

            if (sendOnly)
                throw new InvalidOperationException("Octopus-factuur is nog niet aangemaakt.");

            var issuerRelationId = await GetIssuerSpecificOctopusRelationIdAsync(
                 context.Issuer.Id,
                 context.Invoice.ClientIdClientAccountNavigation,
                 context.Invoice.ClientIdClientContactsNavigation,
                 context.Company,
                 ct);

            var relationLookups = BuildRelationLookups(
                context.Invoice,
                context.Invoice.ClientIdClientAccountNavigation,
                context.Invoice.ClientIdClientContactsNavigation,
                context.Company,
                issuerRelationId);
            if (!relationLookups.Any())
            {
                throw new InvalidOperationException("Onvoldoende klantgegevens om de Octopus-relatie te bepalen.");
            }

            var relation = await EnsureOctopusRelationAsync(
                dossierToken.Token,
                context.DossierNumber,
                relationLookups,
                BuildRelationRequest(
                    context.Invoice,
                    context.Invoice.ClientIdClientAccountNavigation,
                    context.Invoice.ClientIdClientContactsNavigation,
                    context.Company,
                    issuerRelationId),
                ct);

            var relationId = relation?.RelationIdentificationServiceData?.RelationKey?.Id
                ?? issuerRelationId
                ?? context.Invoice.ClientIdClientAccountNavigation?.OctopusRelationId
                ?? context.Invoice.ClientIdClientContactsNavigation?.OctopusRelationId
                ?? context.Company?.OctopusRelationId;

            if (relationId is null or <= 0)
            {
                throw new InvalidOperationException("Octopus-relatie kon niet bepaald worden.");
            }

            context.OctopusRelationId = relationId;
            await UpdateLocalRelationIdsAsync(context, relationId, ct);

            context.VatTypes.Clear();
            var vatTypes = await _db.Vattype
                .Where(v => v.IssuerCompanyId == context.Issuer.Id)
                .AsNoTracking()
                .ToListAsync(ct);
            context.VatTypes.AddRange(vatTypes);
        }

        // Stap 3b: sla relationId ook lokaal op, zodat volgende calls sneller zijn.
        private async Task UpdateLocalRelationIdsAsync(OctopusInvoiceContext context, int? relationId, CancellationToken ct)
        {
            if (!relationId.HasValue || relationId.Value <= 0)
            {
                return;
            }

            var issuerRelationId = relationId.Value;

            await SetIssuerSpecificRelationIdAsync(
                context.Issuer.Id,
                context.Invoice.ClientIdClientAccountNavigation,
                context.Invoice.ClientIdClientContactsNavigation,
                context.Company,
                issuerRelationId,
                ct);

            var saveRequired = false;

            if (context.Invoice.ClientIdClientAccountNavigation is ClientAccount clientAccount
                && (clientAccount.OctopusRelationId is null or <= 0))
            {
                clientAccount.OctopusRelationId = issuerRelationId;
                saveRequired = true;
            }
            else if (context.Invoice.ClientIdClientContactsNavigation is ClientContacts clientContact
                && (clientContact.OctopusRelationId is null or <= 0))
            {
                clientContact.OctopusRelationId = issuerRelationId;
                saveRequired = true;
            }
            else if (context.Company != null && (context.Company.OctopusRelationId is null or <= 0))
            {
                var trackedCompany = await _db.CompanyInfo.FirstOrDefaultAsync(c => c.CompanyId == context.Company.CompanyId, ct);

                if (trackedCompany != null && (trackedCompany.OctopusRelationId is null or <= 0))
                {
                    trackedCompany.OctopusRelationId = issuerRelationId;
                    saveRequired = true;
                }
            }

            if (saveRequired)
            {
                await _db.SaveChangesAsync(ct);
            }
        }

        // Stap 4: maak factuur aan in Octopus (booking + lijnen).
        private async Task CreateOctopusInvoiceAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            CancellationToken ct,
            OctopusWorkflowProgress progress)
        {
            if (progress.CreationCompleted)
                return;

            var relationId = context.OctopusRelationId
                ?? context.Invoice.ClientIdClientAccountNavigation?.OctopusRelationId
                ?? context.Invoice.ClientIdClientContactsNavigation?.OctopusRelationId
                ?? context.Company?.OctopusRelationId;

            if (relationId is null or <= 0)
            {
                throw new InvalidOperationException("Octopus-relatie kon niet bepaald worden.");
            }

            var customFieldValues = BuildInvoiceCustomFieldValues(context.Invoice, context.Issuer, context.FormattedPublicId);

            var payload = new OctopusInvoiceCreateRequest
            {
                BookyearKey = new OctopusBookyearKeyRef { Id = context.BookyearId },
                JournalKey = context.JournalKey,
                DocumentSequenceNr = context.DocumentSequenceNr,
                BookyearPeriodeNr = context.PeriodNumber,
                DocumentDate = context.FinalDate,
                ExpiryDate = context.Invoice.ExpirationDate ?? context.FinalDate,
                CurrencyCode = string.IsNullOrWhiteSpace(context.Invoice.CurrencyCode) ? "EUR" : context.Invoice.CurrencyCode!,
                ExchangeRate = 1m,
                RelationIdentificationServiceData = new OctopusRelationIdentificationServiceData
                {
                    RelationKey = new OctopusRelationKeyRef { Id = relationId ?? 0 },
                    ExternalRelationId = context.Invoice.ClientIdClientAccountNavigation?.OctopusRelationId
                        ?? context.Invoice.ClientId
                        ?? context.Invoice.CompanyId
                        ?? relationId
                        ?? 0
                },
                Comment = context.Invoice.Text,
                OrderReference = null,
                Reference = context.StructuredOgm,
                FinancialDiscount = 0m,
                CustomFieldValueList = customFieldValues,
                InvoiceLines = context.Invoice.InvoicesDetails.Select(line => new OctopusInvoiceLineRequest
                {
                    ExternProductNr = null,
                    Description = line.Text,
                    Count = 1,
                    Unit = string.Empty,
                    UnitPrice = line.Price ?? 0m,
                    DiscountPercentage = line.DiscountPercent ?? 0m,
                    VatCodeKey = ResolveVatCodeKey(line, context.VatTypes, context.Issuer.DefaultVatTypeId),
                    BookingAccountNr = 0,
                    //CostCentreKey = new OctopusCostCentreKeyRef { Id = 0 },
                    CustomFieldValueList = new List<OctopusCustomFieldValue>(),
                    IntrastatServiceData = null
                }).ToList()
            };

            var created = await _octopusClient.CreateInvoiceAsync(dossierToken.Token, context.DossierNumber, payload, ct);
            if (!created)
            {
                throw new InvalidOperationException("Octopus gaf geen bevestiging terug bij het aanmaken van de factuur.");
            }

            context.Invoice.OctopusBookyearId = context.BookyearId;
            context.Invoice.OctopusJournalKey = context.JournalKey;
            context.Invoice.OctopusDocumentSequenceNr = context.DocumentSequenceNr;
            context.Invoice.OctopusDeliveryState ??= "NONE";
            await PersistOctopusWorkflowStateAsync(context.Invoice, OctopusWorkflowStateCreated, ct);
            UpdateProgressFromState(progress, context.Invoice.OctopusWorkflowState);
        }

        //private OctopusBuySellBookingAndAttachmentRequest BuildBuySellBookingPayload(OctopusInvoiceContext context, int relationId)
        //{
        //    var currency = string.IsNullOrWhiteSpace(context.Invoice.CurrencyCode)
        //        ? "EUR"
        //        : context.Invoice.CurrencyCode!;

        //    var bookingLines = context.Invoice.InvoicesDetails.Select(line =>
        //    {
        //        var baseAmount = line.Price ?? 0m;
        //        var vatPercentage = line.VatPercentage ?? 0m;

        //        return new OctopusBuySellBookingLineServiceData
        //        {
        //            AccountKey = 0,
        //            BaseAmount = baseAmount,
        //            VatCodeKey = ResolveVatCodeKey(line, context.VatTypes, context.Issuer.DefaultVatTypeId),
        //            VatAmount = Math.Round(baseAmount * vatPercentage / 100m, 2, MidpointRounding.AwayFromZero),
        //            Comment = line.Text,
        //            VatRecupPercentage = 0m
        //        };
        //    }).ToList();

        //    return new OctopusBuySellBookingAndAttachmentRequest
        //    {
        //        BuySellBookingServiceData = new OctopusBuySellBookingServiceData
        //        {
        //            BookyearKey = new OctopusBookyearKeyRef { Id = context.BookyearId },
        //            JournalKey = context.JournalKey,
        //            DocumentSequenceNr = context.DocumentSequenceNr,
        //            RelationIdentificationServiceData = new OctopusRelationIdentificationServiceData
        //            {
        //                RelationKey = new OctopusRelationKeyRef { Id = relationId },
        //                ExternalRelationId = context.Invoice.ClientIdClientAccountNavigation?.OctopusRelationId
        //                    ?? context.Invoice.ClientId
        //                    ?? context.Invoice.CompanyId
        //                    ?? relationId
        //            },
        //            BookyearPeriodeNr = context.PeriodNumber,
        //            DocumentDate = context.FinalDate,
        //            ExpiryDate = context.Invoice.ExpirationDate ?? context.FinalDate,
        //            Comment = context.Invoice.Text,
        //            Reference = context.StructuredOgm,
        //            Amount = context.Detail.TotalInclVat,
        //            CurrencyCode = currency,
        //            ExchangeRate = 1m,
        //            BookingLines = bookingLines,
        //            PaymentMethod = 0
        //        }
        //    };
        //}

        // Stap 5: genereer PDF en upload naar Octopus (wordt ook gebruikt voor e-mail).
        private async Task<OctopusInvoiceAttachment> UploadInvoiceAttachmentAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            bool sendOnly,
            CancellationToken ct,
            OctopusWorkflowProgress progress)
        {
            if (progress.UploadCompleted)
            {
                // Upload is reeds gebeurd, maar we hebben nog steeds de PDF-inhoud nodig voor eventuele mailverzending.
                return await BuildAttachmentAsync(context, ct);
            }

            if (sendOnly)
                throw new InvalidOperationException("Octopus-factuurbijlage is nog niet geüpload.");

            var attachment = await BuildAttachmentAsync(context, ct);

            var uploaded = await _octopusClient.UploadInvoiceAttachmentAsync(
                dossierToken.Token,
                context.DossierNumber,
                new OctopusInvoiceAttachmentUploadRequest
                {
                    BookyearId = context.BookyearId,
                    JournalKey = context.JournalKey,
                    DocumentSequenceNumber = context.DocumentSequenceNr,
                    InvoiceNumber = attachment.InvoiceNumber,
                    AttachmentType = "Invoice",
                    FileName = attachment.FileName,
                    Content = attachment.PdfBytes,
                    ContentType = "application/pdf"
                },
                ct);

            if (!uploaded)
            {
                throw new InvalidOperationException("Octopus gaf geen bevestiging terug bij het uploaden van de factuurbijlage.");
            }

            await PersistOctopusWorkflowStateAsync(context.Invoice, OctopusWorkflowStateAttachmentUploaded, ct);
            UpdateProgressFromState(progress, context.Invoice.OctopusWorkflowState);

            return attachment;
        }

        // Stap 5b: bouw de PDF-bijlage (status kan "forced final" zijn tijdens issue).
        private async Task<OctopusInvoiceAttachment> BuildAttachmentAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            var issuerCompany = await _ics.GetAsync(context.Issuer.Id, ct)
                ?? throw new InvalidOperationException("Facturatiebedrijf niet gevonden voor PDF-opmaak.");

            var vatTypes = context.VatTypes
                .Select(v => new VatTypeBO
                {
                    Id = v.Id,
                    IssuerCompanyId = v.IssuerCompanyId,
                    Code = v.Code,
                    Description = v.Description,
                    Type = v.Type,
                    BasePercentage = v.BasePercentage,
                    DefaultSellBookingAccountNr = v.DefaultSellBookingAccountNr,
                    InvoiceMention = v.InvoiceMention
                })
                .ToList();

            var invoiceDto = context.Detail.ToInvoiceDto(vatTypes);
            invoiceDto.PublicId ??= context.FormattedPublicId;
            if (context.ForceFinalPdf)
            {
                // Zorg dat de PDF nooit als proforma wordt aangemerkt tijdens issue.
                invoiceDto.Status = "Issued";
            }
            if (!string.IsNullOrWhiteSpace(context.StructuredOgm))
            {
                invoiceDto.StructuredMessage = context.StructuredOgm;
            }

            var pdfBytes = _pdf.Render(invoiceDto, issuerCompany);
            var invoiceNumber = context.FormattedPublicId;
            var invalidChars = Path.GetInvalidFileNameChars();
            var safePublicId = string.IsNullOrWhiteSpace(context.FormattedPublicId) ? context.Invoice.Id.ToString(CultureInfo.InvariantCulture) : context.FormattedPublicId;
            var sanitizedId = new string((safePublicId ?? string.Empty).Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
            var fileName = $"factuur {sanitizedId}.pdf";

            return new OctopusInvoiceAttachment(pdfBytes, fileName, invoiceNumber);
        }

        // Stap 6: vraag Octopus om de factuur te verzenden (externe verzending).
        private async Task<OctopusInvoiceSendResponse?> SendInvoiceViaOctopusAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            CancellationToken ct)
        {
            if (context.SkipSendStep)
            {
                _logger.LogInformation(
                      "Skipping send for invoice {InvoiceId}: digital delivery not required or no destination email",
                    context.Invoice.Id);
                return null;
            }

            var sendRequest = new OctopusInvoiceSendRequest
            {
                BookyearKey = new OctopusBookyearKeyRef { Id = context.BookyearId },
                Journal = context.JournalKey,
                DocumentSequenceNr = context.DocumentSequenceNr,
                ToDocumentSequenceNr = context.DocumentSequenceNr,
                FromMailAddress = string.IsNullOrWhiteSpace(context.Issuer.InvoiceSendEmail)
                    ? context.Issuer.Email
                    : context.Issuer.InvoiceSendEmail,
                CcMailAddress = null,
                BccMailAddress = null,
                ExcludeOctopusPdf = true,
                ForceUseEmail = false
            };

            var sendResponse = await _octopusClient.SendInvoiceAsync(
                dossierToken.Token,
                context.DossierNumber,
                sendRequest,
                ct);

            if (sendResponse is null || !sendResponse.Success)
            {
                _logger.LogWarning(
                    "Octopus send failed for invoice {InvoiceId} in dossier {DossierNumber}. Continuing with classic send.",
                    context.Invoice.Id,
                    context.DossierNumber);
                return sendResponse;
            }

            var sendDocumentKey = sendResponse?.SendInvoiceStatusList?.FirstOrDefault()?.DocumentKey;
            context.DocumentSequenceNr = sendDocumentKey?.DocumentSequenceNr > 0
                ? sendDocumentKey!.DocumentSequenceNr
                : context.DocumentSequenceNr;

            context.Invoice.OctopusBookyearId = sendDocumentKey?.BookyearKey?.Id ?? context.BookyearId;
            context.Invoice.OctopusJournalKey = string.IsNullOrWhiteSpace(sendDocumentKey?.Journal)
                ? context.JournalKey
                : sendDocumentKey!.Journal;
            context.Invoice.OctopusDocumentSequenceNr = context.DocumentSequenceNr;
            context.Invoice.OctopusDeliveryState ??= "NONE";
            context.Invoice.OctopusDeliveryUpdatedAt = DateTime.UtcNow;
            await PersistOctopusWorkflowStateAsync(context.Invoice, OctopusWorkflowStateSent, ct);

            return sendResponse;
        }

        // Stap 6b: publicId aanpassen op basis van Octopus-nummering (indien beschikbaar).
        private async Task<bool> UpdateInvoicePublicIdFromOctopusAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            if (context.DocumentSequenceNr <= 0)
                return false;

            var formattedPublicId = FormatInvoiceNumber(
                context.Issuer.InvoiceNumberPattern,
                context.DocumentSequenceNr,
                context.FinalDate);
            var structuredOgm = GenerateStructuredOgm(context.FiscalYear, context.DocumentSequenceNr);

            var updated = false;

            if (!string.Equals(context.Invoice.PublicId, formattedPublicId, StringComparison.Ordinal))
            {
                context.Invoice.PublicId = formattedPublicId;
                updated = true;
            }

            if (!string.Equals(context.Detail.PublicId, formattedPublicId, StringComparison.Ordinal))
            {
                context.Detail.PublicId = formattedPublicId;
                updated = true;
            }

            if (!string.Equals(context.FormattedPublicId, formattedPublicId, StringComparison.Ordinal))
            {
                context.FormattedPublicId = formattedPublicId;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(structuredOgm)
                && !string.Equals(context.Invoice.StructuredCommOgm, structuredOgm, StringComparison.Ordinal))
            {
                context.Invoice.StructuredCommOgm = structuredOgm;
                context.Detail.StructuredMessage = structuredOgm;
                context.StructuredOgm = structuredOgm;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(structuredOgm))
            {
                var updatedQr = BuildEpcQrPayload(
                    context.Issuer,
                    context.Detail.BankAccount,
                    structuredOgm,
                    context.Detail.TotalInclVat);

                if (!string.IsNullOrWhiteSpace(updatedQr)
                    && !string.Equals(context.Invoice.QrEpcPayload, updatedQr, StringComparison.Ordinal))
                {
                    context.Invoice.QrEpcPayload = updatedQr;
                    context.Detail.QrPayLoad = updatedQr;
                    updated = true;
                }
            }

            if (updated)
            {
                await _db.SaveChangesAsync(ct);
            }

            return updated;
        }

        // Stap 7: klassieke e-mailverzending als er geen (of geen succesvolle) digitale route is.
        private async Task<bool> SendClassicInvoiceEmailIfRequiredAsync(
           OctopusInvoiceContext context,
           OctopusInvoiceAttachment attachment,
           CancellationToken ct)
        {
            // Wanneer Peppol niet van toepassing is, kan de Octopus-stroom aangevuld worden met een klassieke mail.
            // Het PDF-bestand is identiek aan de upload naar Octopus.
            if (context.SkipSendStep || string.IsNullOrWhiteSpace(context.ClientEmail))
                return false;

            try
            {
                var issuerBo = await _ics.GetAsync(context.Issuer.Id, ct)
                    ?? throw new InvalidOperationException("Facturatiebedrijf niet gevonden voor e-mailopmaak.");

                var templateModel = BuildEmailTemplateModel(
                    context.Detail,
                    issuerBo,
                    context.Detail.BankAccount,
                    context.Detail.Currency ?? "EUR");

                var subjectTemplate = issuerBo.EmailSubjectTemplate;
                var renderedSubject = !string.IsNullOrWhiteSpace(subjectTemplate)
                    ? _templateInterpolator.Interpolate(subjectTemplate, templateModel).Trim()
                    : string.Empty;
                var mailSubject = !string.IsNullOrWhiteSpace(renderedSubject)
                    ? renderedSubject
                    : $"Factuur {context.FormattedPublicId ?? context.Invoice.Id.ToString(CultureInfo.InvariantCulture)}";

                var bodyTemplate = issuerBo.EmailBodyTemplate;
                if (context.Detail.IsPrepaid && !string.IsNullOrWhiteSpace(issuerBo.EmailPaidBodyTemplate))
                {
                    bodyTemplate = issuerBo.EmailPaidBodyTemplate;
                }
                var renderedBody = !string.IsNullOrWhiteSpace(bodyTemplate)
                    ? _templateInterpolator.Interpolate(bodyTemplate, templateModel).Trim()
                    : string.Empty;
                var mailBody = !string.IsNullOrWhiteSpace(renderedBody)
                    ? renderedBody
                    : BuildDefaultEmailBody(context.Detail, issuerBo, context.Detail.Currency ?? "EUR", context.Detail.BankAccount ?? string.Empty);

                await _emailSender.SendEmailAsync(
                    context.ClientEmail,
                    mailSubject,
                    mailBody,
                    new List<EmailAttachment> { new(attachment.FileName, attachment.PdfBytes, "application/pdf") },
                    context.ClientCc,
                    context.Issuer.InvoiceSendEmail,
                    context.Issuer.InvoiceSendEmail);

                await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
                {
                    InvoiceId = context.Invoice.Id,
                    ToAddress = context.ClientEmail,
                    CcAddress = context.ClientCc,
                    Subject = mailSubject,
                    ProviderId = context.DossierNumber,
                    SentAt = DateTime.UtcNow,
                    Status = "Sent"
                }, ct);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Classic invoice email failed for invoice {InvoiceId}", context.Invoice.Id);
                return false;
            }
        }
        // Stap 8: logging + verzendgeschiedenis bijwerken.
        private async Task LogOctopusBookingAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";

            await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
            {
                InvoiceId = context.Invoice.Id,
                ToAddress = "Octopus",
                CcAddress = userName,
                Subject = $"Boeking aangemaakt in Octopus door {userName}",
                ProviderId = context.DossierNumber,
                SentAt = DateTime.UtcNow,
                Status = "Booked"
            }, ct);
        }

        private async Task LogOctopusSendAsync(
            OctopusInvoiceContext context,
            OctopusInvoiceSendResponse? sendResponse,
            CancellationToken ct)
        {
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";
            var sentAtUtc = DateTime.UtcNow;

            await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
            {
                InvoiceId = context.Invoice.Id,
                ToAddress = "Octopus",
                CcAddress = userName,
                Subject = $"Doorgestuurd naar Octopus door {userName}",
                ProviderId = context.DossierNumber,
                SentAt = sentAtUtc,
                Status = "Uploaded"
            }, ct);

            if (sendResponse?.SendInvoiceStatusList != null)
            {
                foreach (var status in sendResponse.SendInvoiceStatusList)
                {
                    var method = string.IsNullOrWhiteSpace(status.SendMethod) ? "Octopus" : status.SendMethod!;
                    var subject = $"Octopus verzendstatus ({method})";
                    if (!string.IsNullOrWhiteSpace(status.Comment))
                    {
                        subject = $"{subject}: {Truncate(status.Comment, 120)}";
                    }

                    await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
                    {
                        InvoiceId = context.Invoice.Id,
                        ToAddress = method,
                        CcAddress = userName,
                        Subject = Truncate(subject, 200),
                        ProviderId = context.DossierNumber,
                        SentAt = sentAtUtc,
                        Status = status.Success ? "Sent" : "Error"
                    }, ct);
                }
            }
        }
        private static OctopusWorkflowProgress GetOctopusWorkflowProgress(string? workflowState)
        {
            var creationCompleted = IsOctopusStepCompleted(workflowState, OctopusWorkflowStateCreated);
            var uploadCompleted = IsOctopusStepCompleted(workflowState, OctopusWorkflowStateAttachmentUploaded);
            var sendCompleted = IsOctopusStepCompleted(workflowState, OctopusWorkflowStateSent);

            return new OctopusWorkflowProgress
            {
                CreationCompleted = creationCompleted,
                UploadCompleted = uploadCompleted,
                SendCompleted = sendCompleted
            };
        }

        private static void UpdateProgressFromState(OctopusWorkflowProgress progress, string? workflowState)
        {
            var updated = GetOctopusWorkflowProgress(workflowState);
            progress.CreationCompleted = updated.CreationCompleted;
            progress.UploadCompleted = updated.UploadCompleted;
            progress.SendCompleted = updated.SendCompleted;
        }

        private static bool IsOctopusStepCompleted(string? currentState, string targetState)
        {
            if (string.IsNullOrWhiteSpace(targetState))
                return false;

            if (!OctopusWorkflowOrder.TryGetValue(targetState, out var targetRank))
                return false;

            if (string.IsNullOrWhiteSpace(currentState))
                return false;

            if (!OctopusWorkflowOrder.TryGetValue(currentState, out var currentRank))
                return false;

            return currentRank >= targetRank;
        }

        // Helper voor stap 7/8: delivery state opslaan voor statusweergave.

        private async Task PersistOctopusWorkflowStateAsync(Invoices invoice, string targetState, CancellationToken ct)
        {
            invoice.OctopusWorkflowState = targetState;
            invoice.OctopusWorkflowUpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        private async Task TryUpdateOctopusDeliveryStateAsync(InvoiceDetailBO detail, CancellationToken ct)
        {
            if (detail == null)
                return;

            if (detail.OctopusDocumentSequenceNr is null or <= 0)
                return;

            if (detail.OctopusBookyearId is null or <= 0)
                return;

            if (string.IsNullOrWhiteSpace(detail.OctopusJournalKey))
                return;

            if (!RefreshableOctopusStates.Contains(detail.OctopusDeliveryState ?? string.Empty))
                return;

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null || string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
                return;

            try
            {
                var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(issuer.Id, issuer.OctopusDossierNumber, ct);
                var selection = new OctopusDocumentSelectionData
                {
                    BookyearKey = new OctopusBookyearKeyRef { Id = detail.OctopusBookyearId ?? 0 },
                    Journal = detail.OctopusJournalKey,
                    DocumentSequenceNr = detail.OctopusDocumentSequenceNr ?? 0,
                    ToDocumentSequenceNr = detail.OctopusDocumentSequenceNr ?? 0
                };

                var deliveryStates = await _octopusClient.GetInvoiceDeliveryStatesAsync(
                    dossierToken.Token,
                    issuer.OctopusDossierNumber,
                    selection,
                    ct);

                var state = deliveryStates?.FirstOrDefault();
                if (state != null)
                {
                    await PersistOctopusDeliveryStateAsync(detail.Id, state, ct);
                    detail.OctopusDeliveryState = state.DeliveryState;
                    detail.OctopusDeliveryComment = state.Comment;
                    detail.OctopusDeliveryDateTime = state.DeliveryDateTime;
                    detail.OctopusDeliveryUpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Octopus delivery state update failed for invoice {InvoiceId}", detail.Id);
            }
        }

        private async Task PersistOctopusDeliveryStateAsync(int invoiceId, OctopusDocumentDeliveryState state, CancellationToken ct)
        {
            var entity = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
            if (entity == null)
                return;

            entity.OctopusDeliveryState = state.DeliveryState;
            entity.OctopusDeliveryComment = state.Comment;
            entity.OctopusDeliveryDateTime = state.DeliveryDateTime;
            entity.OctopusDeliveryUpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        private async Task<OctopusRelation?> EnsureOctopusRelationAsync(
                  string dossierToken,
                  string dossierNumber,
                  IReadOnlyList<OctopusRelationLookup> lookups,
                  OctopusRelationRequest request,
                  CancellationToken ct)
        {
            OctopusRelation? found = null;
            foreach (var lookup in lookups)
            {
                var relation = await _octopusClient.FindRelationAsync(dossierToken, dossierNumber, lookup, ct);

                if (relation != null)
                {
                    found = relation;
                    break;
                }
            }

            if (found != null)
            {
                request.RelationIdentificationServiceData ??= new OctopusRelationIdentificationData();
                request.RelationIdentificationServiceData.RelationKey ??= found.RelationIdentificationServiceData?.RelationKey;

                if (NeedsRelationUpdate(found, request))
                {
                    return await _octopusClient.UpsertRelationAsync(dossierToken, dossierNumber, request, ct);
                }

                return found;
            }

            return await _octopusClient.UpsertRelationAsync(dossierToken, dossierNumber, request, ct);
        }

        private async Task<int?> GetIssuerSpecificOctopusRelationIdAsync(
        int issuerCompanyId,
        ClientAccount? clientAccount,
        ClientContacts? clientContact,
        CompanyInfo? company,
        CancellationToken ct)
        {
            var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                async Task<int?> QueryRelationIdAsync(string table, string entityColumn, int entityId)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText =
                        $"SELECT TOP 1 OctopusRelationId FROM {table} WHERE {entityColumn} = @entityId AND IssuerCompanyId = @issuerId";

                    var entityParameter = command.CreateParameter();
                    entityParameter.ParameterName = "@entityId";
                    entityParameter.Value = entityId;
                    entityParameter.DbType = DbType.Int32;
                    command.Parameters.Add(entityParameter);

                    var issuerParameter = command.CreateParameter();
                    issuerParameter.ParameterName = "@issuerId";
                    issuerParameter.Value = issuerCompanyId;
                    issuerParameter.DbType = DbType.Int32;
                    command.Parameters.Add(issuerParameter);

                    var result = await command.ExecuteScalarAsync(ct);
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }

                    return null;
                }

                var accountId = clientAccount?.Id ?? clientContact?.ClientAccountId;
                if (accountId is int clientAccountId)
                {
                    var relationId = await QueryRelationIdAsync("ClientAccountIssuerCompany", "ClientAccountId", clientAccountId);
                    if (relationId.HasValue)
                    {
                        return relationId;
                    }
                }

                if (clientContact?.Id is int clientContactId)
                {
                    var relationId = await QueryRelationIdAsync("ClientContactIssuerCompany", "ClientContactId", clientContactId);
                    if (relationId.HasValue)
                    {
                        return relationId;
                    }
                }

                if (company?.CompanyId is int companyId)
                {
                    var relationId = await QueryRelationIdAsync("CompanyIssuerCompany", "CompanyId", companyId);
                    if (relationId.HasValue)
                    {
                        return relationId;
                    }
                }

                return null;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task SetIssuerSpecificRelationIdAsync(
            int issuerCompanyId,
            ClientAccount? clientAccount,
            ClientContacts? clientContact,
            CompanyInfo? company,
            int relationId,
            CancellationToken ct)
        {
            var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                async Task UpsertRelationIdAsync(string table, string entityColumn, int entityId)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText =
                        $@"IF EXISTS (SELECT 1 FROM {table} WHERE {entityColumn} = @entityId AND IssuerCompanyId = @issuerId)
BEGIN
    UPDATE {table}
    SET OctopusRelationId = @relationId
    WHERE {entityColumn} = @entityId AND IssuerCompanyId = @issuerId;
END
ELSE
BEGIN
    INSERT INTO {table} ({entityColumn}, IssuerCompanyId, OctopusRelationId)
    VALUES (@entityId, @issuerId, @relationId);
END";

                    var entityParameter = command.CreateParameter();
                    entityParameter.ParameterName = "@entityId";
                    entityParameter.Value = entityId;
                    entityParameter.DbType = DbType.Int32;
                    command.Parameters.Add(entityParameter);

                    var issuerParameter = command.CreateParameter();
                    issuerParameter.ParameterName = "@issuerId";
                    issuerParameter.Value = issuerCompanyId;
                    issuerParameter.DbType = DbType.Int32;
                    command.Parameters.Add(issuerParameter);

                    var relationParameter = command.CreateParameter();
                    relationParameter.ParameterName = "@relationId";
                    relationParameter.Value = relationId;
                    relationParameter.DbType = DbType.Int32;
                    command.Parameters.Add(relationParameter);

                    await command.ExecuteNonQueryAsync(ct);
                }

                var accountId = clientAccount?.Id ?? clientContact?.ClientAccountId;
                if (accountId is int clientAccountId)
                {
                    await UpsertRelationIdAsync("ClientAccountIssuerCompany", "ClientAccountId", clientAccountId);
                }

                if (clientContact?.Id is int clientContactId)
                {
                    await UpsertRelationIdAsync("ClientContactIssuerCompany", "ClientContactId", clientContactId);
                }

                if (company?.CompanyId is int companyId)
                {
                    await UpsertRelationIdAsync("CompanyIssuerCompany", "CompanyId", companyId);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static IReadOnlyList<OctopusRelationLookup> BuildRelationLookups(
            Invoices invoice,
            ClientAccount? clientAccount,
            ClientContacts? clientContact = null,
            CompanyInfo? company = null,
            int? issuerRelationId = null)
        {
            var lookups = new List<OctopusRelationLookup>();

            if (issuerRelationId is int issuerSpecificRelationId and > 0)
            {
                lookups.Add(new OctopusRelationLookup { RelationId = issuerSpecificRelationId });
            }
            else
            {
                if (clientAccount?.OctopusRelationId is int relationId and > 0)
                {
                    lookups.Add(new OctopusRelationLookup { RelationId = relationId });
                }
                else if (clientContact?.OctopusRelationId is int contactRelationId and > 0)
                {
                    lookups.Add(new OctopusRelationLookup { RelationId = contactRelationId });
                }
                else if (company?.OctopusRelationId is int companyRelationId and > 0)
                {
                    lookups.Add(new OctopusRelationLookup { RelationId = companyRelationId });
                }
            }

            var vatNumber = FormatVatNumberForOctopus(
                invoice.VatNumber ?? clientAccount?.Vatnumber ?? clientContact?.Vatnumber ?? company?.VatNumber ?? company?.Ondernemingsnummer,
                clientAccount?.InvoicePostalCode?.Country?.LandIsocode
                    ?? clientAccount?.PostalCode?.Country?.LandIsocode
                    ?? clientContact?.InvoicePostalCode?.Country?.LandIsocode
                    ?? clientContact?.PostalCode?.Country?.LandIsocode
                     ?? company?.PostCode?.Country?.LandIsocode
                    ?? invoice.PostalCode?.Country?.LandIsocode
                    ?? company?.LandCode);
            if (!string.IsNullOrWhiteSpace(vatNumber))
            {
                lookups.Add(new OctopusRelationLookup { VatNumber = vatNumber });
            }

            var clientName = SelectClientName(invoice, clientAccount, clientContact, company);

            if (!string.IsNullOrWhiteSpace(clientName))
            {
                lookups.Add(new OctopusRelationLookup { Name = clientName });
            }

            return lookups;
        }


        private static bool NeedsRelationUpdate(OctopusRelation current, OctopusRelationRequest desired)
        {
            static bool Different(string? a, string? b)
                => !string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (desired == null)
                return false;

            if (Different(current.Name, desired.Name)) return true;
            if (Different(current.Firstname, desired.Firstname)) return true;
            if (Different(current.StreetAndNr, desired.StreetAndNr)) return true;
            if (Different(current.PostalCode, desired.PostalCode)) return true;
            if (Different(current.City, desired.City)) return true;
            if (Different(current.Country, desired.Country)) return true;
            if (!string.IsNullOrWhiteSpace(desired.VatNr) && Different(current.VatNr, desired.VatNr)) return true;
            if (desired.VatType.HasValue && current.VatType != desired.VatType) return true;
            if (Different(current.CurrencyCode, desired.CurrencyCode)) return true;
            if (current.Client != desired.Client || current.Supplier != desired.Supplier || current.Active != desired.Active) return true;

            return false;
        }

        private static OctopusRelationRequest BuildRelationRequest(
          Invoices invoice,
          ClientAccount? clientAccount,
          ClientContacts? clientContact,
          CompanyInfo? company,
          int? issuerRelationId)
        {
            var clientName = SelectClientName(invoice, clientAccount, clientContact, company);
            var countryCode = clientAccount?.InvoicePostalCode?.Country?.LandIsocode
                ?? clientAccount?.PostalCode?.Country?.LandIsocode
                ?? clientContact?.InvoicePostalCode?.Country?.LandIsocode
                ?? clientContact?.PostalCode?.Country?.LandIsocode
                ?? company?.PostCode?.Country?.LandIsocode
                ?? invoice.PostalCode?.Country?.LandIsocode
                ?? company?.LandCode;
            var isCompany = !string.IsNullOrWhiteSpace(clientAccount?.CompanyName)
                || (!string.IsNullOrWhiteSpace(clientAccount?.Vatnumber))
                || (!string.IsNullOrWhiteSpace(invoice.VatNumber) && invoice.ClientType != (int)InvoicePartyType.ClientContact)
                || (!string.IsNullOrWhiteSpace(company?.VatNumber) || !string.IsNullOrWhiteSpace(company?.Ondernemingsnummer));

            var firstName = clientContact?.Forename
                ?? clientAccount?.Name
                ?? clientName;

            var request = new OctopusRelationRequest
            {
                RelationIdentificationServiceData = new OctopusRelationIdentificationData
                {
                    RelationKey = issuerRelationId.HasValue && issuerRelationId.Value > 0
                        ? new OctopusRelationKey { Id = issuerRelationId.Value }
                        : clientAccount?.OctopusRelationId is int relationId && relationId > 0
                            ? new OctopusRelationKey { Id = relationId }
                            : clientContact?.OctopusRelationId is int contactRelationId && contactRelationId > 0
                                ? new OctopusRelationKey { Id = contactRelationId }
                                : company?.OctopusRelationId is int companyRelationId && companyRelationId > 0
                                    ? new OctopusRelationKey { Id = companyRelationId }
                                    : null,
                    ExternalRelationId = invoice.ClientId ?? invoice.CompanyId
                },
                Name = clientName,
                Firstname = firstName,
                Client = true,
                Supplier = company != null,
                Active = true,
                StreetAndNr = BuildStreetAndNumber(
                    clientAccount?.InvoiceStreet ?? clientAccount?.Street ?? clientContact?.InvoiceStreet ?? clientContact?.Street,
                    clientAccount?.InvoiceHousenumber ?? clientAccount?.Housenumber ?? clientContact?.InvoiceHousenumber ?? clientContact?.Housenumber,
                    clientAccount?.InvoiceBusnumber ?? clientAccount?.Busnumber ?? clientContact?.InvoiceBusnumber ?? clientContact?.Busnumber ?? company?.Busnummer,
                    invoice.Adress ?? company?.Straat),
                PostalCode = clientAccount?.InvoicePostalCode?.Postcode ?? clientAccount?.PostalCode?.Postcode ?? clientContact?.InvoicePostalCode?.Postcode ?? clientContact?.PostalCode?.Postcode ?? company?.Postcode ?? invoice.PostalCode?.Postcode,
                City = clientAccount?.InvoicePostalCode?.Gemeente ?? clientAccount?.PostalCode?.Gemeente ?? clientContact?.InvoicePostalCode?.Gemeente ?? clientContact?.PostalCode?.Gemeente ?? company?.Gemeente ?? invoice.PostalCode?.Gemeente,
                Country = countryCode,
                VatNr = FormatVatNumberForOctopus(invoice.VatNumber ?? clientAccount?.Vatnumber ?? clientContact?.Vatnumber ?? company?.VatNumber ?? company?.Ondernemingsnummer, countryCode),
                CurrencyCode = "EUR",
                DefaultBookingAccountClient = 0,
                DefaultBookingAccountSupplier = 0,
                SupplierPaymentMethod = 0,
                VatType = DetermineVatType(isCompany, countryCode)
            };

            request.Country ??= "BE";

            return request;
        }

        private static string? SelectClientName(Invoices invoice, ClientAccount? clientAccount, ClientContacts? clientContact = null, CompanyInfo? company = null)
        {
            if (!string.IsNullOrWhiteSpace(clientAccount?.CompanyName))
            {
                return clientAccount.CompanyName;
            }

            if (!string.IsNullOrWhiteSpace(clientContact?.CompanyName))
            {
                return clientContact.CompanyName;
            }

            if (!string.IsNullOrWhiteSpace(company?.BedrijfsNaam))
            {
                return company.BedrijfsNaam;
            }

            if (!string.IsNullOrWhiteSpace(clientAccount?.Name))
            {
                return clientAccount.Name;
            }

            if (!string.IsNullOrWhiteSpace(clientContact?.Name))
            {
                return clientContact.Name;
            }

            return invoice.ClientName;
        }
        private static string? BuildStreetAndNumber(string? street, string? houseNumber, string? busNumber, string? fallback)
        {
            var parts = new[] { street, houseNumber, string.IsNullOrWhiteSpace(busNumber) ? null : busNumber }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var result = string.Join(" ", parts);

            if (string.IsNullOrWhiteSpace(result))
                return fallback;

            return result;
        }

        private static string? NormalizeVatNumber(string? vatNumber)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return vatNumber;

            var trimmed = vatNumber.Trim();
            var cleaned = new string(trimmed.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? trimmed : cleaned;
        }

        private static string? FormatVatNumberForOctopus(string? vatNumber, string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return null;

            var cleanedDigits = new string(vatNumber.Where(char.IsDigit).ToArray());
            if (cleanedDigits.Length >= 10)
            {
                var digits = cleanedDigits[^10..];
                var prefix = string.IsNullOrWhiteSpace(countryCode) ? "BE" : countryCode.Trim().ToUpperInvariant();
                return $"{prefix}{digits[..4]}.{digits.Substring(4, 3)}.{digits.Substring(7, 3)}";
            }

            return NormalizeVatNumber(vatNumber);
        }

        private static int? DetermineVatType(bool isCompany, string? countryCode)
        {
            var isBelgian = string.Equals(countryCode, "BE", StringComparison.OrdinalIgnoreCase);

            if (isCompany)
                return isBelgian ? 1 : 4;

            return isBelgian ? 7 : 8;
        }

        private static string ResolveVatCodeKey(InvoicesDetails line, IReadOnlyCollection<Vattype> vatTypes, int? defaultVatTypeId)
        {
            const decimal tolerance = 0.001m;

            if (vatTypes == null || vatTypes.Count == 0)
            {
                throw new InvalidOperationException("Geen VAT-types geconfigureerd voor dit facturatiebedrijf.");
            }

            if (!string.IsNullOrWhiteSpace(line.VatCode))
            {
                return line.VatCode.Trim();
            }

            if (line.VatTypeId.HasValue)
            {
                var typeMatch = vatTypes.FirstOrDefault(v => v.Id == line.VatTypeId.Value);
                if (typeMatch != null && !string.IsNullOrWhiteSpace(typeMatch.Code))
                {
                    return typeMatch.Code;
                }
            }

            if (line.VatPercentage.HasValue)
            {
                var match = vatTypes.FirstOrDefault(v => Math.Abs(v.BasePercentage - line.VatPercentage.Value) < tolerance);

                if (match != null && !string.IsNullOrWhiteSpace(match.Code))
                {
                    return match.Code;
                }
            }

            if (defaultVatTypeId.HasValue)
            {
                var defaultMatch = vatTypes.FirstOrDefault(v => v.Id == defaultVatTypeId.Value);
                if (defaultMatch != null && !string.IsNullOrWhiteSpace(defaultMatch.Code))
                {
                    return defaultMatch.Code;
                }
            }

            var firstWithCode = vatTypes.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Code));
            if (firstWithCode != null)
            {
                return firstWithCode.Code;
            }

            throw new InvalidOperationException(
                line.VatPercentage.HasValue
                    ? $"Geen VAT-code gevonden voor percentage {line.VatPercentage:0.##} voor dit facturatiebedrijf."
                    : "Geen VAT-code gevonden voor dit facturatiebedrijf.");
        }

        private static readonly Regex InvoicePatternTokenRegex = new(@"\{(num|date):([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly IReadOnlyDictionary<string, Func<DateTime, CultureInfo, string>> InvoiceDateTokenResolvers =
            new Dictionary<string, Func<DateTime, CultureInfo, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["MM"] = static (d, c) => d.ToString("MM", c),
                ["MMM"] = static (d, c) => d.ToString("MMM", c),
                ["MMMM"] = static (d, c) => d.ToString("MMMM", c),
                ["yy"] = static (d, c) => d.ToString("yy", c),
                ["yyyy"] = static (d, c) => d.ToString("yyyy", c),
                ["MM-yyyy"] = static (d, c) => d.ToString("MM-yyyy", c),
            };

        private static string FormatInvoiceNumber(string? pattern, int sequenceNumber, DateOnly invoiceDate)
        {
            var resolvedPattern = string.IsNullOrWhiteSpace(pattern) ? "{num:0000}/{date:yyyy}" : pattern;
            return FormatPattern(resolvedPattern, sequenceNumber, invoiceDate.ToDateTime(TimeOnly.MinValue));
        }

        private static string FormatPattern(string? pattern, int num, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "{num:0000}/{date:yyyy}";

            var builder = new StringBuilder();
            var cursor = 0;
            var culture = CultureInfo.CurrentCulture;

            foreach (Match token in InvoicePatternTokenRegex.Matches(pattern))
            {
                builder.Append(pattern, cursor, token.Index - cursor);

                var type = token.Groups[1].Value;
                var format = token.Groups[2].Value?.Trim();

                if (type.Equals("num", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fmt = string.IsNullOrWhiteSpace(format) ? null : format;
                        builder.Append(fmt is null
                            ? num.ToString(culture)
                            : num.ToString(fmt, culture));
                    }
                    catch (FormatException)
                    {
                        builder.Append(num.ToString(culture));
                    }
                }
                else
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(format) && InvoiceDateTokenResolvers.TryGetValue(format, out var resolver))
                        {
                            builder.Append(resolver(date, culture));
                        }
                        else if (!string.IsNullOrWhiteSpace(format))
                        {
                            builder.Append(date.ToString(format, culture));
                        }
                        else
                        {
                            builder.Append(date.ToString(culture));
                        }
                    }
                    catch (FormatException)
                    {
                        builder.Append(date.ToString("yyyy", culture));
                    }
                }

                cursor = token.Index + token.Length;
            }

            if (cursor < pattern.Length)
                builder.Append(pattern, cursor, pattern.Length - cursor);

            return builder.ToString();
        }

        private static string? BuildStructuredOgm(Invoices invoice, int fiscalYear, int sequenceNumber)
        {
            if (!string.IsNullOrWhiteSpace(invoice.StructuredCommOgm))
            {
                return invoice.StructuredCommOgm.Trim();
            }

            return GenerateStructuredOgm(fiscalYear, sequenceNumber);
        }

        private static string? BuildEpcQrPayload(IssuerCompany issuer, string? bankAccount, string? structuredMessage, decimal amount)
        {
            if (issuer == null || !issuer.EpcQrEnabled)
                return null;
            if (string.IsNullOrWhiteSpace(issuer.EpcBeneficiaryName))
                return null;

            var iban = string.IsNullOrWhiteSpace(bankAccount) ? issuer.EpcIban : bankAccount;
            if (string.IsNullOrWhiteSpace(iban))
                return null;
            if (string.IsNullOrWhiteSpace(structuredMessage))
                return null;
            if (amount <= 0m)
                return null;

            var builder = new StringBuilder();
            builder.AppendLine("BCD");
            builder.AppendLine("001");
            builder.AppendLine("1");
            builder.AppendLine("SCT");
            builder.AppendLine((issuer.EpcBic ?? string.Empty).Trim().ToUpperInvariant());
            builder.AppendLine(TrimEpcValue(issuer.EpcBeneficiaryName, 70));
            builder.AppendLine(NormalizeIban(iban));
            builder.AppendLine($"EUR{amount.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine();
            builder.AppendLine(TrimEpcValue(structuredMessage, 140));
            return builder.ToString();
        }

        private static string NormalizeIban(string iban)
        {
            var sb = new StringBuilder();
            foreach (var ch in iban)
            {
                if (!char.IsWhiteSpace(ch))
                    sb.Append(char.ToUpperInvariant(ch));
            }

            return sb.ToString();
        }

        private static string? FormatBankAccount(string? account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return null;

            var normalized = new string(account.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

            if (normalized.StartsWith("BE", StringComparison.Ordinal) &&
                normalized.Length == 16 &&
                normalized.Skip(2).All(char.IsDigit))
            {
                var checkDigits = normalized.Substring(2, 2);
                var remainder = normalized.Substring(4);
                return $"BE{checkDigits} {remainder.Substring(0, 4)} {remainder.Substring(4, 4)} {remainder.Substring(8, 4)}";
            }

            return account.Trim();
        }

        private static string TrimEpcValue(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = value
                .Replace("\r", string.Empty)
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();

            return cleaned.Length <= maxLength ? cleaned : cleaned.Substring(0, maxLength);
        }

        private static string? GenerateStructuredOgm(int fiscalYear, int sequenceNumber)
        {
            if (sequenceNumber <= 0)
                return null;

            var yearDigits = Math.Abs(fiscalYear % 10000);
            var numberDigits = Math.Abs(sequenceNumber % 1_000_000);
            var baseDigits = $"{yearDigits:0000}{numberDigits:000000}";
            return FormatBelgianStructuredMessage(baseDigits);
        }
        private static string FormatBookyearLabel(DateTime startDate, DateTime endDate)
            => startDate.Year == endDate.Year
                ? $"{startDate:yyyy}"
                : $"{startDate:yyyy}-{endDate:yyyy}";

        private static string? FormatBelgianStructuredMessage(string baseDigits)
        {
            if (string.IsNullOrWhiteSpace(baseDigits) || baseDigits.Length != 10)
                return null;
            if (!baseDigits.All(char.IsDigit))
                return null;

            var number = long.Parse(baseDigits, CultureInfo.InvariantCulture);
            var checksum = (int)(number % 97);
            if (checksum == 0)
                checksum = 97;

            var combined = $"{baseDigits}{checksum:00}";
            return $"+++{combined[..3]}/{combined.Substring(3, 4)}/{combined[^5..]}+++";
        }


        private async Task SetIssuerViewBagsAsync(int issuerId, CancellationToken ct)
        {
            ViewBag.CompanyId = issuerId;
            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerId, ct);
        }

        private static string BuildInvoiceDisplayTitle(string? issuerName, string? publicId, int invoiceId)
        {
            var displayId = string.IsNullOrWhiteSpace(publicId)
                ? $"Factuur #{invoiceId}"
                : publicId.Trim();
            var issuerDisplay = string.IsNullOrWhiteSpace(issuerName) ? null : issuerName.Trim();

            return issuerDisplay is null
                ? displayId
                : $"{issuerDisplay} - {displayId}";
        }

        private static MvcBreadcrumbNode CreateHomeNode()
        {
            return new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        }

        private MvcBreadcrumbNode CreateIndexNode(int issuerId, string? companyName)
        {
            var home = CreateHomeNode();
            var title = string.IsNullOrWhiteSpace(companyName) ? "Facturen" : $"Facturen - {companyName.Trim()}";
            var index = new MvcBreadcrumbNode(nameof(Index), ControllerName, title)
            {
                Parent = home,
                RouteValues = issuerId > 0 ? new { issuerCompanyId = issuerId } : null
            };
            return index;
        }

        private void SetIndexBreadcrumb(int issuerId, string? companyName)
        {
            ViewData["BreadcrumbNode"] = CreateIndexNode(issuerId, companyName);
        }

        private void SetDetailBreadcrumb(int issuerId, string? companyName, int invoiceId, string detailTitle)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var title = string.IsNullOrWhiteSpace(detailTitle) ? "Factuur detail" : detailTitle;
            var detail = new MvcBreadcrumbNode(nameof(Detail), ControllerName, title)
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };
            ViewData["BreadcrumbNode"] = detail;
        }

        private void SetSendBreadcrumb(int issuerId, string? companyName, int invoiceId, string detailTitle)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var title = string.IsNullOrWhiteSpace(detailTitle) ? "Factuur detail" : detailTitle;
            var detail = new MvcBreadcrumbNode(nameof(Detail), ControllerName, title)
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };
            var send = new MvcBreadcrumbNode(nameof(Send), ControllerName, "Verzenden")
            {
                Parent = detail,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };
            ViewData["BreadcrumbNode"] = send;
        }
        private void SetEditBreadcrumb(int issuerId, string? companyName, int invoiceId, string detailTitle)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var title = string.IsNullOrWhiteSpace(detailTitle) ? "Factuur detail" : detailTitle;
            var detail = new MvcBreadcrumbNode(nameof(Detail), ControllerName, title)
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };

            var edit = new MvcBreadcrumbNode(nameof(Edit), ControllerName, "Bewerken")
            {
                Parent = detail,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };

            ViewData["BreadcrumbNode"] = edit;
        }
        private void SetCreateBreadcrumb(int issuerId, string? companyName)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var create = new MvcBreadcrumbNode(nameof(Create), ControllerName, "Nieuwe factuur")
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { issuerId } : null
            };

            ViewData["BreadcrumbNode"] = create;
        }

        private object BuildEmailTemplateModel(InvoiceDetailBO detail, IssuerCompanyBO issuer, string? bankAccount, string currency)
        {
            var formattedBankAccount = FormatBankAccount(bankAccount);
            return new
            {
                Invoice = new
                {
                    detail.Id,
                    detail.PublicId,
                    IssueDate = detail.InvoiceDate,
                    DueDate = detail.ExpirationDate,
                    TotalExcl = detail.TotalExclVat,
                    TotalVat = detail.TotalVat,
                    TotalIncl = detail.TotalInclVat,
                    Type = detail.IsCreditNote ? "Creditnota" : "Factuur",
                    IsCreditNote = detail.IsCreditNote
                },
                Client = new
                {
                    Name = detail.ClientName,
                    VatNumber = detail.ClientVatNumber,
                    Email = detail.ClientEmail
                },
                Issuer = new
                {
                    issuer.Name,
                    issuer.LegalName,
                    issuer.Email,
                    issuer.Phone
                },
                Payment = new
                {
                    BankAccount = formattedBankAccount,
                    Currency = currency,
                    StructuredMessage = detail.StructuredMessage
                }
            };
        }

        private static string BuildDefaultEmailBody(InvoiceDetailBO detail, IssuerCompanyBO issuer, string currency, string? bankAccount)
        {
            var culture = CultureInfo.GetCultureInfo("nl-BE");
            var amount = WebUtility.HtmlEncode(detail.TotalInclVat.ToString("C", culture));
            var invoiceId = WebUtility.HtmlEncode(detail.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture));
            var clientName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(detail.ClientName) ? "klant" : detail.ClientName);
            var builder = new StringBuilder();
            _ = currency;
            var formattedBankAccount = FormatBankAccount(bankAccount);

            builder.Append("<p>Beste ");
            builder.Append(clientName);
            builder.Append(",</p>");

            builder.Append("<p>In de bijlage vind je factuur ");
            builder.Append(invoiceId);
            builder.Append(" met een totaalbedrag van <strong>");
            builder.Append(amount);
            builder.Append("</strong>.");

            if (detail.ExpirationDate.HasValue)
            {
                var dueDate = detail.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", culture);
                builder.Append(" Gelieve dit bedrag te voldoen vóór ");
                builder.Append(WebUtility.HtmlEncode(dueDate));
                builder.Append('.');
            }

            builder.Append("</p>");

            if (!string.IsNullOrWhiteSpace(formattedBankAccount))
            {
                builder.Append("<p>Betaling kan via <strong>");
                builder.Append(WebUtility.HtmlEncode(formattedBankAccount));
                builder.Append("</strong>.</p>");
            }

            builder.Append("<p>Met vriendelijke groeten,<br/>");
            builder.Append(WebUtility.HtmlEncode(issuer.Name ?? string.Empty));
            builder.Append("</p>");

            return builder.ToString();
        }

        private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength);
        }

        private static (int? Number, int? Month, int? Year) ParseInvoicePublicId(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return (null, null, null);

            var parts = publicId
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            int? ParsePart(int index)
            {
                if (index >= parts.Length)
                    return null;

                return int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : (int?)null;
            }

            var number = ParsePart(0);
            var month = ParsePart(1);
            var year = ParsePart(2);

            return (number, month, year);
        }

        private static long BuildInvoiceSortValue(DateOnly invoiceDate, int? number, int? month, int? year, int fallbackId)
        {
            if (year.HasValue)
                return ComposeSortValue(year.Value, month, number);

            var fallbackSequence = (invoiceDate.DayNumber % 1_000_000) * 10 + Math.Abs(fallbackId % 10);
            return ComposeSortValue(invoiceDate.Year, invoiceDate.Month, fallbackSequence);
        }

        private static long ComposeSortValue(int year, int? month, int? number)
        {
            var monthValue = Clamp(month, 0, 999);
            var numberValue = Clamp(number, 0, 999_999);
            return (long)year * 1_000_000_000L + (long)monthValue * 1_000_000L + numberValue;
        }

        private static int Clamp(int? value, int min, int max)
        {
            if (!value.HasValue)
                return min;

            if (value.Value < min)
                return min;

            if (value.Value > max)
                return max;

            return value.Value;
        }

        private static string? NormalizeMultiline(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.Replace("\n", Environment.NewLine);
        }

        private List<OctopusCustomFieldValue> BuildInvoiceCustomFieldValues(Invoices invoice, IssuerCompany issuer, string? formattedPublicId)
        {
            var results = new List<OctopusCustomFieldValue>();
            var mappings = DeserializeInvoiceCustomFieldMappings(issuer.OctopusCustomFieldMappingsJson);

            foreach (var mapping in mappings)
            {
                var value = ResolveInvoiceFieldValue(invoice, mapping.InvoiceField, formattedPublicId);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    results.Add(new OctopusCustomFieldValue
                    {
                        CustomFieldKey = new OctopusCustomFieldKeyRef { Id = mapping.CustomFieldKeyId },
                        Value = value
                    });
                }
            }

            if (issuer.OctopusDownloadLinkCustomFieldKeyId.HasValue)
            {
                var link = BuildInvoiceDownloadLink(invoice, formattedPublicId);
                if (!string.IsNullOrWhiteSpace(link))
                {
                    var existing = results.FirstOrDefault(r => r.CustomFieldKey?.Id == issuer.OctopusDownloadLinkCustomFieldKeyId.Value);
                    if (existing != null)
                    {
                        existing.Value = link;
                    }
                    else
                    {
                        results.Add(new OctopusCustomFieldValue
                        {
                            CustomFieldKey = new OctopusCustomFieldKeyRef { Id = issuer.OctopusDownloadLinkCustomFieldKeyId.Value },
                            Value = link
                        });
                    }
                }
            }

            return results;
        }

        private static List<OctopusCustomFieldMappingBO> DeserializeInvoiceCustomFieldMappings(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<OctopusCustomFieldMappingBO>();
            }

            return JsonSerializer.Deserialize<List<OctopusCustomFieldMappingBO>>(json)
                   ?? new List<OctopusCustomFieldMappingBO>();
        }

        private static string? ResolveInvoiceFieldValue(Invoices invoice, string? invoiceField, string? formattedPublicId)
        {
            return invoiceField?.ToUpperInvariant() switch
            {
                "PUBLICID" => string.IsNullOrWhiteSpace(invoice.PublicId) ? formattedPublicId : invoice.PublicId,
                "STRUCTUREDCOMMOGM" => invoice.StructuredCommOgm,
                "CLIENTNAME" => invoice.ClientName
                    ?? invoice.ClientIdClientAccountNavigation?.Name
                    ?? invoice.ClientIdClientContactsNavigation?.Name,
                "VATNUMBER" => invoice.VatNumber,
                "VATREGIME" => invoice.VatRegime,
                "CURRENCYCODE" => invoice.CurrencyCode,
                "BANKACCOUNT" => invoice.BankAccount,
                "HEADERDESCRIPTION" => invoice.HeaderDescription,
                "TEXT" => invoice.Text,
                "EXTRAINFO" => invoice.ExtraInfo,
                "QREPCPAYLOAD" => invoice.QrEpcPayload,
                "INVOICEMODE" => invoice.InvoiceMode?.ToString(CultureInfo.InvariantCulture),
                "DATE" => invoice.Date.ToString("yyyy-MM-dd"),
                "EXPIRATIONDATE" => invoice.ExpirationDate?.ToString("yyyy-MM-dd"),
                _ => null
            };
        }

        private string BuildInvoiceDownloadLink(Invoices invoice, string? formattedPublicId)
        {
            var tokenPayload = $"{invoice.Id}|{invoice.PublicId ?? string.Empty}|{formattedPublicId ?? string.Empty}";
            var token = _downloadLinkProtector.Protect(tokenPayload);

            return Url.Action(nameof(OctopusDownload), ControllerName, new { id = invoice.Id, token }, Request.Scheme)
                ?? string.Empty;
        }
        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }
        private static InvoiceStatus TranslateStatus(string? status) => InvoiceStatusExtensions.FromCode(status);

        private static InvoiceStatus TranslateStatus(int? statusId, string? statusName)
            => statusId.HasValue ? InvoiceStatusExtensions.FromId(statusId) : InvoiceStatusExtensions.FromCode(statusName);

        private async Task<bool> IsInvoiceGeneratingAsync(int invoiceId, CancellationToken ct)
        {
            var statusId = await _db.Invoices
                .Where(i => i.Id == invoiceId)
                .Select(i => i.StatusId)
                .FirstOrDefaultAsync(ct);

            return InvoiceStatusExtensions.FromId(statusId) == InvoiceStatus.Generating;
        }
        private static bool WasSentViaPeppol(OctopusInvoiceSendResponse? sendResponse)
        {
            return sendResponse?.SendInvoiceStatusList?.Any(status =>
                !string.IsNullOrWhiteSpace(status.SendMethod)
                && status.SendMethod.Contains("PEPPOL", StringComparison.OrdinalIgnoreCase))
                ?? false;
        }

        private static string DetermineSendMethod(string? deliveryState, bool hasEmailLog, bool needsPrint)
        {
            if (!string.IsNullOrWhiteSpace(deliveryState))
            {
                var normalized = deliveryState.ToUpperInvariant();
                if (normalized.Contains("PEPPOL"))
                    return "Peppol";

                if (normalized.Contains("EMAIL"))
                    return "E-mail";
            }

            if (hasEmailLog)
                return "E-mail";

            if (needsPrint)
                return "Print";

            return "Niet verzonden";
        }
        private static bool DetermineCreditNote(bool isCreditSeries, string? status, decimal? totalInclVat)
        {
            if (isCreditSeries)
                return true;

            if (!string.IsNullOrWhiteSpace(status) && status.Contains("credit", StringComparison.OrdinalIgnoreCase))
                return true;

            if (totalInclVat.HasValue && totalInclVat.Value < 0m)
                return true;

            return false;
        }
        private static string BuildAddress(string? street, string? house) =>
    string.IsNullOrWhiteSpace(street) ? "" : (street + (string.IsNullOrWhiteSpace(house) ? "" : $" {house}")).Trim();

        // helper om standaardtekst te bouwen
        private static string BuildStageDescription(UnitStageRow r, decimal ownerPct = 100m)
        {
            var unitTypePart = string.IsNullOrWhiteSpace(r.UnitType) ? "" : (r.UnitType + " ");
            var unitAddr = BuildAddress(r.UnitStreet, r.UnitHouseNumber);
            var projAddr = BuildAddress(r.ProjectStreet, r.ProjectHouseNumber);
            var addr = !string.IsNullOrWhiteSpace(unitAddr) ? unitAddr : projAddr;

            var line1 = $"Voor de bouwwaarde van {unitTypePart}{r.UnitName} in project {r.ProjectName}, {addr} te {r.ProjectCity} ingevolge verkoopsovereenkomst.";
            var line2 = $"{ownerPct.ToString("0.##", CultureInfo.InvariantCulture)} % van de bouwwaarde van {unitTypePart}{r.UnitName} : {r.UnitConstructionTotal.ToString("N2")} €";

            // in één regel zodat je disabled input het netjes toont
            return $"{line1} {line2}";
        }

        public enum InvoiceSendFormMode
        {
            Standard,
            Copy
        }

    }
}
