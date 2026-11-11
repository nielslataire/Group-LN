using BOCore;
using CPMCore.Models.Projecten;
using CPMCore.Services;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>Offerte / wijzigingsopdracht met meerdere leveranciers (design-handoff 28t en 28u, migratie 140): de
    /// leverancier hoort niet meer bij het document maar bij een GROEP regels (ChangeOrderGroup). Hier staan het bouwen
    /// van de groepen voor het scherm, het bewaren ervan, het zoeken van een leverancier en het toevoegen van een nieuwe
    /// groep (AJAX, geen modal). Het document zelf blijft één offerte/WO met één handtekening; de klant ziet enkel de
    /// groepstitels en de prijzen klant. ChangeOrder.ContractActivityId/QuoteSource*/Invoiceable spiegelen de eerste groep,
    /// zodat alles wat er nog op leunt (lijst, nummering, ondertekening, facturatie, legacy-schermen) werkt zoals vroeger.</summary>
    public partial class ProjectenController
    {
        private static string SupplierInitials(string name)
        {
            var letters = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.FirstOrDefault(char.IsLetter)).Where(c => c != default(char)).Take(2).ToArray();
            return letters.Length == 0 ? "·" : new string(letters).ToUpperInvariant();
        }

        /// <summary>De contracten (activiteiten) van elke leverancier in dit project: de keuze bij "Contract" per groep.</summary>
        private async Task<Dictionary<int, List<IdNameBO>>> LoadContractOptionsByCompanyAsync(int projectId)
        {
            var rows = await _db.ContractActivity.AsNoTracking()
                .Where(a => a.Contract.ProjectId == projectId)
                .OrderBy(a => a.Id)
                .Select(a => new { a.Id, a.Contract.CompanyId, Activity = a.Activity.Omschrijving })
                .ToListAsync();
            return rows.GroupBy(r => r.CompanyId)
                .ToDictionary(g => g.Key, g => g.Select(r => new IdNameBO { ID = r.Id, Display = r.Activity }).ToList());
        }

        /// <summary>Bouwt de groepen voor het scherm uit de bewaarde rij; <paramref name="rows"/> (de platte regels van het
        /// scherm) worden over de groepen verdeeld en krijgen hun GroupKey. Regels zonder (bekende) groep horen bij de eerste.</summary>
        private async Task<List<ChangeOrderGroupV2>> BuildGroupsAsync(ChangeOrder co, List<ChangeOrderDetailRowV2> rows, int projectId)
        {
            var result = new List<ChangeOrderGroupV2>();
            var groups = co.Groups.OrderBy(g => g.SortOrder).ThenBy(g => g.Id).ToList();
            if (groups.Count == 0)
            {
                if (co.ChangeOrderDetail.Count == 0) return result;
                // Regels zonder groep (document bewaard vóór de groepen bestonden, of vóór migratie 140 draaide): één groep uit de gegevens van het
                // document zelf, zodat de regels zichtbaar en bewerkbaar blijven. Bij het opslaan wordt ze een echte groep.
                groups.Add(new ChangeOrderGroup
                {
                    Id = 0, SortOrder = 0,
                    SupplierCompanyId = co.ContractActivity?.Contract?.CompanyId,
                    ContractActivityId = co.ContractActivityId,
                    ContractActivity = co.ContractActivity,
                    QuoteSupplierReference = co.QuoteSupplierReference,
                    QuoteSourcePath = co.QuoteSourcePath,
                    QuoteSourceFileName = co.QuoteSourceFileName,
                    InvoicedByUs = co.Invoiceable,
                    SupplierCompany = co.ContractActivity?.Contract?.Company,
                });
            }

            var contractOptions = await LoadContractOptionsByCompanyAsync(projectId);
            var allContracts = (_projectService.GetProjectContractActivitiesForSelect(projectId) is { Success: true } allResp ? allResp.Values : new List<IdNameBO>()).ToList();
            var storage = HttpContext.RequestServices.GetRequiredService<DocStorageService>();
            var groupIdByDetailId = co.ChangeOrderDetail.ToDictionary(d => d.Id, d => d.GroupId);

            for (var i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var supplierName = g.SupplierCompany?.BedrijfsNaam ?? g.ContractActivity?.Contract?.Company?.BedrijfsNaam ?? "";
                var activityName = g.ContractActivity?.Activity?.Omschrijving ?? "";
                var vm = new ChangeOrderGroupV2
                {
                    Key = "g" + i,
                    Id = g.Id,
                    SupplierCompanyId = g.SupplierCompanyId ?? g.ContractActivity?.Contract?.CompanyId ?? 0,
                    SupplierName = supplierName,
                    Initials = SupplierInitials(supplierName),
                    Title = string.IsNullOrWhiteSpace(g.Title) ? activityName : g.Title,
                    ContractActivityId = g.ContractActivityId ?? 0,
                    ContractLabel = g.ContractActivityId.HasValue ? "Contract · " + activityName : "",
                    QuoteSupplierReference = g.QuoteSupplierReference,
                    QuoteSourcePath = g.QuoteSourcePath,
                    QuoteSourceFileName = g.QuoteSourceFileName,
                    InvoicedByUs = g.InvoicedByUs,
                };
                if (vm.SupplierCompanyId > 0 && contractOptions.TryGetValue(vm.SupplierCompanyId, out var options)) vm.ContractOptions = options;
                else if (vm.SupplierCompanyId == 0) vm.ContractOptions = allContracts;   // leverancier nog onbekend: elk contract van het project kan gekozen worden
                if (!string.IsNullOrWhiteSpace(g.QuoteSourcePath))
                {
                    vm.QuoteSourceUrl = await storage.GetSignedUrlAsync(g.QuoteSourcePath, "quotes");
                    var ext = System.IO.Path.GetExtension(g.QuoteSourceFileName ?? g.QuoteSourcePath).ToLowerInvariant();
                    vm.QuoteSourceIsImage = ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif";
                }
                result.Add(vm);
            }

            var byGroupId = result.ToDictionary(g => g.Id);
            foreach (var row in rows)
            {
                var target = groupIdByDetailId.TryGetValue(row.Id, out var gid) && gid.HasValue && byGroupId.TryGetValue(gid.Value, out var found) ? found : result[0];
                row.GroupKey = target.Key;
                target.Rows.Add(row);
            }
            return result;
        }

        /// <summary>Zet de groepen uit het formulier om naar ChangeOrderGroup-rijen (nieuw/gewijzigd/verwijderd) en geeft per
        /// formuliersleutel de groep terug, zodat SyncRows elke regel aan haar groep kan hangen. Spiegelt daarna de eerste groep
        /// naar de oude kolommen op het document.</summary>
        private async Task<Dictionary<string, ChangeOrderGroup>> SyncGroupsAsync(ChangeOrder co, List<ChangeOrderGroupPostV2> posted)
        {
            var map = new Dictionary<string, ChangeOrderGroup>();
            var postedIds = posted.Where(g => g.Id > 0).Select(g => g.Id).ToHashSet();
            foreach (var stale in co.Groups.Where(g => g.Id > 0 && !postedIds.Contains(g.Id)).ToList())
                _db.ChangeOrderGroup.Remove(stale);

            var sortOrder = 0;
            foreach (var p in posted)
            {
                var entity = p.Id > 0 ? co.Groups.FirstOrDefault(g => g.Id == p.Id) : null;
                if (entity is null)
                {
                    entity = new ChangeOrderGroup();
                    co.Groups.Add(entity);
                }
                entity.SortOrder = sortOrder++;
                entity.SupplierCompanyId = p.SupplierCompanyId > 0 ? p.SupplierCompanyId : null;
                var title = (p.Title ?? "").Trim();
                entity.Title = title.Length == 0 ? null : (title.Length > 150 ? title[..150] : title);
                entity.ContractActivityId = p.ContractActivityId > 0 ? p.ContractActivityId : null;
                entity.QuoteSupplierReference = string.IsNullOrWhiteSpace(p.QuoteSupplierReference) ? null : p.QuoteSupplierReference.Trim();
                if (!string.IsNullOrWhiteSpace(p.QuoteSourcePath))
                {
                    entity.QuoteSourcePath = p.QuoteSourcePath.Trim();
                    entity.QuoteSourceFileName = string.IsNullOrWhiteSpace(p.QuoteSourceFileName) ? null : p.QuoteSourceFileName.Trim();
                }
                entity.InvoicedByUs = p.InvoicedByUs;
                if (!string.IsNullOrEmpty(p.Key)) map[p.Key] = entity;
            }
            // Een groep zonder leverancier (oud document zonder contract) die nu een contract kreeg: de leverancier volgt het contract.
            var needSupplier = map.Values.Where(g => !g.SupplierCompanyId.HasValue && g.ContractActivityId.HasValue).ToList();
            if (needSupplier.Count > 0)
            {
                var activityIds = needSupplier.Select(g => g.ContractActivityId.Value).Distinct().ToList();
                var companyByActivity = await _db.ContractActivity.AsNoTracking().Where(a => activityIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, a => a.Contract.CompanyId);
                foreach (var g in needSupplier)
                    if (companyByActivity.TryGetValue(g.ContractActivityId.Value, out var companyId)) g.SupplierCompanyId = companyId;
            }
            MirrorFirstGroup(co);
            return map;
        }

        /// <summary>Oude vorm (QuoteIntakeV2/20c en alles wat nog geen groepen post): één groep met de leverancierskant van het
        /// document. <paramref name="targetGroupId"/> = de groep waarvoor 20c inlas (anders de eerste, of een nieuwe).</summary>
        private async Task<ChangeOrderGroup> ApplyLegacyGroupAsync(ChangeOrder co, ChangeOrderDetailV2SaveModel model, bool fromIntake)
        {
            var group = model.GroupId > 0 ? co.Groups.FirstOrDefault(g => g.Id == model.GroupId) : null;
            group ??= co.Groups.OrderBy(g => g.SortOrder).ThenBy(g => g.Id).FirstOrDefault();
            if (group is null)
            {
                group = new ChangeOrderGroup();
                co.Groups.Add(group);
            }
            if (model.ContractActivityId > 0)
            {
                group.ContractActivityId = model.ContractActivityId;
                group.SupplierCompanyId = await _db.ContractActivity.AsNoTracking()
                    .Where(a => a.Id == model.ContractActivityId).Select(a => (int?)a.Contract.CompanyId).FirstOrDefaultAsync() ?? group.SupplierCompanyId;
            }
            else group.ContractActivityId = null;
            group.QuoteSupplierReference = string.IsNullOrWhiteSpace(model.QuoteSupplierReference) ? null : model.QuoteSupplierReference.Trim();
            if (!string.IsNullOrWhiteSpace(model.QuoteSourcePath))
            {
                group.QuoteSourcePath = model.QuoteSourcePath.Trim();
                group.QuoteSourceFileName = string.IsNullOrWhiteSpace(model.QuoteSourceFileName) ? null : model.QuoteSourceFileName.Trim();
            }
            if (!fromIntake) group.InvoicedByUs = model.InvoiceableByBouwheer;
            MirrorFirstGroup(co);
            return group;
        }

        /// <summary>ChangeOrder.ContractActivityId/QuoteSource*/Invoiceable = de eerste groep (de rest van het systeem leunt er nog op);
        /// Invoiceable blijft aan zodra minstens één groep via ons gefactureerd wordt.</summary>
        private static void MirrorFirstGroup(ChangeOrder co)
        {
            var groups = co.Groups.OrderBy(g => g.SortOrder).ToList();
            var first = groups.FirstOrDefault();
            if (first is null) return;
            co.ContractActivityId = first.ContractActivityId;
            co.QuoteSupplierReference = first.QuoteSupplierReference;
            if (!string.IsNullOrWhiteSpace(first.QuoteSourcePath))
            {
                co.QuoteSourcePath = first.QuoteSourcePath;
                co.QuoteSourceFileName = first.QuoteSourceFileName;
            }
            co.Invoiceable = groups.Any(g => g.InvoicedByUs);
        }

        /// <summary>Kopieert de groepen van een document naar een nieuw document (omzetten, kopie, nieuwe versie) en geeft per
        /// oude groep-Id de nieuwe groep terug, zodat de regels aan de juiste groep gehangen worden.</summary>
        private static Dictionary<int, ChangeOrderGroup> CopyGroups(ChangeOrder from, ChangeOrder to)
        {
            var map = new Dictionary<int, ChangeOrderGroup>();
            foreach (var g in from.Groups.OrderBy(g => g.SortOrder).ThenBy(g => g.Id))
            {
                var copy = new ChangeOrderGroup
                {
                    SortOrder = g.SortOrder,
                    SupplierCompanyId = g.SupplierCompanyId,
                    Title = g.Title,
                    ContractActivityId = g.ContractActivityId,
                    QuoteSupplierReference = g.QuoteSupplierReference,
                    QuoteSourcePath = g.QuoteSourcePath,
                    QuoteSourceFileName = g.QuoteSourceFileName,
                    InvoicedByUs = g.InvoicedByUs,
                };
                to.Groups.Add(copy);
                map[g.Id] = copy;
            }
            return map;
        }

        /// <summary>De contracten van één leverancier in dit project (voor "Contract" in de kop van een groep nadat je er later een leverancier voor koos).</summary>
        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2SupplierContracts(int projectId, int companyId)
        {
            var options = (await LoadContractOptionsByCompanyAsync(projectId)).GetValueOrDefault(companyId) ?? new List<IdNameBO>();
            return Json(options.Select(o => new { value = o.ID.ToString(), text = "Contract · " + o.Display, title = o.Display }));
        }

        /// <summary>Leverancier zoeken (28u, keuzelijst 34): eerst leveranciers met een contract in dit project, dan de andere.
        /// Het keuzelijst-component post {term, countryId, activeOnly} en verwacht [{id, text, sub, group}].</summary>
        [HttpPost]
        public async Task<IActionResult> ChangeOrderDetailV2SupplierLookup(int projectId, string? term, CancellationToken ct = default)
        {
            var like = string.IsNullOrWhiteSpace(term) ? null : $"%{term.Trim()}%";
            var activityByCompany = (await _db.ContractActivity.AsNoTracking()
                    .Where(a => a.Contract.ProjectId == projectId)
                    .Select(a => new { a.Contract.CompanyId, Activity = a.Activity.Omschrijving })
                    .ToListAsync(ct))
                .GroupBy(a => a.CompanyId)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(a => a.Activity).Distinct()));

            var withContractIds = activityByCompany.Keys.ToList();
            var withContractQuery = _db.CompanyInfo.AsNoTracking().Where(c => withContractIds.Contains(c.CompanyId));
            var othersQuery = _db.CompanyInfo.AsNoTracking().Where(c => !withContractIds.Contains(c.CompanyId));
            if (like != null)
            {
                withContractQuery = withContractQuery.Where(c => EF.Functions.Like(c.BedrijfsNaam, like));
                othersQuery = othersQuery.Where(c => EF.Functions.Like(c.BedrijfsNaam, like));
            }

            var withContract = await withContractQuery.OrderBy(c => c.BedrijfsNaam).Take(20).Select(c => new { c.CompanyId, c.BedrijfsNaam }).ToListAsync(ct);
            var others = await othersQuery.OrderBy(c => c.BedrijfsNaam).Take(20).Select(c => new { c.CompanyId, c.BedrijfsNaam }).ToListAsync(ct);

            var items = withContract
                .Select(c => new { id = c.CompanyId, text = c.BedrijfsNaam ?? "", sub = activityByCompany.GetValueOrDefault(c.CompanyId, ""), group = "MET CONTRACT IN DIT PROJECT" })
                .Concat(others.Select(c => new { id = c.CompanyId, text = c.BedrijfsNaam ?? "", sub = "", group = "ANDERE LEVERANCIERS" }));
            return Json(items);
        }

        /// <summary>"Leverancier toevoegen" (28u): rendert een nieuwe groep voor de gekozen leverancier met één lege regel.
        /// Titel = activiteit van het contract; het contract wordt automatisch gekoppeld als de leverancier er één heeft in
        /// dit project (anders "Geen contract · koppelen"); facturatie standaard via ons.</summary>
        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2AddGroup(int projectId, int companyId, int groupIndex, int rowIndex, decimal vatKlant, int? vatKlantTypeId = null, decimal commission = 0)
        {
            // companyId 0 = een groep zonder leverancier: de leverancier (en het contract) kies je later in de kop van de groep.
            var company = companyId > 0
                ? await _db.CompanyInfo.AsNoTracking().Where(c => c.CompanyId == companyId).Select(c => new { c.CompanyId, c.BedrijfsNaam }).FirstOrDefaultAsync()
                : new { CompanyId = 0, BedrijfsNaam = "" };
            if (company is null) return NotFound();

            var options = companyId > 0
                ? (await LoadContractOptionsByCompanyAsync(projectId)).GetValueOrDefault(companyId) ?? new List<IdNameBO>()
                : (_projectService.GetProjectContractActivitiesForSelect(projectId) is { Success: true } allResp ? allResp.Values.ToList() : new List<IdNameBO>());
            var contract = companyId > 0 && options.Count == 1 ? options[0] : null;
            var group = new ChangeOrderGroupV2
            {
                Key = "g" + groupIndex + "n" + DateTime.UtcNow.Ticks % 100000,
                SupplierCompanyId = company.CompanyId,
                SupplierName = company.BedrijfsNaam ?? "",
                Initials = SupplierInitials(company.BedrijfsNaam),
                Title = contract?.Display ?? "",
                ContractActivityId = contract?.ID ?? 0,
                ContractLabel = contract != null ? "Contract · " + contract.Display : "",
                ContractOptions = options,
                InvoicedByUs = true,
            };
            var row = new ChangeOrderDetailRowV2 { Number = 1, VatPercentage = vatKlant, VatTypeId = vatKlantTypeId, Commission = commission, MeasurementType = (int)MeasurementType.Vermoedelijk, MeasurementUnit = (int)MeasurementUnit.stuk, GroupKey = group.Key };
            group.Rows.Add(row);

            ViewData["ProjectId"] = projectId;
            ViewData["ChangeOrderId"] = 0;
            ViewData["GroupIndex"] = groupIndex;
            ViewData["RowStart"] = rowIndex;
            ViewData["VatKlant"] = vatKlant;
            ViewData["VatTypes"] = await LoadVatTypeOptionsAsync(projectId, vatKlantTypeId);
            ViewData["IsLocked"] = false;
            ViewData["Editable"] = true;
            ViewData["CanRemove"] = true;
            return PartialView("Partials/_ChangeOrderGroupV2", group);
        }
    }
}
