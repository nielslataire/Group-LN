using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOCore;
using CPMCore.Models.Budget;
using CPMCore.Models.GlV2;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CPMCore.Controllers
{
    /// <summary>Budgetflow in gl-v2 (design-handoff punt 39): gedeelde wizardschil + de statusacties Afronden / Definitief maken / Ontgrendelen.
    /// Zelfde partial class als de rest van de budgetwizard; de legacy-acties blijven bestaan en vertakken per pagina naar een V2-view.</summary>
    public partial class ProjectenController
    {
        private static readonly (string Label, string Titel, string Action)[] WizardStappen =
        {
            ("Gegevens",      "Projectgegevens",  nameof(BudgetGegevens)),
            ("Oppervlaktes",  "Oppervlaktes",     nameof(BudgetOppervlaktes)),
            ("Sanitair",      "Sanitair",         nameof(BudgetSanitair)),
            ("Gevels",        "Gevels en ramen",  nameof(BudgetGevels)),
            ("Dak & afbraak", "Dak & afbraak",    nameof(BudgetDakAfbraak)),
            ("Activiteiten",  "Activiteiten",     nameof(BudgetActivityLijnen)),
            ("Parameters",    "Parameters",       nameof(BudgetParams)),
            ("Verkoop",       "Verkoop",          nameof(BudgetVerkoop)),
            ("Resultaat",     "Resultaat",        nameof(BudgetResultaat)),
        };

        private bool GebruikGlV2 => ViewData["UseGlV2Layout"] as bool? == true;

        /// <summary>Bouwt de gedeelde schil van een wizardpagina en registreert de bereikte stap. <paramref name="extra"/> = aandachtspunten die
        /// enkel de pagina zelf kent (bv. vraagprijzen onder het minimum op stap 8) en de standaardberekening overschrijven.</summary>
        private async Task<BudgetWizardChromeVm> PrepareWizardV2(int versieId, int step, IDictionary<int, (int Errors, int Warnings)> extra = null, bool registreer = true)
        {
            // Koppelt de pagina aan de helpartikels (Helpcentrum, "?"-paneel): budget-stap-1 … budget-stap-9.
            ViewData["HelpScreenId"] = "budget-stap-" + step;
            var versie = await _uow.BudgetVersies.GetNoTracking().Include(v => v.BudgetMaster).FirstAsync(v => v.Id == versieId);
            var projectNaam = _projectService.GetProjectNameById(versie.ProjectId) ?? "";

            if (registreer && versie.Status != BudgetVersie.StatusDefinitief)
            {
                _budgetService.RegistreerStap(versieId, step);
                versie.LaatsteStap = (byte)Math.Max(versie.LaatsteStap ?? 0, step);
            }

            var meldingen = await GetMeldingenAsync(versie);
            var waarsch = meldingen.Where(m => m.IsOpen).GroupBy(m => m.Stap).ToDictionary(x => x.Key, x => (Errors: x.Count(m => m.Type == ServiceCore.Budget.MeldingType.Fout), Warnings: x.Count(m => m.Type == ServiceCore.Budget.MeldingType.Waarschuwing)));
            if (extra != null) foreach (var kv in extra) waarsch[kv.Key] = kv.Value;

            var bereikt = versie.LaatsteStap ?? 0;
            var stappen = new List<GlV2StepItemVm>();
            for (int i = 1; i <= WizardStappen.Length; i++)
            {
                waarsch.TryGetValue(i, out var w);
                // Wel bezochte stappen na de huidige blijven "afgewerkt"; hebben ze een aandachtspunt, dan laten we de resolver ze goud kleuren.
                var state = (i > step && i <= bereikt && w.Errors == 0 && w.Warnings == 0) ? GlV2StepState.Done : GlV2StepState.Auto;
                stappen.Add(new GlV2StepItemVm
                {
                    Label = WizardStappen[i - 1].Label,
                    State = state,
                    Errors = w.Errors,
                    Warnings = w.Warnings,
                    Href = Url.Action(WizardStappen[i - 1].Action, "Projecten", new { versieId })
                });
            }

            var chrome = new BudgetWizardChromeVm
            {
                VersieId = versieId, MasterId = versie.BudgetMasterId, ProjectId = versie.ProjectId, ProjectName = projectNaam,
                MasterNaam = versie.BudgetMaster?.Naam ?? "", Versienummer = versie.Versienummer, VersieNaam = versie.VersieNaam ?? "",
                Status = versie.Status ?? "Concept", IsHuidig = versie.IsHuidig, VastgezetOp = versie.VastgezetOp, VastgezetDoor = versie.VastgezetDoor ?? "",
                Step = step, StepLabel = WizardStappen[step - 1].Label, StepTitle = WizardStappen[step - 1].Titel,
                Steps = stappen,
                OverviewUrl = Url.Action(nameof(BudgetIndex), "Projecten", new { projectId = versie.ProjectId }) ?? "#",
                AantalWaarschuwingen = waarsch.Values.Sum(v => v.Errors + v.Warnings),
                Meldingen = meldingen,
                Menu = new GlV2ProjectMenuVm { ProjectId = versie.ProjectId, ProjectName = projectNaam, ProjectSubtitle = "project · budget", Mode = GlV2ProjectMenuMode.Outer }
            };

            if (step == 1) { chrome.PrevUrl = chrome.OverviewUrl; chrome.PrevLabel = "Terug naar overzicht"; }
            else { chrome.PrevUrl = Url.Action(WizardStappen[step - 2].Action, "Projecten", new { versieId }) ?? "#"; chrome.PrevLabel = "Vorige: " + WizardStappen[step - 2].Label; }
            if (step < WizardStappen.Length)
            {
                chrome.NextUrl = Url.Action(WizardStappen[step].Action, "Projecten", new { versieId }) ?? "#";
                chrome.NextLabel = (step is 7 or 8 ? "Opslaan & volgende: " : "Volgende: ") + WizardStappen[step].Label;
                chrome.NextSaves = step is 7 or 8;
            }

            ViewData["Chrome"] = chrome;
            return chrome;
        }

        /// <summary>Alle meldingen van de versie (39k). Gecached zolang de versie niet wijzigt (de filter zet bij elke schrijvende actie GewijzigdOp);
        /// ook de genegeerde waarschuwingen zitten in de sleutel, zodat "Negeren" meteen doorwerkt.</summary>
        private async Task<List<ServiceCore.Budget.BudgetMelding>> GetMeldingenAsync(BudgetVersie versie)
        {
            var cache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var vers = await _db.BudgetVersie.AsNoTracking().Where(v => v.Id == versie.Id).Select(v => new { v.GewijzigdOp, v.WaarschuwingenBevestigd, v.Status }).FirstAsync();
            var key = $"bwm:{versie.Id}:{vers.GewijzigdOp?.Ticks ?? 0}:{vers.WaarschuwingenBevestigd}:{vers.Status}";
            if (cache.TryGetValue(key, out List<ServiceCore.Budget.BudgetMelding> bestaand)) return bestaand;
            var service = HttpContext.RequestServices.GetRequiredService<ServiceCore.Budget.BudgetControleService>();
            var nieuw = await service.BerekenAsync(versie.Id);
            cache.Set(key, nieuw, TimeSpan.FromMinutes(10));
            return nieuw;
        }

        // ── Statusacties ──────────────────────────────────────────────────────

        private IActionResult StatusResultaat(bool ok, string melding, int versieId, string terugActie)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers["Accept"].ToString()).Contains("json");
            if (isAjax) return Json(new { success = ok, message = melding });
            TempData[ok ? "Message" : "Error"] = melding;
            return RedirectToAction(terugActie, new { versieId });
        }

        /// <summary>39k: fouten blokkeren "Afronden" (waarschuwingen niet). Geeft de melding terug als er open fouten zijn.</summary>
        private async Task<string> FoutenBlokkerenAsync(int versieId)
        {
            var versie = await _db.BudgetVersie.AsNoTracking().FirstOrDefaultAsync(v => v.Id == versieId);
            if (versie == null) return null;
            var fouten = (await GetMeldingenAsync(versie)).Where(m => m.IsOpen && m.Type == ServiceCore.Budget.MeldingType.Fout).ToList();
            if (fouten.Count == 0) return null;
            return $"Afronden kan niet: er {(fouten.Count == 1 ? "staat nog 1 fout" : $"staan nog {fouten.Count} fouten")} open (stap {string.Join(", ", fouten.Select(f => f.Stap).Distinct().OrderBy(x => x))}). Los ze op en probeer opnieuw.";
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetVersieAfronden(int versieId)
        {
            var blok = await FoutenBlokkerenAsync(versieId); if (blok != null) return StatusResultaat(false, blok, versieId, nameof(BudgetResultaat));
            var r = _budgetService.AfrondenVersie(versieId);
            return StatusResultaat(r.Success, r.Messages.FirstOrDefault()?.Message ?? "", versieId, nameof(BudgetResultaat));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetVersieDefinitief(int versieId)
        {
            var blok = await FoutenBlokkerenAsync(versieId); if (blok != null) return StatusResultaat(false, blok, versieId, nameof(BudgetResultaat));
            var r = _budgetService.MaakDefinitief(versieId, User?.Identity?.Name);
            return StatusResultaat(r.Success, r.Messages.FirstOrDefault()?.Message ?? "", versieId, nameof(BudgetResultaat));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult BudgetVersieOntgrendelen(int versieId)
        {
            var r = _budgetService.OntgrendelDefinitief(versieId);
            return StatusResultaat(r.Success, r.Messages.FirstOrDefault()?.Message ?? "", versieId, nameof(BudgetResultaat));
        }

        /// <summary>Een aandachtspunt bevestigen ("decennale elders gedekt") of weer intrekken.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult BudgetWaarschuwingBevestigen(int versieId, string sleutel, bool bevestigd = true)
        {
            var r = _budgetService.BevestigWaarschuwing(versieId, sleutel, bevestigd, User?.Identity?.Name);
            return Json(new { success = r.Success });
        }

        /// <summary>Autosave van stap 7 (zelfde velden en omrekening als de POST BudgetParams, maar JSON terug i.p.v. een redirect).</summary>
        [HttpPost]
        public async Task<IActionResult> BudgetParamsOpslaan(CPMCore.Models.Budget.BudgetParamsModel model,
            decimal? pctProjectcoord, decimal? pctArchitect, decimal? pctIngenieur, decimal? pctDoelmarge, decimal? pctGrondmarge,
            decimal? pctVeiligheid, decimal? pctDecennale, decimal? pctABR, decimal? pctOnvoorzien, decimal? pctOpmeting, decimal? pctSondering, decimal? pctRegistratie, decimal? pctPubliciteit, decimal? pctMakelaar, decimal? pctAgrarisch, decimal? pctWetBreyne, decimal? pctSlGebouw, decimal? pctSlGrond)
        {
            try
            {
                await BudgetParams(model, pctProjectcoord, pctArchitect, pctIngenieur, pctDoelmarge, pctGrondmarge, pctVeiligheid, pctDecennale, pctABR, pctOnvoorzien, pctOpmeting, pctSondering, pctRegistratie, pctPubliciteit, pctMakelaar, pctAgrarisch, pctWetBreyne, pctSlGebouw, pctSlGrond);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        /// <summary>Hernoemt een budget (naam + omschrijving) vanuit het overzicht (39a, potlood in de kaartkop).</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetMasterHernoemen(int masterId, int projectId, string naam, string omschrijving)
        {
            if (string.IsNullOrWhiteSpace(naam)) { TempData["Error"] = "De naam van het budget is verplicht."; return RedirectToAction(nameof(BudgetIndex), new { projectId }); }
            var m = await _db.BudgetMaster.FirstOrDefaultAsync(x => x.Id == masterId && x.ProjectId == projectId);
            if (m == null) return NotFound();
            m.Naam = naam.Trim().Length > 200 ? naam.Trim()[..200] : naam.Trim();
            m.Omschrijving = string.IsNullOrWhiteSpace(omschrijving) ? null : omschrijving.Trim();
            await _db.SaveChangesAsync();
            TempData["Message"] = "Budget hernoemd.";
            return RedirectToAction(nameof(BudgetIndex), new { projectId });
        }

        /// <summary>Geeft een versie een (andere) naam en notitie; werkt ook op een definitieve versie (dat is metadata, geen budgetinhoud).</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetVersieHernoemen(int versieId, int projectId, string versieNaam, string notitie)
        {
            var v = await _db.BudgetVersie.FirstOrDefaultAsync(x => x.Id == versieId && x.ProjectId == projectId);
            if (v == null) return NotFound();
            v.VersieNaam = string.IsNullOrWhiteSpace(versieNaam) ? null : versieNaam.Trim();
            v.Notitie = string.IsNullOrWhiteSpace(notitie) ? null : notitie.Trim();
            await _db.SaveChangesAsync();
            TempData["Message"] = "Versie bijgewerkt.";
            return RedirectToAction(nameof(BudgetIndex), new { projectId });
        }

        /// <summary>Verwijdert een budgetversie die niet definitief is (dat vraagt het verwijderrecht op projecten; de naam bevat "Delete" zodat de permissiefilter dat afdwingt).
        /// Is het de laatste versie van het budget, dan verdwijnt het budget mee. Kindrijen (oppervlaktes, activiteiten, verkoop…) gaan mee via de cascade in de database.
        /// Was het het actieve budget of de huidige versie, dan neemt een ander budget/versie die rol over.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetVersieDelete(int versieId, int projectId)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<FacadeCore.IPermissionService>();
            if (!ps.HasDelete(PermissionCodes.ProjectsDetail)) { TempData["Error"] = "U hebt geen rechten om een budget te verwijderen."; return RedirectToAction(nameof(BudgetIndex), new { projectId }); }
            var v = await _db.BudgetVersie.FirstOrDefaultAsync(x => x.Id == versieId && x.ProjectId == projectId);
            if (v == null) return NotFound();
            if (v.Status == "Definitief") { TempData["Error"] = "Een definitief budget kan niet verwijderd worden. Maak eerst een andere versie definitief."; return RedirectToAction(nameof(BudgetIndex), new { projectId }); }

            var masterId = v.BudgetMasterId;
            var master = await _db.BudgetMaster.FirstOrDefaultAsync(m => m.Id == masterId);
            var rest = await _db.BudgetVersie.Where(x => x.BudgetMasterId == masterId && x.Id != versieId).OrderByDescending(x => x.Versienummer).ToListAsync();
            var wasHuidig = v.IsHuidig;
            var wasActief = master?.IsActief == true;

            // De verwijderknop mag niet blijven hangen aan een verweesd budget: de laatste versie neemt het budget mee.
            if (rest.Count == 0 && master != null) _db.BudgetMaster.Remove(master);
            else
            {
                _db.BudgetVersie.Remove(v);
                if (wasHuidig && rest.Count > 0 && !rest.Any(x => x.IsHuidig)) rest[0].IsHuidig = true;
            }
            await _db.SaveChangesAsync();

            if (rest.Count == 0 && wasActief)
            {
                var volgende = await _db.BudgetMaster.Where(m => m.ProjectId == projectId).OrderByDescending(m => m.Id).FirstOrDefaultAsync();
                if (volgende != null) { volgende.IsActief = true; await _db.SaveChangesAsync(); }
            }
            TempData["Message"] = rest.Count == 0 ? "Het budget is verwijderd." : "De versie is verwijderd.";
            return RedirectToAction(nameof(BudgetIndex), new { projectId });
        }

        /// <summary>39a "Maak actief": één actief budget per project.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetMasterActiveren(int masterId, int projectId)
        {
            foreach (var m in await _db.BudgetMaster.Where(x => x.ProjectId == projectId).ToListAsync()) m.IsActief = m.Id == masterId;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Actief budget gewijzigd.";
            return RedirectToAction(nameof(BudgetIndex), new { projectId });
        }

        /// <summary>De versies voor de vergelijking: maximaal 3, enkel van dit project, de basis eerst.</summary>
        private async Task<List<int>> LeesVergelijkIdsAsync(int projectId, string versies, int? basis)
        {
            var ids = (versies ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0).Distinct().ToList();
            if (basis is > 0 && ids.Contains(basis.Value)) { ids.Remove(basis.Value); ids.Insert(0, basis.Value); }
            ids = ids.Take(3).ToList();
            var geldig = await _db.BudgetVersie.AsNoTracking().Where(v => v.ProjectId == projectId && ids.Contains(v.Id)).Select(v => v.Id).ToListAsync();
            return ids.Where(geldig.Contains).ToList();
        }

        /// <summary>Budgetten vergelijken (design-handoff 39n): 2 of 3 versies, de eerste is de basis (standaard het definitieve budget).</summary>
        [HttpGet]
        public async Task<IActionResult> BudgetVersiesVergelijken(int projectId, string versies, int? basis, string modus = "alles")
        {
            var projectNaam = _projectService.GetProjectNameById(projectId);
            if (string.IsNullOrEmpty(projectNaam)) return NotFound();
            var ids = await LeesVergelijkIdsAsync(projectId, versies, basis);
            if (!GebruikGlV2 || ids.Count < 2)
            {
                if (ids.Count < 2) TempData["Error"] = "Kies minstens twee versies om te vergelijken.";
                return RedirectToAction(nameof(BudgetIndex), new { projectId });
            }

            var service = HttpContext.RequestServices.GetRequiredService<ServiceCore.Budget.BudgetVergelijkService>();
            var vergelijking = await service.VergelijkAsync(ids, string.Equals(modus, "kosten", StringComparison.OrdinalIgnoreCase));
            var mastersResponse = _budgetService.GetBudgetMasters(projectId);
            var index = new CPMCore.Models.Projecten.BudgetIndexModel { ProjectId = projectId, ProjectName = projectNaam, BudgetMasters = mastersResponse.Success ? mastersResponse.Values : new List<BudgetMasterBO>() };
            await VulBudgetIndexV2Async(index);

            ViewData["HelpScreenId"] = "budget-vergelijken";
            SetBudgetPageContext(projectId, projectNaam, nameof(BudgetVersiesVergelijken), "Budgetten vergelijken", new { projectId, versies, basis, modus });
            return View("BudgetVergelijkV2", new CPMCore.Models.Projecten.BudgetVergelijkPaginaModel
            {
                ProjectId = projectId, ProjectName = projectNaam, Vergelijking = vergelijking, Index = index, Modus = vergelijking.EnkelKosten ? "kosten" : "alles", VersieIds = ids
            });
        }

        /// <summary>Knop "Exporteren" op de vergelijkingspagina: Excel met kerncijfers, hoofdgroepen met activiteiten, verkoop per eenheid en parameters.</summary>
        [HttpGet]
        public async Task<IActionResult> BudgetVergelijkExport(int projectId, string versies, int? basis, string modus = "alles")
        {
            var projectNaam = _projectService.GetProjectNameById(projectId);
            if (string.IsNullOrEmpty(projectNaam)) return NotFound();
            var ids = await LeesVergelijkIdsAsync(projectId, versies, basis);
            if (ids.Count < 2) return RedirectToAction(nameof(BudgetIndex), new { projectId });
            var service = HttpContext.RequestServices.GetRequiredService<ServiceCore.Budget.BudgetVergelijkService>();
            var vergelijking = await service.VergelijkAsync(ids, string.Equals(modus, "kosten", StringComparison.OrdinalIgnoreCase));
            var bytes = ServiceCore.Budget.BudgetVergelijkService.NaarExcel(vergelijking, projectNaam);
            var naam = $"Budgetvergelijking_{projectNaam}_{DateTime.Now:yyyyMMdd}.xlsx";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) naam = naam.Replace(c, '_');
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", naam.Replace(' ', '_'));
        }

        /// <summary>Vult de gl-v2-overzichtsgegevens (39a): definitieve versie, totalen, eenheden per budget, kopieeropties.</summary>
        private async Task VulBudgetIndexV2Async(CPMCore.Models.Projecten.BudgetIndexModel model)
        {
            var versies = model.BudgetMasters.SelectMany(m => m.Versies).ToList();
            model.DefinitiefVersie = versies.FirstOrDefault(v => v.Status == BudgetVersie.StatusDefinitief);
            // Totalen komen uit de bewaarde kolom; ontbreekt/veroudert die (filter zet ze op NULL bij elke wijziging), dan lui herberekenen en bewaren.
            var bewaard = await _db.BudgetVersie.AsNoTracking().Where(x => x.ProjectId == model.ProjectId).Select(x => new { x.Id, x.TotaalKosten }).ToDictionaryAsync(x => x.Id, x => x.TotaalKosten);
            foreach (var v in versies.Where(v => v.Status != BudgetVersie.StatusConcept || v.IsHuidig || v.LaatsteStap >= 9))
            {
                if (bewaard.TryGetValue(v.Id, out var t) && t.HasValue) { model.Totalen[v.Id] = t; continue; }
                try
                {
                    var res = await _berekeningService.BerekenAsync(v.Id);
                    model.Totalen[v.Id] = res.TotaalKosten;
                    var tot = Math.Round(res.TotaalKosten, 2);
                    await _db.BudgetVersie.Where(x => x.Id == v.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.TotaalKosten, (decimal?)tot));
                }
                catch { model.Totalen[v.Id] = null; }
            }
            // Marge (39a): enkel voor versies waarvan alle stappen doorlopen zijn (afgerond, definitief of concept dat stap 9 bereikte). Zwaarder dan het totaal
            // (voorstel + verkooplijnen), dus bewaard in het geheugen per versie en tijdstip van de laatste wijziging.
            var cache = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            foreach (var v in versies.Where(v => v.Status == BudgetVersie.StatusDefinitief || v.Status == BudgetVersie.StatusAfgerond || v.LaatsteStap >= 9))
            {
                var sleutel = $"bw-ix-marge2:{v.Id}:{(v.GewijzigdOp ?? v.CreatedAt).Ticks}:{v.Status}";
                if (!cache.TryGetValue(sleutel, out CPMCore.Models.Projecten.BudgetMargeVm marge))
                {
                    try
                    {
                        var res = await _berekeningService.BerekenAsync(v.Id);
                        var p = await _berekeningService.GetOrCreateParamsAsync(v.Id);
                        var voorstel = await _verkoopVoorstelService.BerekenAsync(v.Id);
                        var sam = await _verkoopVoorstelService.SamenvattingAsync(v.Id);
                        var lijnen = await _verkoopVoorstelService.HerberekendeLijnenAsync(v.Id);
                        var vr = ServiceCore.Budget.VerkoopResultaat.Bereken(sam, voorstel, lijnen, p.MakelaarsCommissiePerc ?? 0m, p.OverigeVerkoopkosten ?? 0m, res.TotaalKosten);
                        marge = vr.HeeftVerkoop ? new CPMCore.Models.Projecten.BudgetMargeVm { Percentage = vr.MargeOpKostprijs, Bedrag = vr.Resultaat } : null;
                    }
                    catch { marge = null; }
                    cache.Set(sleutel, marge, TimeSpan.FromHours(12));
                }
                if (marge != null) model.Marges[v.Id] = marge;
            }
            foreach (var m in model.BudgetMasters)
            {
                var huidig = m.Versies.FirstOrDefault(v => v.IsHuidig) ?? m.Versies.OrderByDescending(v => v.Versienummer).FirstOrDefault();
                model.EenhedenPerMaster[m.Id] = huidig == null ? 0 : await _uow.BudgetOppervlaktes.GetNoTracking().CountAsync(o => o.BudgetVersieId == huidig.Id);
            }
            model.KopieerOpties = await _uow.BudgetVersies.GetNoTracking().Include(v => v.BudgetMaster).ThenInclude(m => m.Project)
                .Where(v => !v.BudgetMaster.IsGearchiveerd).OrderByDescending(v => v.ProjectId == model.ProjectId).ThenBy(v => v.BudgetMaster.Project.ProjectName).ThenBy(v => v.BudgetMaster.Naam).ThenByDescending(v => v.Versienummer)
                .Select(v => new IdNameBO { ID = v.Id, Display = "v" + v.Versienummer + (v.VersieNaam == null || v.VersieNaam == "" ? "" : " · " + v.VersieNaam) + " (" + v.Status + ")", Group = v.BudgetMaster.Project.ProjectName + " · " + v.BudgetMaster.Naam })
                .ToListAsync();

            // 39l: budgetten (met versies) waarvan een nieuw budget een kopie kan zijn, gegroepeerd per project; dit project eerst
            var vs = await _uow.BudgetVersies.GetNoTracking().Include(v => v.BudgetMaster).ThenInclude(m => m.Project)
                .Where(v => !v.BudgetMaster.IsGearchiveerd).OrderBy(v => v.BudgetMaster.Project.ProjectName).ThenBy(v => v.BudgetMaster.Naam).ThenByDescending(v => v.Versienummer).ToListAsync();
            model.KopieBronnen = vs.GroupBy(v => v.BudgetMasterId).Select(g => new CPMCore.Models.Projecten.BudgetKopieBronVm
            {
                MasterId = g.Key, Naam = g.First().BudgetMaster.Naam, ProjectNaam = g.First().BudgetMaster.Project?.ProjectName ?? "", EigenProject = g.First().ProjectId == model.ProjectId,
                Versies = g.Select(v => new IdNameBO { ID = v.Id, Display = "v" + v.Versienummer + (string.IsNullOrWhiteSpace(v.VersieNaam) ? "" : " " + v.VersieNaam) + " · " + (v.Status ?? "Concept") }).ToList()
            }).OrderByDescending(b => b.EigenProject).ThenBy(b => b.ProjectNaam).ThenBy(b => b.Naam).ToList();
            model.AantalProjectEenheden = await _uow.Units.GetNoTracking().CountAsync(u => u.ProjectId == model.ProjectId && !u.IsLink);
            try
            {
                var sRijen = _uow.BouwIndex.GetNoTracking().Where(x => x.IndexType == "S").ToList();
                var sNu = await _bouwIndex.GetActieveIndexAsync("S");
                var rij = ServiceCore.Budget.BudgetControleService.ZoekIndexRij(sRijen, sNu);
                var peil = rij == null ? null : (rij.GeldigVanaf?.ToString("MM/yyyy") ?? (rij.Jaar.HasValue ? (rij.Maand.HasValue ? $"{rij.Maand:00}/{rij.Jaar}" : rij.Jaar.ToString()) : null));
                model.ActieveIndexTekst = peil == null ? "Nieuwste S- en I-index" : "S- en I-index " + peil;
            }
            catch { model.ActieveIndexTekst = "Nieuwste S- en I-index"; }
        }

        /// <summary>Wijzigt de reductiefactoren van stap 2 voor deze versie (design-handoff 39c "Reductiefactoren VMSW").</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult BudgetVmswFactorenOpslaan(int versieId, bool herstel, Dictionary<string, string> factoren)
        {
            var f = BOCore.Budget.VmswFactorenBO.Standaard;
            if (!herstel && factoren != null)
            {
                decimal Lees(string k, decimal def)
                {
                    if (!factoren.TryGetValue(k, out var raw) || string.IsNullOrWhiteSpace(raw)) return def;
                    return decimal.TryParse(raw.Replace(" ", "").Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : def;
                }
                f.Bewoonbaar = Lees("bewoonbaar", f.Bewoonbaar); f.Tuin = Lees("tuin", f.Tuin); f.TerrasPrefab = Lees("terrasPrefab", f.TerrasPrefab);
                f.TerrasGelijkvloers = Lees("terrasGelijkvloers", f.TerrasGelijkvloers); f.Dakterras = Lees("dakterras", f.Dakterras);
                f.GaragesBovengronds = Lees("garagesBovengronds", f.GaragesBovengronds); f.GarBergOndergronds = Lees("garBergOndergronds", f.GarBergOndergronds);
                f.BergGelijkvloers = Lees("bergGelijkvloers", f.BergGelijkvloers); f.Carports = Lees("carports", f.Carports); f.DoorritGvl = Lees("doorritGvl", f.DoorritGvl);
                f.Zolder = Lees("zolder", f.Zolder); f.GemeenschappelijkeDelen = Lees("gemeenschappelijkeDelen", f.GemeenschappelijkeDelen); f.Wegenis = Lees("wegenis", f.Wegenis);
            }
            var r = _budgetService.SetVmswFactoren(versieId, f);
            return Json(new { success = r.Success, message = r.Messages.FirstOrDefault()?.Message, aangepast = f.IsAangepast });
        }
    }
}
