using BOCore;
using FacadeCore;
using CPMCore.Models.Projecten;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace CPMCore.Controllers
{
    /// <summary>Nacalculatie › Budget instellen, gl-v2 (design-handoff punt 42d). Bouwt op de bestaande
    /// handmatige budgetregels (ProjectBudget / BudgetActivityBO) en overschrijft per activiteit het
    /// definitief budget — zie <see cref="GetBegrootRegelsV2"/>. Geen migratie nodig.</summary>
    public partial class ProjectenController
    {
        private CalculationSettingsV2Vm BuildCalculationSettingsV2Vm(ProjectCalculationSettings model)
        {
            var vm = new CalculationSettingsV2Vm { ProjectId = model.ProjectId, ProjectName = model.ProjectName ?? "" };

            var definitief = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.ProjectId == model.ProjectId && v.Status == BudgetVersie.StatusDefinitief)
                .Select(v => new { v.Id, v.Versienummer, Naam = v.BudgetMaster.Naam })
                .FirstOrDefault();

            // Definitief bedrag per activiteit (som van de lijnen, zoals RecalcBudgetSom) + naam/groep.
            var defPerActiviteit = new Dictionary<int, (string Naam, int GroupId, decimal Bedrag)>();
            if (definitief != null)
            {
                vm.DefinitiefLabel = $"{definitief.Naam} v{definitief.Versienummer}";
                var groepen = _budgetActivityService.GetLotGroepenAsync(definitief.Id).GetAwaiter().GetResult();
                foreach (var l in groepen.SelectMany(g => g.Lijnen))
                {
                    defPerActiviteit[l.ActivityId] = defPerActiviteit.TryGetValue(l.ActivityId, out var bestaand)
                        ? (bestaand.Naam, bestaand.GroupId, bestaand.Bedrag + l.TotaalAlternatief)
                        : (l.ActivityOmschrijving ?? "", l.GroupId, l.TotaalAlternatief);
                }
            }

            var manueel = model.BudgetActivities.GroupBy(b => b.Activity.ID).ToDictionary(g => g.Key, g => g.First());
            var rijen = new List<CalculationSettingsRowV2>();
            foreach (var d in defPerActiviteit)
            {
                manueel.TryGetValue(d.Key, out var m);
                rijen.Add(new CalculationSettingsRowV2
                {
                    ActivityId = d.Key, GroupId = d.Value.GroupId, Naam = d.Value.Naam,
                    Definitief = d.Value.Bedrag, ManueelId = m?.Id ?? 0, Budget = m?.Price ?? d.Value.Bedrag,
                });
            }
            foreach (var m in manueel.Values.Where(m => !defPerActiviteit.ContainsKey(m.Activity.ID)))
            {
                rijen.Add(new CalculationSettingsRowV2
                {
                    ActivityId = m.Activity.ID, GroupId = m.Activity.Group.ID, Naam = m.Activity.Name,
                    Definitief = null, ManueelId = m.Id, Budget = m.Price,
                });
            }

            foreach (var g in model.ActivityGroups.OrderBy(x => x.Lot))
            {
                vm.Groepen.Add(new CalculationSettingsGroupV2
                {
                    GroupId = g.ID, Naam = g.Name,
                    Rijen = rijen.Where(r => r.GroupId == g.ID).OrderBy(r => r.Naam).ToList(),
                });
            }

            var aanwezig = rijen.Select(r => r.ActivityId).ToHashSet();
            vm.Opties = model.ListActivities
                .Where(a => !aanwezig.Contains(a.ID))
                .Select(a => new CalculationSettingsOptionV2 { ActivityId = a.ID, GroupId = a.GroupId ?? 0, Naam = a.Display, GroepNaam = a.Group ?? "" })
                .ToList();
            return vm;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculationSettingsV2Post(int projectid, List<CalculationSettingsPostRowV2> rows)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasWrite(PermissionCodes.ProjectsPostCalculation)) return Forbid();

            rows = (rows ?? new()).Where(r => r != null && r.ActivityId > 0).GroupBy(r => r.ActivityId).Select(g => g.First()).ToList();

            // Definitieve bedragen per activiteit opnieuw server-side bepalen: de client wordt niet vertrouwd
            // voor de vraag "wijkt dit af van het definitief budget?".
            var defPerActiviteit = new Dictionary<int, decimal>();
            var definitief = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.ProjectId == projectid && v.Status == BudgetVersie.StatusDefinitief)
                .Select(v => new { v.Id }).FirstOrDefault();
            if (definitief != null)
            {
                foreach (var l in _budgetActivityService.GetLotGroepenAsync(definitief.Id).GetAwaiter().GetResult().SelectMany(g => g.Lijnen))
                    defPerActiviteit[l.ActivityId] = (defPerActiviteit.TryGetValue(l.ActivityId, out var s) ? s : 0m) + l.TotaalAlternatief;
            }

            var bestaand = (_projectService.GetProjectBudget(projectid).Values ?? new List<BudgetActivityBO>())
                .GroupBy(b => b.Activity.ID).ToDictionary(g => g.Key, g => g.First());

            var teBewaren = new List<BudgetActivityBO>();
            var teVerwijderen = new List<int>();
            foreach (var r in rows)
            {
                var prijs = Math.Round(ParseInvariantAmount(r.Price ?? ""), 2);
                bestaand.TryGetValue(r.ActivityId, out var huidig);
                var gelijkAanDefinitief = defPerActiviteit.TryGetValue(r.ActivityId, out var def) && Math.Round(def, 2) == prijs;
                if (gelijkAanDefinitief)
                {
                    if (huidig != null) teVerwijderen.Add(huidig.Id);
                    continue;
                }
                teBewaren.Add(new BudgetActivityBO
                {
                    Id = huidig?.Id ?? 0,
                    ProjectId = projectid,
                    Activity = new ActivityBO { ID = r.ActivityId },
                    Price = prijs,
                });
            }

            foreach (var id in teVerwijderen) _uow.Budgets.DeleteObject(id);
            if (teVerwijderen.Count > 0) _uow.SaveChanges();

            var resp = teBewaren.Count > 0 ? _projectService.InsertUpdateProjectBudgetActivities(teBewaren, projectid) : null;
            if (resp == null || resp.Success)
            {
                TempData["MessageType"] = "success";
                TempData["MessageTitle"] = "Geslaagd!";
                TempData["Message"] = "Het budget is bewaard.";
            }
            else
            {
                TempData["MessageType"] = "error";
                TempData["MessageTitle"] = "Fout!";
                TempData["Message"] = "Het budget is niet bewaard.";
            }
            return RedirectToAction("Recalculation", "Projecten", new { projectid });
        }
    }
}
