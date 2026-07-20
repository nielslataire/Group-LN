using BOCore;
using CPMCore.Models.Instellingen;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsEmailTemplates)]
public class EmailTemplateBeheerController : BaseController
{
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailTemplateBeheerController(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var result = _emailTemplateService.GetAll(alleenActief: false);
        var vm = new EmailTemplateListVM { Templates = result.Values ?? new() };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Aanmaken()
    {
        var vm = new EmailTemplateEditVM { IsActief = true };
        return View("Bewerken", vm);
    }

    [HttpGet]
    public IActionResult Bewerken(int id)
    {
        var result = _emailTemplateService.GetById(id);
        if (result.HasErrors || result.Value == null)
        {
            AddMessage("error", "Template niet gevonden.", "Fout");
            return RedirectToAction(nameof(Index));
        }

        var vm = MapBoToVm(result.Value);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Bewerken(EmailTemplateEditVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var bo = new EmailTemplateBO
        {
            ID = vm.ID,
            Naam = vm.Naam,
            Onderwerp = vm.Onderwerp,
            BodyHtml = vm.BodyHtml,
            IsActief = vm.IsActief
        };

        var response = _emailTemplateService.InsertUpdate(bo);

        if (response.HasErrors)
        {
            foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                ModelState.AddModelError(string.Empty, msg.Message);
            return View(vm);
        }

        AddMessage("success", "Template opgeslagen.", "Opgeslagen");
        var templateId = vm.ID != 0 ? vm.ID : response.InsertedId;
        return RedirectToAction(nameof(Bewerken), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Verwijderen(int id)
    {
        var response = _emailTemplateService.Delete(id);

        if (response.HasErrors)
            AddMessage("error", "Template kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Template verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Index));
    }

    private static EmailTemplateEditVM MapBoToVm(EmailTemplateBO bo) => new()
    {
        ID = bo.ID,
        Naam = bo.Naam,
        Onderwerp = bo.Onderwerp,
        BodyHtml = bo.BodyHtml,
        IsActief = bo.IsActief
    };
}
