using BOCore;
using CPMCore.Models.Instellingen;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsVacatureBeheer)]
public class VacatureBeheerController : BaseController
{
    private readonly IVacatureService _vacatureService;

    public VacatureBeheerController(IVacatureService vacatureService)
    {
        _vacatureService = vacatureService;
    }

    // ── LIST ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Index()
    {
        SetPageHeader("bx bx-briefcase", "Vacatures");

        var result = _vacatureService.GetVacatures(alleenGepubliceerd: false);
        var vm = new VacatureListVM { Vacatures = result.Values ?? new() };
        return View(vm);
    }

    // ── CREATE ──────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Aanmaken()
    {
        SetPageHeader("bx bx-briefcase", "Nieuwe vacature");

        var vm = new VacatureEditVM();
        return View("Bewerken", vm);
    }

    // ── EDIT ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Bewerken(int id)
    {
        SetPageHeader("bx bx-briefcase", "Vacature bewerken");

        var result = _vacatureService.GetVacatureById(id);
        if (result.HasErrors || result.Value == null)
        {
            AddMessage("error", "Vacature niet gevonden.", "Fout");
            return RedirectToAction(nameof(Index));
        }

        var vm = MapBoToVm(result.Value);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Bewerken(VacatureEditVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-briefcase", vm.ID == 0 ? "Nieuwe vacature" : "Vacature bewerken");
            return View(vm);
        }

        var bo = new VacatureBO
        {
            ID                = vm.ID,
            Titel             = vm.Titel,
            Slug              = vm.Slug,
            Categorie         = vm.Categorie,
            Locatie           = vm.Locatie,
            Dienstverband     = vm.Dienstverband,
            Opleiding         = vm.Opleiding,
            Start             = vm.Start,
            KorteBeschrijving = vm.KorteBeschrijving,
            Beschrijving      = vm.Beschrijving,
            IsGepubliceerd    = vm.IsGepubliceerd,
            SortOrder         = vm.SortOrder
        };

        var response = _vacatureService.InsertUpdate(bo);

        if (response.HasErrors)
        {
            foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                ModelState.AddModelError(string.Empty, msg.Message);
            SetPageHeader("bx bx-briefcase", vm.ID == 0 ? "Nieuwe vacature" : "Vacature bewerken");
            return View(vm);
        }

        AddMessage("success", "Vacature opgeslagen.", "Opgeslagen");
        var vacatureId = vm.ID != 0 ? vm.ID : response.InsertedId;
        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── DELETE ──────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Verwijderen(int id)
    {
        var response = _vacatureService.DeleteVacature(id);

        if (response.HasErrors)
            AddMessage("error", "Vacature kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Vacature verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Index));
    }

    // ── TAKENPAKKET ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaakOpslaan(VacatureTaakVM vm)
    {
        var bo = new VacatureTaakBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateTaak(bo);

        if (response.HasErrors)
            AddMessage("error", "Taak kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Taak opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaakVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateTakenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaakVerwijderen(int taakId, int vacatureId)
    {
        var response = _vacatureService.DeleteTaak(taakId);

        if (response.HasErrors)
            AddMessage("error", "Taak kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Taak verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── WIE ZOEKEN WE (must-have / mooi meegenomen) ─────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VereisteOpslaan(VacatureVereisteVM vm)
    {
        var bo = new VacatureVereisteBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Categorie = vm.Categorie, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateVereiste(bo);

        if (response.HasErrors)
            AddMessage("error", "Vereiste kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Vereiste opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VereisteVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateVereistenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VereisteVerwijderen(int vereisteId, int vacatureId)
    {
        var response = _vacatureService.DeleteVereiste(vereisteId);

        if (response.HasErrors)
            AddMessage("error", "Vereiste kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Vereiste verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── WAT BIEDEN WE ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoordeelOpslaan(VacatureVoordeelVM vm)
    {
        var bo = new VacatureVoordeelBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateVoordeel(bo);

        if (response.HasErrors)
            AddMessage("error", "Voordeel kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Voordeel opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoordeelVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateVoordelenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoordeelVerwijderen(int voordeelId, int vacatureId)
    {
        var response = _vacatureService.DeleteVoordeel(voordeelId);

        if (response.HasErrors)
            AddMessage("error", "Voordeel kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Voordeel verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── STAPPENLIJST SOLLICITATIE ────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StapOpslaan(VacatureSollicitatieStapVM vm)
    {
        var bo = new VacatureSollicitatieStapBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Titel = vm.Titel, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateSollicitatieStap(bo);

        if (response.HasErrors)
            AddMessage("error", "Stap kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Stap opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StapVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateSollicitatieStappenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StapVerwijderen(int stapId, int vacatureId)
    {
        var response = _vacatureService.DeleteSollicitatieStap(stapId);

        if (response.HasErrors)
            AddMessage("error", "Stap kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Stap verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── PRIVATE HELPERS ──────────────────────────────────────────────────

    private static VacatureEditVM MapBoToVm(VacatureBO bo) => new()
    {
        ID                = bo.ID,
        Titel             = bo.Titel,
        Slug              = bo.Slug,
        Categorie         = bo.Categorie,
        Locatie           = bo.Locatie,
        Dienstverband     = bo.Dienstverband,
        Opleiding         = bo.Opleiding,
        Start             = bo.Start,
        KorteBeschrijving = bo.KorteBeschrijving,
        Beschrijving      = bo.Beschrijving,
        IsGepubliceerd    = bo.IsGepubliceerd,
        SortOrder         = bo.SortOrder,
        TaakItems              = bo.TaakItems,
        VereisteItems          = bo.VereisteItems,
        VoordeelItems          = bo.VoordeelItems,
        SollicitatieStapItems  = bo.SollicitatieStapItems
    };
}
