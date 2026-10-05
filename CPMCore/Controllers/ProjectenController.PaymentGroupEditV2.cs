using BOCore;
using CPMCore.Models.Projecten;
using FacadeCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 "Betalingsgroep bewerken" + "Nieuwe betalingsgroep" (design-handoff punt 26a-d,
    /// vervolg op 21g). Vervangt in de gl-v2-ervaring het legacy <c>PaymentStagesAddUpdate</c>-scherm
    /// (dat blijft voor de oude layout bestaan en stuurt in gl-v2 hierheen door).
    ///
    /// Schijfvolgorde = Id-volgorde (er is bewust geen volgorde-kolom: PaymentStagesV2, de
    /// "bereikt"-modal en de facturatie sorteren allemaal op Id). Slepen/dupliceren herschikt daarom
    /// door de inhoud van NIET-vaste schijven over de bestaande, nog vrije Id-slots te verdelen
    /// (zie <see cref="AssignStageSlots"/>); vaste schijven behouden hun Id en dus hun plaats.
    /// Vast = gefactureerd, per eenheid bereikt, of trigger van een wijzigingsopdracht-betaalschijf.</summary>
    public partial class ProjectenController
    {
        private const string DefaultEmptyStageName = "Bij de voorlopige oplevering (ontvangst van de sleutels)";

        [HttpGet]
        public IActionResult PaymentGroupEditV2(int projectid, int groupid = 0, int sourceGroupId = 0, string? name = null, int? vatTypeId = null)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!_ps.HasWrite(PermissionCodes.ProjectsInvoicing))
            {
                AddMessage("error", "U hebt geen rechten om betalingsgroepen te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(PaymentStagesV2), new { projectid });
            }

            var project = _db.Project.AsNoTracking().FirstOrDefault(p => p.ProjectId == projectid);
            if (project is null) return NotFound();

            var vm = new PaymentGroupEditV2Vm
            {
                ProjectId = projectid,
                ProjectName = project.ProjectName ?? "",
                GroupId = groupid,
                VatOptions = GetPaymentGroupVatOptions(projectid),
            };

            if (groupid > 0)
            {
                var group = _db.InvoicingPaymentGroup.AsNoTracking().FirstOrDefault(g => g.Id == groupid && g.ProjectId == projectid);
                if (group is null) return NotFound();
                vm.Name = group.Name ?? "";
                vm.VatTypeId = group.VatTypeId;
                FillStagesAndUnits(vm);
            }
            else
            {
                vm.OtherUnits = LoadProjectUnits(projectid, 0);
                if (sourceGroupId > 0)
                {
                    var source = LoadPaymentGroupSources(projectid, 0).FirstOrDefault(s => s.GroupId == sourceGroupId);
                    if (source is not null)
                    {
                        vm.CopiedFromName = source.Name;
                        vm.Name = string.IsNullOrWhiteSpace(name) ? source.Name + " (kopie)" : name.Trim();
                        vm.VatTypeId = vatTypeId ?? source.VatTypeId;
                        vm.Stages = source.Stages.Select(s => new PaymentGroupEditStageV2 { Id = 0, Name = s.Name, Percentage = s.Percentage }).ToList();
                    }
                }
                if (vm.Stages.Count == 0)
                {
                    vm.Name = string.IsNullOrWhiteSpace(name) ? "" : name.Trim();
                    vm.VatTypeId = vatTypeId ?? vm.VatTypeId;
                    vm.Stages.Add(new PaymentGroupEditStageV2 { Name = DefaultEmptyStageName, Percentage = 100m });
                }
            }
            if (vm.VatTypeId is null || vm.VatOptions.All(o => o.Id != vm.VatTypeId))
                vm.VatTypeId = vm.VatOptions.FirstOrDefault()?.Id;

            vm.OtherGroups = LoadPaymentGroupSources(projectid, groupid);
            if (groupid > 0 && _ps.HasDelete(PermissionCodes.ProjectsInvoicing))
            {
                vm.CanDelete = true;
                vm.DeleteBlockReason = PaymentGroupDeleteBlockReason(groupid);
                vm.DeleteUnitCount = _db.Units.Count(u => u.ProjectId == projectid && (u.PaymentGroupId == groupid || u.UnitConstructionValue.Any(v => v.PaymentGroupId == groupid)));
            }
            SetPaymentGroupEditBreadcrumb(vm);
            return View("Invoicing/PaymentGroupEditV2", vm);
        }

        /// <summary>Waarom een betalingsgroep niet verwijderd mag worden, of null. Zelfde "vast"-begrip als
        /// het bewerkscherm: gefactureerd, per eenheid bereikt, of trigger van een wijzigingsopdracht-termijn.
        /// Gekoppelde eenheden blokkeren NIET — die worden losgekoppeld.</summary>
        private string? PaymentGroupDeleteBlockReason(int groupId)
        {
            var stageIds = _db.InvoicingPaymentStages.AsNoTracking().Where(s => s.GroupId == groupId).Select(s => s.Id).ToList();
            if (_db.InvoicesDetails.AsNoTracking().Any(d => d.PaymentStageId.HasValue && stageIds.Contains(d.PaymentStageId.Value)))
                return "Er is al gefactureerd op een schijf van deze groep.";
            if (_db.UnitPaymentStageReached.AsNoTracking().Any(r => stageIds.Contains(r.PaymentStageId)))
                return "Een schijf van deze groep is al bereikt voor een eenheid. Zet die eerst terug.";
            if (_db.ChangeOrderPaymentTerm.AsNoTracking().Any(t => t.TriggerStageId.HasValue && stageIds.Contains(t.TriggerStageId.Value)))
                return "Een wijzigingsopdracht factureert bij een schijf van deze groep.";
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePaymentGroupV2(int projectId, int groupId)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasDelete(PermissionCodes.ProjectsInvoicing))
            {
                AddMessage("error", "U hebt geen rechten om betalingsgroepen te verwijderen.", "Geen toegang");
                return RedirectToAction(nameof(PaymentStagesV2), new { projectid = projectId });
            }

            var group = _db.InvoicingPaymentGroup.FirstOrDefault(g => g.Id == groupId && g.ProjectId == projectId);
            if (group is null) return RedirectToAction(nameof(PaymentStagesV2), new { projectid = projectId });

            var reason = PaymentGroupDeleteBlockReason(groupId);
            if (reason != null)
            {
                AddMessage("error", reason, "Niet verwijderd");
                return RedirectToAction(nameof(PaymentGroupEditV2), new { projectid = projectId, groupid = groupId });
            }

            foreach (var u in _db.Units.Where(u => u.ProjectId == projectId && u.PaymentGroupId == groupId).ToList()) u.PaymentGroupId = null;
            foreach (var v in _db.UnitConstructionValue.Where(v => v.PaymentGroupId == groupId).ToList()) v.PaymentGroupId = null;
            _db.InvoicingPaymentStages.RemoveRange(_db.InvoicingPaymentStages.Where(s => s.GroupId == groupId).ToList());
            _db.InvoicingPaymentGroup.Remove(group);
            _db.SaveChanges();

            AddMessage("success", $"De betalingsgroep \"{group.Name}\" is verwijderd.", "Verwijderd");
            return RedirectToAction(nameof(PaymentStagesV2), new { projectid = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePaymentGroupV2(PaymentGroupEditV2Post post)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!_ps.HasWrite(PermissionCodes.ProjectsInvoicing))
            {
                AddMessage("error", "U hebt geen rechten om betalingsgroepen te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(PaymentStagesV2), new { projectid = post.ProjectId });
            }

            var project = _db.Project.AsNoTracking().FirstOrDefault(p => p.ProjectId == post.ProjectId);
            if (project is null) return NotFound();

            InvoicingPaymentGroup? group = null;
            if (post.GroupId > 0)
            {
                group = _db.InvoicingPaymentGroup.Include(g => g.InvoicingPaymentStages)
                    .FirstOrDefault(g => g.Id == post.GroupId && g.ProjectId == post.ProjectId);
                if (group is null) return NotFound();
            }

            var vatOptions = GetPaymentGroupVatOptions(post.ProjectId);
            var lockedIds = group is null ? new HashSet<int>() : GetLockedStageIds(group.InvoicingPaymentStages.Select(s => s.Id).ToList());
            var errors = new List<(string Key, string Message)>();

            var name = (post.Name ?? "").Trim();
            if (name.Length == 0) errors.Add(("Name", "Vul de naam van de betalingsgroep in."));

            if (vatOptions.Count > 0 && (post.VatTypeId is null || vatOptions.All(o => o.Id != post.VatTypeId)))
                errors.Add(("VatTypeId", "Kies een btw-type."));

            var rows = new List<PaymentGroupEditStageV2>();
            for (var i = 0; i < post.StageIds.Count; i++)
            {
                var rowName = (i < post.StageNames.Count ? post.StageNames[i] : "")?.Trim() ?? "";
                var pct = ParsePercentage(i < post.StagePercentages.Count ? post.StagePercentages[i] : null);
                var id = post.StageIds[i];
                rows.Add(new PaymentGroupEditStageV2
                {
                    Id = id,
                    Name = rowName,
                    Percentage = pct ?? 0m,
                    Invoicable = i < post.StageInvoicable.Count && post.StageInvoicable[i],
                    IsLocked = id > 0 && lockedIds.Contains(id),
                });
                if (rowName.Length == 0) errors.Add(($"Stages[{i}]", $"Schijf {i + 1}: vul de omschrijving in."));
                if (pct is null || pct <= 0m || pct > 100m) errors.Add(($"Stages[{i}]", $"Schijf {i + 1}: geef een percentage tussen 0 en 100 op."));
            }
            if (rows.Count == 0) errors.Add(("Stages", "Een betalingsgroep heeft minstens één schijf."));
            else if (Math.Abs(rows.Sum(r => r.Percentage) - 100m) > 0.005m)
                errors.Add(("Stages", $"Het totaal van de schijven moet 100 % zijn (nu {rows.Sum(r => r.Percentage).ToString("0.##", CultureInfo.GetCultureInfo("nl-BE"))} %)."));

            Dictionary<int, int>? slots = null;
            if (group is not null)
            {
                var existingById = group.InvoicingPaymentStages.ToDictionary(s => s.Id);
                if (rows.Any(r => r.Id > 0 && !existingById.ContainsKey(r.Id)))
                    errors.Add(("Stages", "Een schijf hoort niet bij deze groep. Laad de pagina opnieuw."));
                foreach (var lockedId in lockedIds)
                {
                    var row = rows.FirstOrDefault(r => r.Id == lockedId);
                    if (row is null) errors.Add(("Stages", $"“{existingById[lockedId].Name}” is al gefactureerd of bereikt en kan niet verwijderd worden."));
                    else if (row.Percentage < existingById[lockedId].Percentage - 0.0001m)
                        errors.Add(("Stages", $"“{existingById[lockedId].Name}” is al gefactureerd of bereikt: het percentage kan niet verlaagd worden."));
                }
                if (errors.Count == 0)
                {
                    slots = AssignStageSlots(rows, existingById.Keys.OrderBy(i => i).ToList(), lockedIds, out var slotError);
                    if (slots is null) errors.Add(("Stages", slotError!));
                }
            }

            if (errors.Count > 0)
            {
                foreach (var (key, message) in errors) ModelState.AddModelError(key, message);
                var vm = new PaymentGroupEditV2Vm
                {
                    ProjectId = post.ProjectId,
                    ProjectName = project.ProjectName ?? "",
                    GroupId = post.GroupId,
                    Name = post.Name ?? "",
                    VatTypeId = post.VatTypeId,
                    VatOptions = vatOptions,
                    Stages = rows,
                };
                vm.Units = new List<PaymentGroupEditUnitV2>();
                var posted = post.UnitIds.ToHashSet();
                var allUnits = LoadProjectUnits(post.ProjectId, post.GroupId);
                vm.Units = allUnits.Where(u => posted.Contains(u.UnitId)).ToList();
                vm.OtherUnits = allUnits.Where(u => !posted.Contains(u.UnitId)).ToList();
                vm.OtherGroups = LoadPaymentGroupSources(post.ProjectId, post.GroupId);
                SetPaymentGroupEditBreadcrumb(vm);
                return View("Invoicing/PaymentGroupEditV2", vm);
            }

            var vat = post.VatTypeId.HasValue ? _db.Vattype.AsNoTracking().FirstOrDefault(v => v.Id == post.VatTypeId.Value) : null;
            var isNew = group is null;
            group ??= new InvoicingPaymentGroup { ProjectId = post.ProjectId };
            group.Name = name;
            if (vat is not null) { group.VatTypeId = vat.Id; group.VatPercentage = vat.BasePercentage; }
            if (isNew) _db.InvoicingPaymentGroup.Add(group);

            if (isNew)
            {
                foreach (var r in rows)
                    group.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Name = r.Name, Percentage = r.Percentage, Invoicable = false });
            }
            else
            {
                var byId = group.InvoicingPaymentStages.ToDictionary(s => s.Id);
                var used = new HashSet<int>();
                for (var i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    if (slots!.TryGetValue(i, out var slotId))
                    {
                        var entity = byId[slotId];
                        if (r.Id != slotId && !r.IsLocked) entity.DocId = null;
                        entity.Name = r.Name;
                        entity.Percentage = r.Percentage;
                        if (!r.IsLocked) entity.Invoicable = r.Invoicable;
                        used.Add(slotId);
                    }
                    else
                    {
                        group.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Name = r.Name, Percentage = r.Percentage, Invoicable = false });
                    }
                }
                foreach (var stale in byId.Values.Where(s => !used.Contains(s.Id)).ToList())
                    _db.InvoicingPaymentStages.Remove(stale);
            }

            ApplyUnitLinks(group, post.UnitIds);

            try
            {
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Betalingsgroep {GroupId} opslaan mislukt", post.GroupId);
                AddMessage("error", "De betalingsgroep kon niet opgeslagen worden.", "Fout!");
                return RedirectToAction(nameof(PaymentGroupEditV2), new { projectid = post.ProjectId, groupid = post.GroupId });
            }

            AddMessage("success", $"Betalingsgroep “{group.Name}” opgeslagen.", "Geslaagd!");
            return RedirectToAction(nameof(PaymentStagesV2), new { projectid = post.ProjectId });
        }

        /// <summary>26a — "Nieuwe betalingsgroep": inhoud van de modal (AJAX), ook geopend vanuit
        /// "Dupliceer groep" met <paramref name="sourceGroupId"/> al gekozen.</summary>
        [HttpGet]
        public IActionResult NewPaymentGroupModal(int projectId, int sourceGroupId = 0)
        {
            var project = _db.Project.AsNoTracking().FirstOrDefault(p => p.ProjectId == projectId);
            if (project is null) return NotFound();
            var vm = new NewPaymentGroupModalV2Vm
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName ?? "",
                PreselectGroupId = sourceGroupId > 0 ? sourceGroupId : null,
                VatOptions = GetPaymentGroupVatOptions(projectId),
                Sources = LoadPaymentGroupSources(projectId, 0),
            };
            return PartialView("Modals/_ModalNewPaymentGroupV2", vm);
        }

        // ── Hulpmethoden ────────────────────────────────────────────────────────────────────────

        private List<PaymentGroupVatOptionV2> GetPaymentGroupVatOptions(int projectId)
            => GetVatTypeSelectList(projectId)
                .Where(o => int.TryParse(o.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                .Select(o => new PaymentGroupVatOptionV2 { Id = int.Parse(o.Value, CultureInfo.InvariantCulture), Text = o.Text })
                .ToList();

        private static decimal? ParsePercentage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Replace("%", "").Replace(" ", "").Replace(" ", "").Trim();
            // "5,5" (nl-BE) of "5.5"; duizendtallen komen bij een percentage niet voor.
            s = s.Replace(',', '.');
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? Math.Round(v, 2) : null;
        }

        /// <summary>Stage-ids die vastliggen: gefactureerd, per eenheid bereikt of trigger van een
        /// wijzigingsopdracht-betaalschijf.</summary>
        private HashSet<int> GetLockedStageIds(List<int> stageIds)
        {
            if (stageIds.Count == 0) return new HashSet<int>();
            var invoiced = _db.InvoicesDetails.AsNoTracking()
                .Where(d => d.LineType == "Stages" && d.PaymentStageId.HasValue && stageIds.Contains(d.PaymentStageId.Value))
                .Select(d => d.PaymentStageId!.Value).Distinct().ToList();
            var reached = _db.UnitPaymentStageReached.AsNoTracking()
                .Where(r => stageIds.Contains(r.PaymentStageId)).Select(r => r.PaymentStageId).Distinct().ToList();
            var triggers = _db.ChangeOrderPaymentTerm.AsNoTracking()
                .Where(t => t.TriggerStageId.HasValue && stageIds.Contains(t.TriggerStageId.Value))
                .Select(t => t.TriggerStageId!.Value).Distinct().ToList();
            return invoiced.Concat(reached).Concat(triggers).ToHashSet();
        }

        /// <summary>Wijst elke ingediende rij (index) een bestaand Id-slot toe zodat de Id-volgorde gelijk
        /// is aan de ingediende volgorde. Vaste rijen houden hun eigen Id; de andere krijgen het kleinste
        /// vrije, niet-vaste Id dat groter is dan het vorige en kleiner dan het volgende vaste Id. Rijen
        /// zonder passend slot ontbreken in het resultaat en worden als nieuwe schijf (hoogste Id)
        /// ingevoegd — dat kan enkel als er daarna geen vaste schijf meer volgt.</summary>
        private static Dictionary<int, int>? AssignStageSlots(List<PaymentGroupEditStageV2> rows, List<int> existingIdsAsc, HashSet<int> locked, out string? error)
        {
            error = null;
            var result = new Dictionary<int, int>();
            var pool = existingIdsAsc.Where(id => !locked.Contains(id)).ToList();
            var prev = int.MinValue;
            var appended = false;
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.IsLocked)
                {
                    if (r.Id <= prev || appended)
                    {
                        error = "Gefactureerde of bereikte schijven liggen vast: ze kunnen niet voorbij een andere schijf verplaatst worden.";
                        return null;
                    }
                    result[i] = r.Id;
                    prev = r.Id;
                    continue;
                }
                var nextLocked = rows.Skip(i + 1).Where(x => x.IsLocked).Select(x => x.Id).DefaultIfEmpty(int.MaxValue).First();
                var slot = pool.FirstOrDefault(id => id > prev && id < nextLocked);
                if (slot == 0)
                {
                    if (nextLocked != int.MaxValue)
                    {
                        error = "Er is geen plaats voor een extra schijf vóór een gefactureerde of bereikte schijf. Voeg nieuwe schijven toe na de laatste vaste schijf, of vervang een bestaande.";
                        return null;
                    }
                    appended = true;
                    continue;
                }
                pool.Remove(slot);
                result[i] = slot;
                prev = slot;
            }
            return result;
        }

        private void FillStagesAndUnits(PaymentGroupEditV2Vm vm)
        {
            var stages = _db.InvoicingPaymentStages.AsNoTracking().Where(s => s.GroupId == vm.GroupId).OrderBy(s => s.Id).ToList();
            var locked = GetLockedStageIds(stages.Select(s => s.Id).ToList());
            vm.Stages = stages.Select(s => new PaymentGroupEditStageV2
            {
                Id = s.Id,
                Name = s.Name ?? "",
                Percentage = s.Percentage,
                Invoicable = s.Invoicable,
                IsLocked = locked.Contains(s.Id),
            }).ToList();

            var all = LoadProjectUnits(vm.ProjectId, vm.GroupId);
            vm.Units = all.Where(u => u.InGroup).ToList();
            vm.OtherUnits = all.Where(u => !u.InGroup).ToList();
        }

        /// <summary>Alle eenheden van het project; <c>InGroup</c> = gekoppeld aan <paramref name="groupId"/>,
        /// anders staat de naam van hun huidige groep in <c>CurrentGroupName</c>.</summary>
        private List<PaymentGroupEditUnitV2> LoadProjectUnits(int projectId, int groupId)
        {
            var units = _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId)
                .Include(u => u.UnitConstructionValue)
                .OrderBy(u => u.Name)
                .ToList();
            var groupNames = _db.InvoicingPaymentGroup.AsNoTracking().Where(g => g.ProjectId == projectId).ToDictionary(g => g.Id, g => g.Name ?? "");

            var invoicedUnitIds = new HashSet<int>();
            if (groupId > 0)
            {
                var stageIds = _db.InvoicingPaymentStages.AsNoTracking().Where(s => s.GroupId == groupId).Select(s => s.Id).ToList();
                var unitIds = units.Select(u => u.Id).ToList();
                invoicedUnitIds = _db.InvoicesDetails.AsNoTracking()
                    .Where(d => d.LineType == "Stages" && d.PaymentStageId.HasValue && stageIds.Contains(d.PaymentStageId.Value) && d.UnitId.HasValue && unitIds.Contains(d.UnitId.Value))
                    .Select(d => d.UnitId!.Value).Distinct().ToHashSet();
            }

            return units.Select(u =>
            {
                var gid = u.UnitConstructionValue.Select(c => c.PaymentGroupId).FirstOrDefault(g => g.HasValue) ?? u.PaymentGroupId;
                var inGroup = groupId > 0 && u.UnitConstructionValue.Any(c => c.PaymentGroupId == groupId);
                return new PaymentGroupEditUnitV2
                {
                    UnitId = u.Id,
                    Name = u.Name ?? "",
                    InGroup = inGroup,
                    CurrentGroupName = inGroup ? null : (gid.HasValue && groupNames.TryGetValue(gid.Value, out var gn) ? gn : null),
                    IsLocked = inGroup && invoicedUnitIds.Contains(u.Id),
                };
            }).ToList();
        }

        private List<PaymentGroupSourceV2> LoadPaymentGroupSources(int projectId, int excludeGroupId)
        {
            var groups = _db.InvoicingPaymentGroup.AsNoTracking()
                .Where(g => g.Id != excludeGroupId)
                .Select(g => new { g.Id, g.Name, g.ProjectId, ProjectName = g.Project.ProjectName, Municipality = g.Project.PostalCode.Gemeente, Postcode = g.Project.PostalCode.Postcode, g.VatPercentage, g.VatTypeId })
                .ToList();
            if (groups.Count == 0) return new List<PaymentGroupSourceV2>();

            var stages = _db.InvoicingPaymentStages.AsNoTracking()
                .Select(s => new { s.Id, s.GroupId, s.Name, s.Percentage })
                .ToList()
                .GroupBy(s => s.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());
            var unitCounts = _db.UnitConstructionValue.AsNoTracking()
                .Where(c => c.PaymentGroupId != null)
                .Select(c => new { GroupId = c.PaymentGroupId!.Value, c.UnitId })
                .ToList()
                .GroupBy(c => c.GroupId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.UnitId).Distinct().Count());

            return groups
                .Where(g => stages.ContainsKey(g.Id))
                .OrderByDescending(g => g.ProjectId == projectId)
                .ThenBy(g => g.ProjectName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Take(300)
                .Select(g => new PaymentGroupSourceV2
                {
                    GroupId = g.Id,
                    Name = g.Name ?? "",
                    ProjectId = g.ProjectId,
                    ProjectName = g.ProjectName ?? "",
                    ProjectMunicipality = string.Join(" ", new[] { g.Postcode, g.Municipality }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    IsThisProject = g.ProjectId == projectId,
                    VatPercentage = g.VatPercentage ?? 0m,
                    VatTypeId = g.VatTypeId,
                    StageCount = stages[g.Id].Count,
                    UnitCount = unitCounts.TryGetValue(g.Id, out var n) ? n : 0,
                    Stages = stages[g.Id].Select(s => new PaymentGroupEditStageV2 { Id = s.Id, Name = s.Name ?? "", Percentage = s.Percentage }).ToList(),
                })
                .ToList();
        }

        /// <summary>Eenheid-koppeling = <c>UnitConstructionValue.PaymentGroupId</c> (de V2-pagina leest
        /// enkel dat) plus de oudere <c>Units.PaymentGroupId</c>. Een eenheid zonder constructiewaarde
        /// krijgt een lege rij zodat de koppeling bestaat. Eenheden met een factuurregel in deze groep
        /// blijven gekoppeld.</summary>
        private void ApplyUnitLinks(InvoicingPaymentGroup group, List<int> desiredUnitIds)
        {
            var desired = desiredUnitIds.ToHashSet();
            var units = _db.Units.Include(u => u.UnitConstructionValue)
                .Where(u => u.ProjectId == group.ProjectId)
                .ToList();
            var lockedUnitIds = group.Id > 0
                ? LoadProjectUnits(group.ProjectId, group.Id).Where(u => u.IsLocked).Select(u => u.UnitId).ToHashSet()
                : new HashSet<int>();

            foreach (var u in units)
            {
                var inGroup = group.Id > 0 && u.UnitConstructionValue.Any(c => c.PaymentGroupId == group.Id);
                if (desired.Contains(u.Id))
                {
                    if (inGroup) continue;
                    if (u.UnitConstructionValue.Count == 0)
                        u.UnitConstructionValue.Add(new UnitConstructionValue { PaymentGroup = group });
                    else
                        foreach (var c in u.UnitConstructionValue) c.PaymentGroup = group;
                    u.PaymentGroup = group;
                }
                else if (inGroup && !lockedUnitIds.Contains(u.Id))
                {
                    foreach (var c in u.UnitConstructionValue.Where(c => c.PaymentGroupId == group.Id)) c.PaymentGroupId = null;
                    if (u.PaymentGroupId == group.Id) u.PaymentGroupId = null;
                }
            }
        }

        private void SetPaymentGroupEditBreadcrumb(PaymentGroupEditV2Vm vm)
        {
            var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = dashboard };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = vm.ProjectId } };
            var stages = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(PaymentStagesV2), "Projecten", "Betalingsschijven") { Parent = projectDetail, RouteValues = new { projectid = vm.ProjectId } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(PaymentGroupEditV2), "Projecten", vm.IsNew ? "Nieuwe betalingsgroep" : (string.IsNullOrWhiteSpace(vm.Name) ? "Betalingsgroep bewerken" : vm.Name))
            {
                Parent = stages,
                RouteValues = new { projectid = vm.ProjectId, groupid = vm.GroupId }
            };
        }
    }
}
