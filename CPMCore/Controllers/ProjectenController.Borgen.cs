using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.GlV2;
using CPMCore.Models.Projecten;
using CPMCore.Services.Borg;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartBreadcrumbs.Attributes;
using System.Globalization;

namespace CPMCore.Controllers
{
    /// <summary>Nacalculatie › Borgen &amp; inhoudingen (design-handoff 42i/42j/42l): overzicht van de ingehouden borgen per contract,
    /// borg vrijgeven, en de extra inhoudingen op facturen afhandelen. De berekeningen zitten in <see cref="BorgService"/>.</summary>
    public partial class ProjectenController
    {
        private BorgService BorgSvc => HttpContext.RequestServices.GetRequiredService<BorgService>();

        [HttpGet]
        [Breadcrumb("Borgen", FromAction = "Recalculation")]
        public async Task<IActionResult> Borgen(int projectid, string tab = "borgen")
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var projectName = _projectService.GetProjectNameById(projectid);
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var overzicht = await BorgSvc.GetOverzichtAsync(projectid);
            var vm = new BorgenV2Vm
            {
                ProjectId = projectid, ProjectName = projectName, CanWrite = ps.HasWrite(PermissionCodes.ProjectsPostCalculation),
                Tab = tab == "inhoudingen" ? "inhoudingen" : "borgen", Overzicht = overzicht,
            };
            var be = CultureInfo.GetCultureInfo("nl-BE");
            string Geld(decimal v) => "€ " + v.ToString("N2", be);
            vm.Kpis = new List<GlV2KpiItemVm>
            {
                new() { Label = "Open borgen", Value = Geld(overzicht.TotaalOpen), IconClass = "ph-vault", Hint = $"{overzicht.AantalOpen} contract{(overzicht.AantalOpen == 1 ? "" : "en")}" },
                new() { Label = "Nu vrij te geven", Value = Geld(overzicht.TotaalNuVrij), IconClass = "ph-lock-key-open", Hint = (overzicht.VoDatum.HasValue ? $"VO {overzicht.VoDatum:dd/MM/yyyy} bereikt · " : "") + $"{overzicht.AantalNuVrij} contract{(overzicht.AantalNuVrij == 1 ? "" : "en")}", Tone = overzicht.AantalNuVrij > 0 ? GlV2KpiTone.Warning : GlV2KpiTone.Primary },
                new() { Label = "Wacht op DO", Value = Geld(overzicht.TotaalWacht), IconClass = "ph-hourglass", Hint = overzicht.DoDatum.HasValue ? $"definitieve oplevering {overzicht.DoDatum:dd/MM/yyyy}" : "datum definitieve oplevering nog niet ingevuld" },
                new() { Label = "Vrijgegeven", Value = Geld(overzicht.TotaalVrijgegeven), IconClass = "ph-check-circle", Hint = $"{overzicht.AantalVrijgaves} vrijgave{(overzicht.AantalVrijgaves == 1 ? "" : "s")} sinds start" },
            };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projectenIndex, RouteValues = new { projectid } };
            var recalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie") { Parent = projectDetail, RouteValues = new { projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Borgen", "Projecten", "Borgen") { Parent = recalc, RouteValues = new { projectid } };
            SetPageHeader("bx bx-building-house", $"{projectName} - Borgen");
            ViewData["HelpScreenId"] = "nacalculatie-borgen";
            return View("BorgenV2", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorgenVrijgeven(int projectId, List<string> sleutels)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasWrite(PermissionCodes.ProjectsPostCalculation)) return Forbid();
            var userId = User.FindFirst(CpmClaims.UserId)?.Value;
            // Per gekozen rij (sleutel "c{contractId}" of "l{leverancierId}") het te nemen bedrag; leeg = alles wat vrij te geven is.
            var keuzes = new Dictionary<string, decimal?>();
            foreach (var sleutel in (sleutels ?? new List<string>()).Distinct())
            {
                var raw = Request.Form["bedrag_" + sleutel].ToString();
                keuzes[sleutel] = string.IsNullOrWhiteSpace(raw) ? null : ParseInvariantAmount(raw);
            }
            var (aantal, totaal) = await BorgSvc.VrijgevenAsync(projectId, keuzes, userId);
            if (aantal == 0) AddMessage("error", "Er is niets vrijgegeven: kies contracten die nu vrij te geven zijn.", "Niet vrijgegeven");
            else AddMessage("success", $"Borg vrijgegeven voor {aantal} contract{(aantal == 1 ? "" : "en")}: € {totaal:N2}.", "Geslaagd!");
            return RedirectToAction(nameof(Borgen), new { projectid = projectId });
        }

        /// <summary>Een foute borgvrijgave ongedaan maken (blijft als spoor bewaard, telt niet meer mee).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorgVrijgaveIntrekken(int projectId, int id)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasWrite(PermissionCodes.ProjectsPostCalculation)) return Forbid();
            var fout = await BorgSvc.TrekVrijgaveInAsync(projectId, id, User.FindFirst(CpmClaims.UserId)?.Value);
            if (fout != null) AddMessage("error", fout, "Niet ongedaan gemaakt");
            else AddMessage("success", "De vrijgave is ongedaan gemaakt. Het bedrag staat weer als ingehouden borg.", "Geslaagd!");
            return RedirectToAction(nameof(Borgen), new { projectid = projectId });
        }

        /// <summary>Een foute afhandeling van een extra inhouding ongedaan maken.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InhoudingAfhandelingIntrekken(int projectId, int id, string? terug)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasWrite(PermissionCodes.ProjectsPostCalculation)) return Forbid();
            var fout = await BorgSvc.TrekAfhandelingInAsync(projectId, id, User.FindFirst(CpmClaims.UserId)?.Value);
            if (fout != null) AddMessage("error", fout, "Niet ongedaan gemaakt");
            else AddMessage("success", "De afhandeling is ongedaan gemaakt. Het bedrag staat weer open.", "Geslaagd!");
            if (!string.IsNullOrEmpty(terug) && Url.IsLocalUrl(terug)) return LocalRedirect(terug);
            return RedirectToAction(nameof(Borgen), new { projectid = projectId, tab = "inhoudingen" });
        }

        /// <summary>Creditnota's van deze leverancier die nog niet verrekend zijn (keuzelijst in het afhandelvenster, 42l).</summary>
        [HttpGet]
        public async Task<IActionResult> InhoudingCreditnotas(int projectId, int companyId)
        {
            var lijst = await BorgSvc.GetVrijeCreditnotasAsync(projectId, companyId);
            return Json(lijst.Select(c => new { id = c.InvoiceId, nummer = c.Nummer ?? "—", datum = c.Datum.ToString("dd/MM/yyyy"), bedrag = c.Bedrag }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InhoudingAfhandelen(int projectId, int inhoudingId, string modus, string? bedragVrij, int? creditnotaId, string? bedragVerreken, string? restActie, string? opmerking, string? terug)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            if (!ps.HasWrite(PermissionCodes.ProjectsPostCalculation)) return Forbid();
            var userId = User.FindFirst(CpmClaims.UserId)?.Value;
            var fout = await BorgSvc.AfhandelenStapAsync(inhoudingId, modus ?? "", ParseInvariantAmount(bedragVrij ?? ""), creditnotaId, ParseInvariantAmount(bedragVerreken ?? ""), restActie ?? "open", opmerking, userId);
            if (fout != null) AddMessage("error", fout, "Niet bewaard");
            else AddMessage("success", "De extra inhouding is afgehandeld.", "Geslaagd!");
            if (!string.IsNullOrEmpty(terug) && Url.IsLocalUrl(terug)) return LocalRedirect(terug);
            return RedirectToAction(nameof(Borgen), new { projectid = projectId, tab = "inhoudingen" });
        }
    }
}
