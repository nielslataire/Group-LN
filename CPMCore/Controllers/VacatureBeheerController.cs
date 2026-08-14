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

    // ── PRIVATE HELPERS ──────────────────────────────────────────────────

    private static VacatureEditVM MapBoToVm(VacatureBO bo) => new()
    {
        ID                = bo.ID,
        Titel             = bo.Titel,
        Slug              = bo.Slug,
        Categorie         = bo.Categorie,
        Locatie           = bo.Locatie,
        Dienstverband     = bo.Dienstverband,
        KorteBeschrijving = bo.KorteBeschrijving,
        Beschrijving      = bo.Beschrijving,
        IsGepubliceerd    = bo.IsGepubliceerd,
        SortOrder         = bo.SortOrder
    };
}
