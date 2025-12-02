using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Service;
using CPMCore.Services.Peppol;
using CPMCore.Services.Octopus;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using ServiceCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers
{
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
        }

        // LIST
        public async Task<IActionResult> Index(int issuerCompanyId)
        {
            var bos = await _invoices.GetByCompanyAsync(issuerCompanyId);
            var vms = bos
            .Select(x =>
            {
                var parts = ParseInvoicePublicId(x.PublicId);
                var sortValue = BuildInvoiceSortValue(x.InvoiceDate, parts.Number, parts.Month, parts.Year, x.Id);

                return new InvoiceListItemVM
                {
                    Id = x.Id,
                    PublicId = x.PublicId,
                    ClientName = x.ClientName,
                    InvoiceDate = x.InvoiceDate,
                    Status = TranslateStatus(x.StatusName),
                    GrossTotal = x.GrossTotal,
                    Balance = x.Balance,
                    InvoiceNumber = parts.Number,
                    InvoiceMonth = parts.Month,
                    InvoiceYear = parts.Year,
                    IsCreditNote = DetermineCreditNote(x.IsCreditNote, x.StatusName, x.GrossTotal),
                    InvoiceSortValue = sortValue
                };
            })
                .OrderByDescending(x => x.InvoiceSortValue)
                .ThenByDescending(x => x.Id)
                .ToList();

            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerCompanyId);
            ViewBag.CompanyId = issuerCompanyId;
            SetIndexBreadcrumb(issuerCompanyId, ViewBag.CompanyName as string);
            return View(vms);
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

            var dto = detail.ToInvoiceDto();
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
            var dto = detail.ToInvoiceDto();
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
                await _emailSender.SendEmailAsync(vm.To, vm.Subject, vm.Body, attachments, vm.Cc);
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
                Status = TranslateStatus(row.StatusName)
            };

            return PartialView("Modals/_ModalDeleteInvoice", vm);
        }

        //VAN DRAFT NAAR DEFINITIEF
        [HttpGet]
        public async Task<IActionResult> Issue(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            try
            {
                await SendInvoiceToOctopusAsync(id, ct);
                var publicId = await _cmd.IssueDraftAsync(id, issueDate: null, ct: ct);
                if (!string.IsNullOrWhiteSpace(publicId))
                    AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
                else
                    AddMessage("success", "Factuur definitief gemaakt.", "Factuur");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Issue invoice {InvoiceId} blocked", id);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue invoice {InvoiceId} failed", id);
                AddMessage("error", "Factuur kon niet definitief gemaakt worden.", "Factuur");
            }

            return RedirectToAction(nameof(Index), new { issuerCompanyId });
        }

        // CREATE (GET)
        [HttpGet]
        public async Task<IActionResult> Create(int? issuerId = null, CancellationToken ct = default)
        {
            // haal alles via service
            var issuersBo = await _ics.ListActiveIssuersAsync(ct);
            var termsBo = await _ics.ListPaymentTermsAsync(ct);


            // gekozen issuer (param of eerste actieve)
            var selectedIssuerId = issuerId
                ?? (await _ics.GetFirstActiveIssuerIdAsync(ct))
                ?? 0;

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


            var vm = new InvoiceComposeVM
            {
                IssuerCompanyId = selectedIssuerId,
                PaymentTermId = selectedTermId,
                VatTypeId = selectedVatId,

                Issuers = issuersBo
                    .Select(i => new IssuerItemVM(i.Id, i.Name, i.DefaultPaymentTermId, i.DefaultVatTypeId))
                    .ToList(),

                PaymentTerms = termsBo
                    .Select(t => new PaymentTermItemVM(t.Id, t.Name, t.Days))
                    .ToList(),

                VatTypes = VatsBo
                     .Select(t => new VatTypeVM(t.Id, t.BasePercentage, t.Code, t.Description, t.Type, t.DefaultSellBookingAccountNr))
                    .ToList(),

                IssuerBankAccountId = defaultAccountId,
                IssuerBankAccounts = accountsBo
                    .Select(a => new SelectListItem
                    {
                        Value = a.Id.ToString(),
                        Text = string.IsNullOrWhiteSpace(a.DisplayName)
                            ? a.Iban
                            : $"{a.DisplayName} ({a.Iban})",
                        Selected = defaultAccountId.HasValue && a.Id == defaultAccountId.Value
                    })
                    .ToList()
            };

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
                InvoiceDate = vm.InvoiceDate
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
            return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
        }

        private async Task<InvoiceDraftBO> BuildInvoiceDraftBoAsync(InvoiceComposeVM vm, CancellationToken ct)
        {
            if (vm == null)
                throw new ArgumentNullException(nameof(vm));

            if (vm.IssuerCompanyId <= 0 || vm.PartyId is null || vm.PartyType is null)
                throw new InvalidOperationException("Vul issuer en afnemer in.");

            if (vm.PartyType == InvoicePartyType.Supplier && vm.ProjectId is null && vm.SupplierContractId is null)
                throw new InvalidOperationException("Kies een project of een contract voor de leveranciersfactuur.");

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
                FooterDescription = vm.FooterDescription
            };

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
                var (id, publicId) = await _cmd.CreateWithLinesAsync(bo, issueNow: vm.StartAs == StartStatus.Invoice, ct);

                if (publicId != null)
                    AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
                else
                    AddMessage("success", "Conceptfactuur opgeslagen.", "Factuur");

                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
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
            var termsBo = await _ics.ListPaymentTermsAsync(ct);


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
                VatTypes = vatBo.Select(t => new VatTypeVM(t.Id, t.BasePercentage, t.Code, t.Description, t.Type, t.DefaultSellBookingAccountNr)).ToList(),
                Issuers = issuersBo.Select(i => new IssuerItemVM(i.Id, i.Name, i.DefaultPaymentTermId, i.DefaultVatTypeId)).ToList(),
                PaymentTerms = termsBo.Select(t => new PaymentTermItemVM(t.Id, t.Name, t.Days)).ToList()
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
                BankAccount = bo.BankAccount,
                HeaderText = NormalizeMultiline(bo.HeaderText),
                DetailText = NormalizeMultiline(bo.DetailText),
                ExtraInfo = NormalizeMultiline(bo.ExtraInfo),
                TotalExclVat = RoundCurrency(bo.TotalExclVat),
                TotalVat = RoundCurrency(bo.TotalVat),
                TotalInclVat = RoundCurrency(bo.TotalInclVat),
                PaidAmount = bo.PaidAmount,
                Balance = bo.Balance
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

            var detailTitle = BuildInvoiceDisplayTitle(companyDisplay, detail.PublicId, detail.Id);
            SetEditBreadcrumb(issuerId, companyDisplay, detail.Id, detailTitle);
            ViewData["Title"] = detailTitle;
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

        private async Task SendInvoiceToOctopusAsync(int invoiceId, CancellationToken ct)
        {
            var invoice = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.InvoicesDetails)
                .Include(i => i.IssuerCompany)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
                ?? throw new InvalidOperationException("Factuur niet gevonden.");

            var issuer = invoice.IssuerCompany
                ?? throw new InvalidOperationException("Factuur heeft geen gekoppeld facturatiebedrijf.");

            if (string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
                throw new InvalidOperationException("Octopus dossiernummer ontbreekt. Vul dit in bij het facturatiebedrijf.");

            var finalDate = invoice.Date == default
                ? DateOnly.FromDateTime(DateTime.Today)
                : invoice.Date;

            var seriesId = await _db.InvoiceSeries
                .Where(s => s.IssuerCompanyId == issuer.Id && s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Geen actieve nummerreeks voor dit facturatiebedrijf.");

            var fiscalYear = finalDate.Year;
            var sequence = await _db.InvoiceSequence
                .Include(s => s.Bookyear)!
                .ThenInclude(b => b.OctopusBookyearPeriods)
                .Include(s => s.Journal)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SeriesId == seriesId && s.FiscalYear == fiscalYear, ct);

            if (sequence?.Bookyear == null || sequence.Journal == null)
                throw new InvalidOperationException("Koppel een Octopus-boekjaar en dagboek aan de nummerreeks voor dit jaar.");

            var nextNumber = (sequence.CurrentNumber == 0 ? 0 : sequence.CurrentNumber) + 1;

            var periodNumber = sequence.Bookyear.OctopusBookyearPeriods
                .FirstOrDefault(p =>
                {
                    var start = DateOnly.FromDateTime(p.StartDate);
                    var end = DateOnly.FromDateTime(p.EndDate);
                    return finalDate >= start && finalDate <= end;
                })?.BookyearPeriodNr
                ?? 0;

            var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(issuer.Id, issuer.OctopusDossierNumber, ct);

            var payload = new OctopusInvoiceCreateRequest
            {
                BookyearKey = new OctopusBookyearKeyRef { Id = sequence.Bookyear.BookyearKeyId },
                JournalKey = sequence.Journal.JournalKey,
                DocumentSequenceNr = nextNumber,
                BookyearPeriodeNr = periodNumber,
                DocumentDate = finalDate,
                ExpiryDate = invoice.ExpirationDate ?? finalDate,
                CurrencyCode = string.IsNullOrWhiteSpace(invoice.CurrencyCode) ? "EUR" : invoice.CurrencyCode!,
                ExchangeRate = invoice.FxRateToCompany ?? 0m,
                RelationIdentificationServiceData = new OctopusRelationIdentificationServiceData
                {
                    RelationKey = new OctopusRelationKeyRef { Id = invoice.ClientId ?? 0 },
                    ExternalRelationId = invoice.ClientId ?? invoice.CompanyId ?? 0
                },
                Comment = invoice.DetailText,
                OrderReference = invoice.ProjectId?.ToString(CultureInfo.InvariantCulture),
                Reference = invoice.PublicId,
                FinancialDiscount = 0m,
                CustomFieldValueList = new List<OctopusCustomFieldValue>(),
                InvoiceLines = invoice.InvoicesDetails.Select(line => new OctopusInvoiceLineRequest
                {
                    ExternProductNr = line.Id.ToString(CultureInfo.InvariantCulture),
                    Description = line.Text,
                    Count = 1,
                    Unit = string.Empty,
                    UnitPrice = line.Price ?? 0m,
                    DiscountPercentage = line.DiscountPercent ?? 0m,
                    VatCodeKey = (line.VatPercentage ?? 0m).ToString(CultureInfo.InvariantCulture),
                    BookingAccountNr = 0,
                    CostCentreKey = new OctopusCostCentreKeyRef { Id = 0 },
                    CustomFieldValueList = new List<OctopusCustomFieldValue>(),
                    IntrastatServiceData = null
                }).ToList()
            };

            await _octopusClient.CreateInvoiceAsync(dossierToken.Token, issuer.OctopusDossierNumber, payload, ct);
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

        private object BuildEmailTemplateModel(InvoiceDetailBO detail, IssuerCompanyBO issuer, string? bankAccount, string currency)
        {
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
                    TotalIncl = detail.TotalInclVat
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
                    BankAccount = bankAccount,
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

            if (!string.IsNullOrWhiteSpace(bankAccount))
            {
                builder.Append("<p>Betaling kan via <strong>");
                builder.Append(WebUtility.HtmlEncode(bankAccount));
                builder.Append("</strong>.</p>");
            }

            builder.Append("<p>Met vriendelijke groeten,<br/>");
            builder.Append(WebUtility.HtmlEncode(issuer.Name ?? string.Empty));
            builder.Append("</p>");

            return builder.ToString();
        }

        private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

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
        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }
        private static InvoiceStatus TranslateStatus(string? status) => InvoiceStatusExtensions.FromCode(status);
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
