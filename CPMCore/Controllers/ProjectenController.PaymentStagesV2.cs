using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Projecten;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 Betalingsschijven-pagina (design-handoff 21g/21h, "Betalingsschijven — schijven ×
    /// eenheden, btw van de groep, eindafrekening gemarkeerd" / "Schijf bereikt aanduiden"). Eigen
    /// bestand (partial) naast de legacy <c>PaymentStages</c>-actie in ProjectenController.cs, die
    /// ongewijzigd blijft. Introduceert <c>UnitPaymentStageReached</c> (migratie 062) als fijnmaziger,
    /// per-eenheid alternatief bovenop de bestaande groep-brede <c>InvoicingPaymentStages.Invoicable</c>-
    /// vlag: een eenheid×schijf-combinatie is factureerbaar zodra ÓF de groep-vlag aanstaat ÓF er een
    /// UnitPaymentStageReached-rij bestaat — zie <see cref="IsReached"/>, ook gebruikt door
    /// ProjectenController.InvoicingV2.cs zodra dat hierop overschakelt.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public IActionResult PaymentStagesV2(int projectid)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsInvoicing);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            var vm = new PaymentStagesV2Vm { ProjectId = projectid, ProjectName = project.Name, CanWrite = canWrite };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(PaymentStagesV2), "Projecten", "Betalingsschijven") { Parent = projectDetail, RouteValues = new { projectid = projectid } };

            var groups = _db.InvoicingPaymentGroup.AsNoTracking()
                .Where(g => g.ProjectId == projectid)
                .OrderBy(g => g.Id)
                .ToList();
            if (groups.Count == 0) return View(vm);

            var groupIds = groups.Select(g => g.Id).ToList();
            var stages = _db.InvoicingPaymentStages.AsNoTracking()
                .Where(s => groupIds.Contains(s.GroupId))
                .OrderBy(s => s.Id)
                .ToList();

            // Eenheden per groep: via UnitConstructionValue.PaymentGroupId, zelfde koppeling als
            // InvoicingV2.cs. Enkel PROJECTId hoeft niet expliciet herhaald — een eenheid met een
            // ConstructionValue in deze groep hoort per definitie bij dit project.
            var units = _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectid)
                .Include(u => u.Type)
                .Include(u => u.ClientAccount)
                .Include(u => u.UnitConstructionValue)
                .ToList();

            var unitIds = units.Select(u => u.Id).ToList();
            var invoicedPairs = _db.InvoicesDetails.AsNoTracking()
                .Where(d => d.LineType == "Stages" && d.PaymentStageId.HasValue && d.UnitId.HasValue && unitIds.Contains(d.UnitId.Value))
                .Select(d => new { StageId = d.PaymentStageId!.Value, UnitId = d.UnitId!.Value })
                .ToList().Select(x => (x.StageId, x.UnitId)).ToHashSet();

            var stageIds = stages.Select(s => s.Id).ToList();
            var reachedPairs = _db.UnitPaymentStageReached.AsNoTracking()
                .Where(r => stageIds.Contains(r.PaymentStageId) && unitIds.Contains(r.UnitId))
                .Select(r => new { r.PaymentStageId, r.UnitId })
                .ToList().Select(x => (x.PaymentStageId, x.UnitId)).ToHashSet();

            foreach (var group in groups)
            {
                // Elke eenheid die aan deze groep gekoppeld is (via UnitConstructionValue.PaymentGroupId)
                // krijgt een kolom, ook als ValueSold nog leeg is (akte nog niet verleden/aktedatum nog
                // niet ingevuld) — die eenheid toont dan gewoon "—" in elke schijf-rij (Niels, 2026-09-30).
                // Voordien liet de ValueSold>0-voorwaarde die eenheden volledig uit de matrix vallen.
                var groupUnits = units.Where(u => u.UnitConstructionValue.Any(cv => cv.PaymentGroupId == group.Id))
                    .OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                // Een groep zonder eenheden blijft zichtbaar (kaart zonder eenheid-kolommen): anders is een
                // net aangemaakte groep (punt 26) onvindbaar tot er een eenheid aan hangt.

                var card = new PaymentGroupCardV2 { GroupId = group.Id, Name = group.Name, VatPercentage = group.VatPercentage ?? 21m };
                foreach (var u in groupUnits)
                    card.Units.Add(new PaymentGroupUnitV2 { UnitId = u.Id, UnitName = u.Name, IsSold = u.ClientAccountId.HasValue });

                var groupStages = stages.Where(s => s.GroupId == group.Id).ToList();
                for (var i = 0; i < groupStages.Count; i++)
                {
                    var stage = groupStages[i];
                    var row = new PaymentStageRowV2 { StageId = stage.Id, Number = i + 1, Name = stage.Name, Percentage = stage.Percentage, IsLast = i == groupStages.Count - 1, IsGroupForced = stage.Invoicable };
                    foreach (var u in groupUnits)
                    {
                        var hasSoldValue = u.UnitConstructionValue.Any(cv => cv.PaymentGroupId == group.Id && cv.ValueSold > 0);
                        var state = !u.ClientAccountId.HasValue ? "not-sold"
                            : !hasSoldValue ? "not-invoicable"
                            : invoicedPairs.Contains((stage.Id, u.Id)) ? "invoiced"
                            : IsReached(stage, reachedPairs, u.Id) ? "reached"
                            : "not-reached";
                        row.CellsByUnitId[u.Id] = new PaymentStageCellV2 { State = state };
                    }
                    row.HasActionableUnits = row.CellsByUnitId.Values.Any(c => c.State == "not-reached");
                    card.Stages.Add(row);
                }
                vm.Groups.Add(card);
            }

            return View(vm);
        }

        /// <summary>Eenheid×schijf is factureerbaar zodra ÓF de groep-brede Invoicable-vlag aanstaat ÓF er
        /// een UnitPaymentStageReached-rij voor deze specifieke eenheid bestaat (migratie 062).</summary>
        private static bool IsReached(InvoicingPaymentStages stage, HashSet<(int StageId, int UnitId)> reachedPairs, int unitId)
            => stage.Invoicable || reachedPairs.Contains((stage.Id, unitId));

        [HttpGet]
        public IActionResult MarkStageReachedModal(int projectId, int stageId)
        {
            var stage = _db.InvoicingPaymentStages.AsNoTracking().Include(s => s.Group).FirstOrDefault(s => s.Id == stageId);
            if (stage is null) return NotFound();

            var groupStageIds = _db.InvoicingPaymentStages.AsNoTracking().Where(s => s.GroupId == stage.GroupId).OrderBy(s => s.Id).Select(s => s.Id).ToList();
            var position = groupStageIds.IndexOf(stageId) + 1;

            var units = _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId)
                .Include(u => u.Type)
                .Include(u => u.ClientAccount)
                .Include(u => u.UnitConstructionValue)
                .Where(u => u.UnitConstructionValue.Any(cv => cv.PaymentGroupId == stage.GroupId))
                .OrderBy(u => u.Name)
                .ToList();

            var unitIds = units.Select(u => u.Id).ToList();
            var invoicedUnitIds = _db.InvoicesDetails.AsNoTracking()
                .Where(d => d.LineType == "Stages" && d.PaymentStageId == stageId && d.UnitId.HasValue && unitIds.Contains(d.UnitId.Value))
                .Select(d => d.UnitId!.Value).ToHashSet();
            var alreadyReachedUnitIds = _db.UnitPaymentStageReached.AsNoTracking()
                .Where(r => r.PaymentStageId == stageId && unitIds.Contains(r.UnitId))
                .Select(r => r.UnitId).ToHashSet();

            var vm = new MarkStageReachedV2Vm { ProjectId = projectId, StageId = stageId, StageNumber = position, StageName = stage.Name, StagePercentage = stage.Percentage };
            foreach (var u in units)
            {
                var cv = u.UnitConstructionValue.FirstOrDefault(v => v.PaymentGroupId == stage.GroupId && v.ValueSold > 0);
                var amount = Math.Round((cv?.ValueSold ?? 0m) * stage.Percentage / 100m, 2, MidpointRounding.AwayFromZero);

                bool actionable; string? reason = null;
                if (!u.ClientAccountId.HasValue) { actionable = false; reason = "nog niet verkocht"; }
                else if (cv is null) { actionable = false; reason = "akte nog niet verleden"; }
                else if (invoicedUnitIds.Contains(u.Id)) { actionable = false; reason = "al gefactureerd"; }
                else if (stage.Invoicable || alreadyReachedUnitIds.Contains(u.Id)) { actionable = false; reason = "al bereikt"; }
                else actionable = true;

                vm.Units.Add(new MarkStageReachedUnitV2
                {
                    UnitId = u.Id,
                    UnitName = u.Name + (u.ClientAccount != null ? $" · {u.ClientAccount.Name}" : ""),
                    Amount = amount,
                    IsActionable = actionable,
                    DisabledReason = reason,
                });
            }

            return PartialView("Modals/_ModalMarkStageReachedV2", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkStageReached(int projectId, int stageId, List<int> unitIds, DateOnly reachedDate, int? proofMediaId)
        {
            unitIds ??= new List<int>();
            var actorId = User.GetCpmUserId();

            if (unitIds.Count > 0)
            {
                var existing = _db.UnitPaymentStageReached
                    .Where(r => r.PaymentStageId == stageId && unitIds.Contains(r.UnitId))
                    .ToList();
                var existingUnitIds = existing.Select(r => r.UnitId).ToHashSet();

                foreach (var r in existing)
                {
                    r.ReachedDate = reachedDate;
                    if (proofMediaId.HasValue) r.ProofMediaId = proofMediaId;
                }
                foreach (var unitId in unitIds.Where(id => !existingUnitIds.Contains(id)))
                {
                    _db.UnitPaymentStageReached.Add(new UnitPaymentStageReached
                    {
                        UnitId = unitId,
                        PaymentStageId = stageId,
                        ReachedDate = reachedDate,
                        ReachedByUserId = actorId,
                        ProofMediaId = proofMediaId,
                        CreatedAt = DateTime.Now,
                    });
                }
                await _db.SaveChangesAsync();
            }

            AddMessage(unitIds.Count > 0 ? "success" : "error",
                unitIds.Count > 0 ? $"Bereikt voor {unitIds.Count} {(unitIds.Count == 1 ? "eenheid" : "eenheden")}." : "Kies minstens één eenheid.",
                unitIds.Count > 0 ? "Schijf bereikt" : "Niets gekozen");
            return RedirectToAction(nameof(PaymentStagesV2), new { projectid = projectId });
        }

        /// <summary>Schijf terug uitzetten voor één eenheid (Niels, 2026-09-30: "Ik kan een schijf niet
        /// terug uitzetten als deze nog niet gefactureerd is, dit moet wel kunnen"). Verwijdert enkel de
        /// eigen UnitPaymentStageReached-rij — nooit de groep-brede Invoicable-vlag, en nooit als er al
        /// een factuurregel voor deze eenheid×schijf bestaat (server-side herhaling van de "invoiced"-
        /// gate, ook al toont de UI de knop dan al niet).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnmarkStageReached(int projectId, int stageId, int unitId)
        {
            var alreadyInvoiced = _db.InvoicesDetails.AsNoTracking()
                .Any(d => d.LineType == "Stages" && d.PaymentStageId == stageId && d.UnitId == unitId);
            if (alreadyInvoiced)
            {
                AddMessage("error", "Deze schijf is al gefactureerd voor deze eenheid en kan niet meer teruggezet worden.", "Kan niet uitzetten");
                return RedirectToAction(nameof(PaymentStagesV2), new { projectid = projectId });
            }

            var row = await _db.UnitPaymentStageReached.FirstOrDefaultAsync(r => r.PaymentStageId == stageId && r.UnitId == unitId);
            if (row != null)
            {
                _db.UnitPaymentStageReached.Remove(row);
                await _db.SaveChangesAsync();
            }

            AddMessage("success", "Schijf terug uitgezet voor deze eenheid.", "Bereikt ongedaan gemaakt");
            return RedirectToAction(nameof(PaymentStagesV2), new { projectid = projectId });
        }

        /// <summary>21h "BEWIJS" — "Werffoto's kiezen uit Media": kiest uit de bestaande project-Media
        /// (ProjectPictures, enkel foto's) i.p.v. een nieuwe upload of het Documenten-onderdeel. Zelfde
        /// databron als DetailPhotosV2 (<see cref="_projectService"/>.GetPicturesByProjectId), maar
        /// hier read-only en enkel foto's (MediaType==0) — video's zijn geen "werffoto"-bewijs.</summary>
        [HttpGet]
        public IActionResult PickProofPhotoModal(int projectId)
        {
            var imgBase = (Configuration["URL:ImageWebURL"] ?? "").TrimEnd('/');
            var response = _projectService.GetPicturesByProjectId(projectId);
            var photos = response.Success
                ? response.Values.Where(p => p.MediaType == 0).OrderByDescending(p => p.DateTimeUploaded).ToList()
                : new List<ProjectPictureBO>();

            var vm = new PickProofPhotoV2Vm
            {
                ProjectId = projectId,
                Photos = photos.Select(p => new PickProofPhotoItemV2
                {
                    MediaId = p.Id,
                    ThumbUrl = $"{imgBase}/pictures/{p.Name}",
                    Title = string.IsNullOrWhiteSpace(p.Caption) ? p.Name : p.Caption,
                }).ToList(),
            };
            return PartialView("Modals/_ModalPickProofPhotoV2", vm);
        }
    }
}
