using BOCore;
using CPMCore.Models.Invoicing;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

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

        public InvoicesController(
            IInvoiceQueryService invoices,
            ICompanyQueryService companies,
            ILogger<InvoicesController> logger,
            IPartyLookupService lookup,
            IInvoiceCommandService cmd,
            IProjectSupplierLookupService ps)
        {
            _invoices = invoices;
            _companies = companies;
            _logger = logger;
            _lookup = lookup;
            _cmd = cmd;
            _ps = ps;
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

        // CREATE (GET)
        [HttpGet]
        public async Task<IActionResult> Create(int? issuerId = null, CancellationToken ct = default)
        {
            var vm = new InvoiceComposeVM
            {
                IssuerCompanyId = issuerId ?? await _lookup.GetFirstActiveIssuerIdAsync(ct),
                Issuers = (await _lookup.ListActiveIssuersAsync(ct))
                            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
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

                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId, ct);
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

        // LIJNEN VOOR SCHIJVEN AANMAKEN 
        [HttpGet]
        public async Task<IActionResult> ComposeStageLines(int clientId, int? projectId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0)
                    return PartialView("_StageLinesTable", new List<InvoiceLineVM>());

                var rows = await _ps.GetUnitsWithInvocableStagesForClientAsync(clientId, ct);
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

        [HttpGet]
        public async Task<IActionResult> ComposeChangeOrderLines(int clientId, int? projectId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0) return PartialView("_ChangeOrderLinesTable", new List<InvoiceLineVM>());

                // TODO: pas aan naar jouw service/query
                var rows = await _ps.GetApprovedChangeOrdersForClientAsync(clientId, projectId, ct);
                // Verwacht: UnitId, UnitName, UnitType, ProjectName, ProjectStreet, ProjectHouseNumber, ProjectCity,
                //           ChangeOrderId, Title, AmountExcl, VatPercentage

                var lines = rows
                    .OrderBy(r => r.UnitName).ThenBy(r => r.Title)
                    .Select((r, idx) => new InvoiceLineVM
                    {
                        IsSelected = false,
                        Text = r.Title,                          // Omschrijving per lijn = titel CO
                        Price = r.AmountExcl,                    // basisbedrag (100%)
                        VatPercentage = r.VatPercentage,
                        UnitId = r.UnitId,
                        PaymentStageId = null,
                        LineType = "ChangeOrders",
                        GroupName = "Wijzigingsopdrachten",
                        UtilityCost = false,

                        // metadata voor header
                        UnitName = r.UnitName,
                        UnitType = r.UnitType,
                        ProjectName = r.ProjectName,
                        ProjectStreet = r.ProjectStreet,
                        ProjectHouseNumber = r.ProjectHouseNumber,
                        ProjectCity = r.ProjectCity,
                        UnitConstructionTotal = 0m,              // niet nodig hier
                        OwnerPercentage = 100m
                    })
                    .ToList();

                return PartialView("_ChangeOrderLinesTable", lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ComposeChangeOrderLines failed");
                return PartialView("_ChangeOrderLinesTable", new List<InvoiceLineVM>());
            }
        }


        // LIJNEN VOOR NUTSAANSLUITINGEN AANMAKEN 

        [HttpGet]
        public async Task<IActionResult> ComposeUtilityLines(int clientId, int? projectId, CancellationToken ct = default)
        {
            try
            {
                if (clientId <= 0) return PartialView("_UtilityLinesTable", new List<InvoiceLineVM>());

                // TODO: pas aan naar jouw service/query
                var rows = await _ps.GetUtilityCostsForClientAsync(clientId, projectId, ct);
                // Verwacht: UnitId?, UnitName?, ProjectName, ProjectStreet, ProjectHouseNumber, ProjectCity,
                //           UtilityId, Title, AmountExcl, VatPercentage

                var lines = rows
                    .OrderBy(r => r.ProjectName).ThenBy(r => r.UnitName).ThenBy(r => r.Title)
                    .Select(r => new InvoiceLineVM
                    {
                        IsSelected = false,
                        Text = r.Title,                   // per lijn tonen we de bron (periode/titel)
                        Price = r.AmountExcl,            // basis voor afrekening-som
                        VatPercentage = r.VatPercentage, // default vat; bij afrekening kan je 21% afdwingen
                        UnitId = r.UnitId,
                        LineType = "Utilities",
                        GroupName = "Nutsvoorzieningen",
                        UtilityCost = true,

                        // metadata voor header
                        UnitName = r.UnitName,
                        ProjectName = r.ProjectName,
                        ProjectStreet = r.ProjectStreet,
                        ProjectHouseNumber = r.ProjectHouseNumber,
                        ProjectCity = r.ProjectCity
                    })
                    .ToList();

                return PartialView("_UtilityLinesTable", lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ComposeUtilityLines failed");
                return PartialView("_UtilityLinesTable", new List<InvoiceLineVM>());
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

        // SAVE (POST) – create (vrije lijnen of schijven)
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

            // === Schijvenmodus: lijnen met checkbox ===
            var usingStageLines = (vm.Mode == InvoiceMode.Stages && vm.Lines != null && vm.Lines.Any());

            if (vm.Mode == InvoiceMode.Stages && !usingStageLines)
            {
                // Backward-compatible guard (oude flow met StageIds)
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
                // ✅ NIEUWE FLOW: neem ENKEL aangevinkte schijflijnen mee
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

                // In de nieuwe flow gebruiken we StageIds niet meer
                bo.StageIds = new List<int>();
            }
            else
            {
                // Oude flow (fallback): service bouwt lijnen adhv StageIds
                bo.StageIds = vm.StageIds?.ToList() ?? new List<int>();
                bo.Lines = new List<InvoiceLineBO>();
            }

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
