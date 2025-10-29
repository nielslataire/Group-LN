using BOCore;
using CPMCore.Models.Invoicing;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceCore;
using System;
using System.Globalization;
using System.Linq;
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

        public InvoicesController(
            IInvoiceQueryService invoices,
            ICompanyQueryService companies,
            ILogger<InvoicesController> logger,
            IPartyLookupService lookup,
            IInvoiceCommandService cmd,
            IProjectSupplierLookupService ps,
            IIssuerCompanyService ics)
        {
            _invoices = invoices;
            _companies = companies;
            _logger = logger;
            _lookup = lookup;
            _cmd = cmd;
            _ps = ps;
            _ics = ics;
        }

        // LIST
        public async Task<IActionResult> Index(int issuerCompanyId)
        {
            var bos = await _invoices.GetByCompanyAsync(issuerCompanyId);
            var vms = bos.Select(x => new InvoiceListItemVM
            {
                Id = x.Id,
                PublicId = x.PublicId,
                ClientName = x.ClientName,
                InvoiceDate = x.InvoiceDate,
                Status = x.StatusName,
                GrossTotal = x.GrossTotal,
                Balance = x.Balance
            }).ToList();

            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerCompanyId);
            ViewBag.CompanyId = issuerCompanyId;
            return View(vms);
        }

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
                    .ToList()
            };

            return View(vm);
        }

        // PARTY LOOKUP (AJAX Select2)
        [HttpGet]
        public async Task<IActionResult> PartyLookup(string? term, int take = 20, CancellationToken ct = default)
        {
            var rows = await _lookup.SearchPartiesAsync(term ?? "", take, ct);

            var results = rows.Select(x => new
            {
                id = x.Type switch
                {
                    InvoicePartyType.ClientAccount => $"ca:{x.Id}",
                    InvoicePartyType.ClientContact => $"cc:{x.Id}",
                    InvoicePartyType.Supplier => $"su:{x.Id}",
                    _ => $"x:{x.Id}"
                },
                text = x.Name,
                type = x.Type.ToString()
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
                ProjectId = vm.ProjectId,
                SupplierContractId = vm.SupplierContractId,
                PaymentGroupId = vm.PaymentGroupId
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
