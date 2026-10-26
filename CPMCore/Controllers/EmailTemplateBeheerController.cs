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
    public IActionResult Index(bool classic = false)
    {
        SetPageHeader("bx bx-envelope", "E-mailtemplates");
        if (classic) ViewData["UseGlV2Layout"] = false;

        var result = _emailTemplateService.GetAll(alleenActief: false);
        var vm = new EmailTemplateListVM { Templates = result.Values ?? new() };

        ViewData["BreadcrumbNode"] = BuildIndexBreadcrumb();

        // gl-v2 (punt 55f): lijst en editor op één scherm; zonder gekozen template opent de eerste.
        if (IsV2)
        {
            var eerste = vm.Templates.FirstOrDefault();
            return RenderV2(eerste == null ? null : MapBoToVm(eerste));
        }

        return View(vm);
    }

    private bool IsV2 => ViewData["UseGlV2Layout"] as bool? == true;

    // Eén scherm voor lijst en editor. edit = null: nog geen templates, of niets gekozen.
    private IActionResult RenderV2(EmailTemplateEditVM? edit)
    {
        var result = _emailTemplateService.GetAll(alleenActief: false);
        return View("IndexV2", new EmailTemplateV2Vm { Templates = result.Values ?? new(), Edit = edit, Afzender = User.Identity?.Name ?? "" });
    }

    [HttpGet]
    public IActionResult Aanmaken()
    {
        SetPageHeader("bx bx-envelope", "Nieuwe e-mailtemplate");

        var vm = new EmailTemplateEditVM { IsActief = true };

        var indexNode = BuildIndexBreadcrumb();
        ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Aanmaken", "EmailTemplateBeheer", "Nieuwe template")
        {
            Parent = indexNode
        };

        if (IsV2) return RenderV2(vm);
        return View("Bewerken", vm);
    }

    [HttpGet]
    public IActionResult Bewerken(int id)
    {
        SetPageHeader("bx bx-envelope", "E-mailtemplate bewerken");

        var result = _emailTemplateService.GetById(id);
        if (result.HasErrors || result.Value == null)
        {
            AddMessage("error", "Template niet gevonden.", "Fout");
            return RedirectToAction(nameof(Index));
        }

        var vm = MapBoToVm(result.Value);

        var indexNode = BuildIndexBreadcrumb();
        ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Bewerken", "EmailTemplateBeheer", "Template bewerken")
        {
            Parent = indexNode
        };

        if (IsV2) return RenderV2(vm);
        return View(vm);
    }

    private static SmartBreadcrumbs.Nodes.MvcBreadcrumbNode BuildIndexBreadcrumb()
    {
        var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = index
        };
        return new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "EmailTemplateBeheer", "E-mailtemplates")
        {
            Parent = instellingenIndex
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Bewerken(EmailTemplateEditVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-envelope", vm.ID == 0 ? "Nieuwe e-mailtemplate" : "E-mailtemplate bewerken");
            ViewData["BreadcrumbNode"] = BuildIndexBreadcrumb();
            if (IsV2) return RenderV2(vm);
            return View(vm);
        }

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
            ViewData["BreadcrumbNode"] = BuildIndexBreadcrumb();
            if (IsV2) return RenderV2(vm);
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
