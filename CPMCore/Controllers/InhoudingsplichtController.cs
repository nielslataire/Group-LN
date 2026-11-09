using BOCore;
using CPMCore.Models.Inhoudingsplicht;
using CPMCore.Services.Security;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.IncomingInvoices;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>
    /// Controlelijsten inhoudingsplicht (30bis/30ter): aannemers die vóór een betaling bij de RSZ gecontroleerd worden (design-handoff 57).
    /// Elke lijst blijft bewaard met de consultatiereferenties, de trace-id's en de ruwe RSZ-resultaten als bewijs.
    /// </summary>
    [Authorize]
    [CPMCore.Filters.PermissionRead(PermissionCodes.Invoicing)]
    public class InhoudingsplichtController : BaseController
    {
        private readonly IRetainmentCheckService _service;
        private readonly IPermissionService _permissionService;

        public InhoudingsplichtController(IRetainmentCheckService service, IPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        [Breadcrumb("Inhoudingsplicht")]
        public async Task<IActionResult> Index([FromServices] cpmRunningContext db, Guid? herhaal = null, Guid? open = null, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            SetPageHeader("bx bx-shield-quarter", "Controle inhoudingsplicht");
            var vm = new InhoudingsplichtIndexVm
            {
                Configured = _service.IsConfigured,
                CanWrite = _permissionService.HasWrite(PermissionCodes.Invoicing),
                Simulation = _service.IsSimulation,
                Lists = await _service.ListBatchesAsync(50, ct),
                OpenBatchId = open
            };
            ViewBag.Configured = vm.Configured; ViewBag.CanWrite = vm.CanWrite; ViewBag.Simulation = vm.Simulation;
            if (ViewData["UseGlV2Layout"] as bool? != true)
                return View("Index", vm.Lists);

            ViewData["HelpScreenId"] = "inhoudingsplicht";
            await FillSuppliersAsync(vm, db, ct);
            if (herhaal.HasValue) await ApplyRepeatAsync(vm, herhaal.Value, ct);
            return View("IndexV2", vm);
        }

        /// <summary>Het zijpaneel van een controlelijst: samenvatting, per onderneming de uitslag en het bewijs (referentie, trace-id, tijdstip).</summary>
        public async Task<IActionResult> Paneel(Guid id, CancellationToken ct = default)
        {
            var batch = await _service.GetBatchAsync(id, ct);
            if (batch == null) return NotFound();
            await _permissionService.EnsureLoadedAsync();
            ViewBag.CanWrite = _permissionService.HasWrite(PermissionCodes.Invoicing);
            return PartialView("_ListPanelV2", batch);
        }

        /// <summary>Naam geven aan een controlelijst (vanuit het zijpaneel). Enkel de naam verandert; het bewijs blijft zoals het was.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hernoem(Guid id, string name, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.Invoicing))
                return Forbid();
            var ok = await _service.RenameBatchAsync(id, name, ct);
            return ok ? Json(new { success = true, name = string.IsNullOrWhiteSpace(name) ? null : name.Trim() }) : NotFound();
        }

        /// <summary>Leveranciers met hun werven (contracten en facturen), laatste factuur en laatste controle, voor de selectielijst.</summary>
        private async Task FillSuppliersAsync(InhoudingsplichtIndexVm vm, cpmRunningContext db, CancellationToken ct)
        {
            var projects = await db.Project.AsNoTracking()
                .Select(p => new { p.ProjectId, p.ProjectName, Active = p.StatusId == (int)ProjectStatusType.Uitvoering })
                .ToDictionaryAsync(p => p.ProjectId, ct);
            var contracts = await db.Contract.AsNoTracking().Select(c => new { c.Id, c.CompanyId, c.ProjectId }).ToListAsync(ct);
            var contractById = contracts.ToDictionary(c => c.Id);
            var invoices = await db.IncommingInvoices.AsNoTracking()
                .Where(i => i.CompanyId != null || i.ContractId != null)
                .Select(i => new { i.CompanyId, i.ContractId, i.ProjectId, i.Date })
                .ToListAsync(ct);

            var sites = new Dictionary<int, HashSet<int>>();
            var lastInvoice = new Dictionary<int, DateTime>();
            void Link(int companyId, int? projectId)
            {
                if (projectId is null || !projects.ContainsKey(projectId.Value)) return;
                if (!sites.TryGetValue(companyId, out var set)) sites[companyId] = set = new HashSet<int>();
                set.Add(projectId.Value);
            }
            foreach (var c in contracts) Link(c.CompanyId, c.ProjectId);
            foreach (var i in invoices)
            {
                var contract = i.ContractId.HasValue && contractById.TryGetValue(i.ContractId.Value, out var k) ? k : null;
                var companyId = i.CompanyId ?? contract?.CompanyId;
                if (companyId is null) continue;
                Link(companyId.Value, i.ProjectId ?? contract?.ProjectId);
                var date = i.Date.ToDateTime(TimeOnly.MinValue);
                if (!lastInvoice.TryGetValue(companyId.Value, out var d) || date > d) lastInvoice[companyId.Value] = date;
            }

            var companies = await db.CompanyInfo.AsNoTracking()
                .Where(c => !c.IsCustomer && c.IsActive)
                .Select(c => new { c.CompanyId, c.BedrijfsNaam, c.VatNumber, c.Ondernemingsnummer, Activities = c.Activity.Select(a => a.Omschrijving).Take(2).ToList() })
                .ToListAsync(ct);

            var rows = companies.Select(c =>
            {
                var identifier = RetainmentCheckService.NormalizeIdentifier(string.IsNullOrWhiteSpace(c.VatNumber) ? c.Ondernemingsnummer : c.VatNumber);
                return new InhoudingsplichtIndexVm.SupplierRow
                {
                    CompanyId = c.CompanyId,
                    Name = c.BedrijfsNaam,
                    Activity = string.Join(", ", c.Activities),
                    Identifier = identifier,
                    IdentifierDisplay = FormatIdentifier(identifier),
                    LastInvoice = lastInvoice.TryGetValue(c.CompanyId, out var li) ? li : null,
                    Sites = sites.TryGetValue(c.CompanyId, out var set)
                        ? set.Select(id => new InhoudingsplichtIndexVm.SiteRef { ProjectId = id, Name = projects[id].ProjectName, Active = projects[id].Active })
                             .OrderByDescending(s => s.Active).ThenBy(s => s.Name).ToList()
                        : new List<InhoudingsplichtIndexVm.SiteRef>()
                };
            }).OrderBy(r => r.Name).ToList();

            var last = await _service.GetLastChecksAsync(rows.Where(r => r.Identifier != null).Select(r => r.Identifier).ToList(), ct);
            foreach (var r in rows)
                if (r.Identifier != null && last.TryGetValue(r.Identifier, out var l)) r.Last = l;

            vm.Suppliers = rows;
            vm.ActiveSites = rows.SelectMany(r => r.Sites.Where(s => s.Active))
                .GroupBy(s => new { s.ProjectId, s.Name })
                .Select(g => new InhoudingsplichtIndexVm.ActiveSite { ProjectId = g.Key.ProjectId, Name = g.Key.Name, Count = g.Count() })
                .OrderBy(s => s.Name).ToList();
            vm.Source = vm.ActiveSites.Count > 0 ? "actief" : "alle";
        }

        /// <summary>Opnieuw selecteren: de ondernemingen van een eerdere lijst worden aangevinkt; wie geen leverancier is, komt bij de extra btw-nummers.</summary>
        private async Task ApplyRepeatAsync(InhoudingsplichtIndexVm vm, Guid batchId, CancellationToken ct)
        {
            var batch = await _service.GetBatchAsync(batchId, ct);
            if (batch == null) return;
            var extra = new List<string>();
            foreach (var item in batch.Items)
            {
                var id = RetainmentCheckService.NormalizeIdentifier(item.EnterpriseIdentifier);
                var match = id == null ? null : vm.Suppliers.FirstOrDefault(s => s.Identifier == id);
                if (match != null) vm.SelectedCompanyIds.Add(match.CompanyId);
                else extra.Add((string.IsNullOrWhiteSpace(item.SupplierName) ? "" : item.SupplierName + "; ") + (FormatIdentifier(id) ?? item.EnterpriseIdentifier));
            }
            vm.ExtraText = string.Join("\n", extra);
            vm.ListName = (batch.Name ?? "Controle").Replace(" (herhaling)", "") + " (herhaling)";
            vm.Source = "alle";
        }

        /// <summary>0123456789 → BE0123.456.789; buitenlandse nummers blijven staan.</summary>
        private static string FormatIdentifier(string id) =>
            id != null && id.Length == 10 && id.All(char.IsDigit) ? $"BE{id[..4]}.{id.Substring(4, 3)}.{id[7..]}" : id;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] companyIds, string extra, string name, string lijst, [FromServices] cpmRunningContext db, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.Invoicing))
                return Forbid();

            var inputs = new List<RetainmentCheckInput>();
            var ids = (companyIds ?? Array.Empty<int>()).Distinct().ToList();
            if (ids.Count > 0)
            {
                var companies = await db.CompanyInfo.AsNoTracking().Where(c => ids.Contains(c.CompanyId)).ToListAsync(ct);
                inputs.AddRange(companies.Select(c => new RetainmentCheckInput { SupplierName = c.BedrijfsNaam, Identifier = string.IsNullOrWhiteSpace(c.VatNumber) ? c.Ondernemingsnummer : c.VatNumber }));
            }
            var parsed = ParseExtra(extra ?? lijst, out var invalid);
            if (parsed.Any(p => string.IsNullOrWhiteSpace(p.SupplierName)))
            {
                // Een geplakt nummer van een bestaande leverancier krijgt zijn naam, zodat de lijst (het bewijs) leesbaar blijft.
                var all = await db.CompanyInfo.AsNoTracking().Where(c => !c.IsCustomer).Select(c => new { c.BedrijfsNaam, c.VatNumber, c.Ondernemingsnummer }).ToListAsync(ct);
                var names = new Dictionary<string, string>();
                foreach (var c in all)
                    foreach (var raw in new[] { c.VatNumber, c.Ondernemingsnummer })
                    {
                        var key = RetainmentCheckService.NormalizeIdentifier(raw);
                        if (key != null) names.TryAdd(key, c.BedrijfsNaam);
                    }
                foreach (var p in parsed.Where(p => string.IsNullOrWhiteSpace(p.SupplierName)))
                    if (names.TryGetValue(RetainmentCheckService.NormalizeIdentifier(p.Identifier) ?? "", out var found)) p.SupplierName = found;
            }
            var known = new HashSet<string>(inputs.Select(i => RetainmentCheckService.NormalizeIdentifier(i.Identifier)).Where(i => i != null));
            inputs.AddRange(parsed.Where(p => known.Add(RetainmentCheckService.NormalizeIdentifier(p.Identifier))));

            if (inputs.Count == 0)
            {
                AddMessage("warning", "Kies minstens één aannemer of plak een btw-nummer.", "Inhoudingsplicht");
                return RedirectToAction(nameof(Index));
            }
            if (inputs.Count > 200)
            {
                AddMessage("warning", "Maximaal 200 ondernemingen per controlelijst.", "Inhoudingsplicht");
                return RedirectToAction(nameof(Index));
            }

            var batch = await _service.CheckBatchAsync(inputs, User.Identity?.Name, ct, string.IsNullOrWhiteSpace(name) ? null : name.Trim());
            AddMessage("success", invalid > 0 ? $"Controlelijst bewaard. {invalid} ongeldig nummer overgeslagen." : "Controlelijst bewaard.", "Inhoudingsplicht");
            return RedirectToAction(nameof(Index), new { open = batch.BatchId });
        }

        /// <summary>Controleert één leverancier vanaf zijn fiche en keert daarheen terug; de controle blijft bewaard (en telt als bewijs) zoals elke andere.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckSupplier(int companyId, [FromServices] cpmRunningContext db, CancellationToken ct = default)
        {
            await _permissionService.EnsureLoadedAsync();
            if (!_permissionService.HasWrite(PermissionCodes.Invoicing))
                return Forbid();

            var supplier = await db.CompanyInfo.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId, ct);
            if (supplier == null) return NotFound();

            var identifier = !string.IsNullOrWhiteSpace(supplier.VatNumber) ? supplier.VatNumber : supplier.Ondernemingsnummer;
            var batch = await _service.CheckBatchAsync(new[] { new RetainmentCheckInput { SupplierName = supplier.BedrijfsNaam, Identifier = identifier } }, User.Identity?.Name, ct);
            var item = batch.Items.FirstOrDefault();
            if (item == null || item.Failed)
                AddMessage("warning", item?.ErrorMessage ?? "De controle is mislukt.", "Inhoudingsplicht");
            else
                AddMessage(item.HasObligation ? "warning" : "success", item.HasObligation ? "Deze leverancier heeft een inhoudingsplicht." : "Geen inhoudingsplicht voor deze leverancier.", "Inhoudingsplicht");
            return RedirectToAction("Details", "Leveranciers", new { id = companyId });
        }

        [Breadcrumb("Controlelijst", FromController = typeof(InhoudingsplichtController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            var batch = await _service.GetBatchAsync(id, ct);
            if (batch == null) return NotFound();
            SetPageHeader("bx bx-shield-quarter", "Controlelijst inhoudingsplicht");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailsV2" : "Details", batch);
        }

        /// <summary>De ruwe RSZ-resultaten van een lijst als JSON-bestand: het technische bewijs naast de afdrukbare lijst.</summary>
        public async Task<IActionResult> Bewijs(Guid id, CancellationToken ct = default)
        {
            var batch = await _service.GetBatchAsync(id, ct);
            if (batch == null) return NotFound();

            var doc = new
            {
                batchId = batch.BatchId,
                name = batch.Name,
                checkedAtUtc = batch.CheckedAt,
                checkedBy = batch.CheckedBy,
                environment = batch.Environment,
                items = batch.Items.Select(i => new
                {
                    supplierName = i.SupplierName,
                    identifierSent = i.EnterpriseIdentifier,
                    retainmentCheckReference = i.RetainmentCheckReference,
                    belGovTraceId = i.TraceId,
                    error = i.ErrorMessage,
                    rszResponse = string.IsNullOrEmpty(i.RawResponse) ? (JsonElement?)null : JsonDocument.Parse(i.RawResponse).RootElement.Clone()
                })
            };
            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            return File(Encoding.UTF8.GetBytes(json), "application/json", $"inhoudingsplicht-{batch.CheckedAt:yyyyMMdd-HHmm}-{batch.BatchId.ToString()[..8]}.json");
        }

        /// <summary>
        /// Geplakte extra nummers: één per lijn of gescheiden door komma's; met of zonder BE, punten of spaties; een naam ervoor mag ("naam; nummer").
        /// Onbruikbare nummers worden overgeslagen en geteld.
        /// </summary>
        private static List<RetainmentCheckInput> ParseExtra(string text, out int invalid)
        {
            invalid = 0;
            var result = new List<RetainmentCheckInput>();
            foreach (var raw in (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                string name = null;
                string[] numbers;
                var sep = line.IndexOfAny(new[] { ';', '\t' });
                if (sep >= 0) { name = line[..sep].Trim(); numbers = new[] { line[(sep + 1)..] }; }
                else numbers = line.Split(',');

                foreach (var number in numbers)
                {
                    if (string.IsNullOrWhiteSpace(number)) continue;
                    var id = RetainmentCheckService.NormalizeIdentifier(number);
                    var valid = id != null && ((id.Length == 10 && id.All(char.IsDigit)) || (id.Length >= 6 && char.IsLetter(id[0]) && char.IsLetter(id[1])));
                    if (!valid) { invalid++; continue; }
                    result.Add(new RetainmentCheckInput { SupplierName = string.IsNullOrWhiteSpace(name) ? null : name, Identifier = number.Trim() });
                }
            }
            return result;
        }
    }
}
