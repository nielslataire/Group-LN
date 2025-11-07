using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Service;
using CPMCore.Services.Peppol;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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


namespace CPMCore.Controllers
{
    public class InvoicesController : Controller
    {
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
            IPeppolSender peppolSender)
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
                    Status = x.StatusName,
                    GrossTotal = x.GrossTotal,
                    Balance = x.Balance,
                    InvoiceNumber = parts.Number,
                    InvoiceMonth = parts.Month,
                    InvoiceYear = parts.Year,
                    InvoiceSortValue = sortValue
                };
            })
                .OrderByDescending(x => x.InvoiceSortValue)
                .ThenByDescending(x => x.Id)
                .ToList();

            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerCompanyId);
            ViewBag.CompanyId = issuerCompanyId;
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
            var issuerId = issuerCompanyId ?? detail.IssuerCompanyId;

            if (issuerId > 0)
            {
                ViewBag.CompanyId = issuerId;
                ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerId, ct);
            }

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
        public async Task<IActionResult> Send(int id, int? issuerCompanyId = null, CancellationToken ct = default)
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

            var vm = await CreateSendViewModelAsync(detail, issuer, includeDefaults: true, checkPeppol: true, ct);

            var issuerId = issuerCompanyId ?? issuer.Id;
            if (issuerId > 0)
            {
                await SetIssuerViewBagsAsync(issuerId, ct);
            }

            return View(vm);
        }

        //FACTUUR VERZENDEN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(InvoiceSendVM form, CancellationToken ct = default)
        {
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

            var vm = await CreateSendViewModelAsync(detail, issuer, includeDefaults: false, checkPeppol: true, ct);
            vm.To = form.To;
            vm.Cc = form.Cc;
            vm.Subject = form.Subject;
            vm.Body = form.Body;
            vm.AttachPdf = form.AttachPdf;
            vm.AttachUbl = form.AttachUbl;
            vm.SendToPeppol = form.SendToPeppol && vm.CanSendViaPeppol;

            await SetIssuerViewBagsAsync(vm.IssuerCompanyId, ct);

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
                    SentAt = DateTime.UtcNow,
                    Status = "Sent"
                }, ct);
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

            return RedirectToAction(nameof(Send), new { id = detail.Id, issuerCompanyId = vm.IssuerCompanyId });
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            try
            {
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
            var VatsBo = await _ics.ListVatTypeAsync(ct);

            // gekozen issuer (param of eerste actieve)
            var selectedIssuerId = issuerId
                ?? (await _ics.GetFirstActiveIssuerIdAsync(ct))
                ?? 0;

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
                    .Select(t => new VatTypeVM(t.Id, t.VATPercentage, t.VATText))
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

        // SAVE (POST) – create (vrije lijnen, schijven, wijzigingsopdrachten)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(InvoiceComposeVM vm, CancellationToken ct)
        {
            if (vm.IssuerCompanyId <= 0 || vm.PartyId is null || vm.PartyType is null)
            {
                AddMessage("error", "Vul issuer en afnemer in.", "Factuur");
                return await Create(vm.IssuerCompanyId, ct: ct);
            }

            // leveranciersguard: minstens project of contract
            if (vm.PartyType == InvoicePartyType.Supplier && vm.ProjectId is null && vm.SupplierContractId is null)
            {
                AddMessage("error", "Kies een project of een contract voor de leveranciersfactuur.", "Factuur");
                return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
            }

            // === Schijven (oude fallback wanneer er geen aangevinkte lijnen gepost werden) ===
            var usingStageLines = (vm.Mode == InvoiceMode.Stages && vm.Lines != null && vm.Lines.Any());
            if (vm.Mode == InvoiceMode.Stages && !usingStageLines)
            {
                if (vm.PartyType != InvoicePartyType.ClientAccount && vm.PartyType != InvoicePartyType.ClientContact)
                {
                    AddMessage("error", "Schijvenfacturatie is enkel voor klanten.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

                if (vm.StageIds == null || vm.StageIds.Count == 0)
                {
                    AddMessage("error", "Kies minstens één schijf.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

                var ok = await _ps.AreStagesValidForClientAsync(vm.PartyId.Value, vm.StageIds, ct);
                if (!ok)
                {
                    AddMessage("error", "Een of meer gekozen schijven horen niet bij deze klant of zijn niet factureerbaar.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }
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
                (bo.CompanyId, bo.ClientType, bo.ClientId) = (null, (int)vm.PartyType, vm.PartyId);

            // ===================== MODUS-AFHANKELIJKE LIJNEN =====================

            if (vm.Mode == InvoiceMode.Free)
            {
                // Vrije lijnen zoals vroeger
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
                // Nieuwe flow: enkel aangevinkte schijflijnen mee posten
                var selected = vm.Lines!.Where(x => x.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    AddMessage("error", "Kies minstens één schijf-lijn.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

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
                // ✅ Wijzigingsopdrachten – server-side herberekenen op basis van percentage (credit toegestaan)
                if (vm.PartyType != InvoicePartyType.ClientAccount && vm.PartyType != InvoicePartyType.ClientContact)
                {
                    AddMessage("error", "Wijzigingsopdrachten zijn enkel voor klanten.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

                var selected = (vm.Lines ?? Enumerable.Empty<InvoiceLineVM>())
                               .Where(l => l.IsSelected && l.ChangeOrderDetailId.HasValue)
                               .ToList();

                if (selected.Count == 0)
                {
                    AddMessage("error", "Kies minstens één wijzigingsopdracht.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

                var clientId = vm.PartyId!.Value;

                // Basisinfo ophalen (incl. basisbedrag dat positief/negatief kan zijn)
                var allRows = await _ps.GetApprovedChangeOrdersForClientAsync(clientId, vm.ProjectId, ct);
                var byDetail = allRows.ToDictionary(x => x.ChangeOrderDetailId, x => x);

                // === Servervalidator ===
                // a) Dubbels blokkeren
                var dup = selected.Select(s => s.ChangeOrderDetailId!.Value)
                                  .GroupBy(id => id)
                                  .FirstOrDefault(g => g.Count() > 1);
                if (dup != null)
                {
                    AddMessage("error", "Dezelfde wijzigingsopdracht werd meermaals geselecteerd.", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

                const decimal tol = 0.005m; // 0.5 cent tolerantie tegen afronding
                foreach (var l in selected)
                {
                    var detailId = l.ChangeOrderDetailId!.Value;

                    if (!byDetail.TryGetValue(detailId, out var src))
                    {
                        AddMessage("error", "Een wijzigingsopdracht is niet (meer) factureerbaar.", "Factuur");
                        return await Create(vm.IssuerCompanyId, ct: ct);
                    }

                    // clamp 0..100
                    var pct = l.StagePercentage;
                    if (pct < 0m || pct > 100m)
                    {
                        AddMessage("error", $"Percentage moet tussen 0 en 100 liggen (detail {detailId}).", "Factuur");
                        return await Create(vm.IssuerCompanyId, ct: ct);
                    }

                    var remaining = src.BaseAmountExcl; // kan negatief zijn (credit)
                    var expected = Math.Round(remaining * (pct / 100m), 2, MidpointRounding.AwayFromZero);

                    // tekenconsistentie: credit blijft credit (tenzij 0)
                    if (expected != 0m && Math.Sign(expected) != Math.Sign(remaining))
                    {
                        AddMessage("error", $"Teken van het bedrag komt niet overeen met het resterende saldo (detail {detailId}).", "Factuur");
                        return await Create(vm.IssuerCompanyId, ct: ct);
                    }

                    // niet méér dan resterend saldo in absolute waarde
                    if (Math.Abs(expected) - Math.Abs(remaining) > tol)
                    {
                        AddMessage("error", $"Gevraagde fractie overschrijdt het resterende saldo (detail {detailId}).", "Factuur");
                        return await Create(vm.IssuerCompanyId, ct: ct);
                    }
                }
                // === einde validator ===

                // Lijnen opbouwen (negatieve bedragen zijn toegestaan → credit)
                var boLines = new List<InvoiceLineBO>();
                foreach (var l in selected)
                {
                    var detailId = l.ChangeOrderDetailId!.Value;
                    if (!byDetail.TryGetValue(detailId, out var row)) continue;

                    var pct = Math.Clamp(l.StagePercentage, 0m, 100m);
                    var calc = Math.Round(row.BaseAmountExcl * (pct / 100m), 2, MidpointRounding.AwayFromZero);

                    if (calc == 0m) continue; // niks te boeken

                    boLines.Add(new InvoiceLineBO
                    {
                        Text = row.Title,
                        Price = calc,                                // kan negatief zijn (credit)
                        VatPercentage = row.VatPercentage,
                        LineType = "ChangeOrders",
                        GroupName = "Wijzigingsopdrachten",
                        ChangeOrderDetailId = detailId,
                        UnitId = row.UnitId
                    });
                }

                if (boLines.Count == 0)
                {
                    AddMessage("error", "Geen geldige bedragen om te boeken (controleer percentages).", "Factuur");
                    return await Create(vm.IssuerCompanyId, ct: ct);
                }

                bo.Lines = boLines;
            }

            else
            {
                // Oude schijven-flow (fallback): service bouwt lijnen adhv StageIds
                bo.StageIds = vm.StageIds?.ToList() ?? new List<int>();
                bo.Lines = new List<InvoiceLineBO>();
            }

            // =====================================================================

            var (id, publicId) = await _cmd.CreateWithLinesAsync(bo, issueNow: vm.StartAs == StartStatus.Invoice, ct);

            if (publicId != null)
                AddMessage("success", $"Factuur uitgegeven: {publicId}", "Factuur");
            else
                AddMessage("success", "Conceptfactuur opgeslagen.", "Factuur");

            return RedirectToAction(nameof(Create), new { issuerId = vm.IssuerCompanyId });
        }


        // EDIT (voor later verder uitwerken)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var inv = new InvoiceEditVM();
            if (inv == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Create));
            }
            return View(inv);
        }

        // ========== helper ==========
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
            return vm;
        }

        private static InvoiceDetailLineVM MapDetailLine(InvoiceLineBO line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            var discount = line.DiscountAmount
                ?? (line.DiscountPercent.HasValue
                    ? Math.Round(line.Price * (line.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                    : 0m);

            var net = line.Price - discount;
            var vat = Math.Round(net * (line.VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);
            var gross = net + vat;

            return new InvoiceDetailLineVM
            {
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

        private static string? CombineAddress(string? line1, string? line2)
        {
            if (string.IsNullOrWhiteSpace(line1))
                return line2;
            if (string.IsNullOrWhiteSpace(line2))
                return line1;
            return $"{line1}, {line2}";
        }

        private async Task<InvoiceSendVM> CreateSendViewModelAsync(InvoiceDetailBO detail, IssuerCompanyBO issuer, bool includeDefaults, bool checkPeppol, CancellationToken ct)
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

            if (includeDefaults)
            {
                vm.To = detail.ClientEmail ?? string.Empty;
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
                    if (includeDefaults)
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

        private async Task SetIssuerViewBagsAsync(int issuerId, CancellationToken ct)
        {
            ViewBag.CompanyId = issuerId;
            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerId, ct);
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
        private static string TranslateStatus(string? status)
        {
            return (status ?? string.Empty).Trim() switch
            {
                "Draft" => "Concept",
                "Issued" => "Genummerd",
                "Sent" => "Verzonden",
                "PartiallyPaid" => "Deels betaald",
                "Paid" => "Betaald",
                "Overdue" => "Vervallen",
                "Cancelled" => "Geannuleerd",
                _ => string.IsNullOrWhiteSpace(status) ? "Onbekend" : status
            };
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

  

    }
}
