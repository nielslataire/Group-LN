using CPMCore.Models.Instellingen;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;
using System.Linq;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 55a/55b): enkel extra weergave-acties; de bestaande acties (Bewerken, Verwijderen, Blok…, Faq…) blijven ongewijzigd en worden door de V2-views gebruikt.
// ?classic=true houdt de klassieke pagina's bereikbaar.
public partial class BlogBeheerController
{
    private bool IsV2 => ViewData["UseGlV2Layout"] as bool? == true;

    // Dashboard › Instellingen › Blogartikelen
    private MvcBreadcrumbNode BlogNode()
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        return new MvcBreadcrumbNode("Index", "BlogBeheer", "Blogartikelen") { Parent = instellingen };
    }

    private IActionResult IndexV2(BlogArtikelListVM vm)
    {
        ViewData["BreadcrumbNode"] = BlogNode();
        return View("IndexV2", vm);
    }

    // Na een mislukte POST zijn de blokken en vragen niet mee gepost: voor een bestaand artikel opnieuw laden, zodat ze niet verdwijnen.
    private BlogArtikelEditVM HervulLijsten(BlogArtikelEditVM vm)
    {
        if (vm.ID == 0) return vm;
        var bo = _blogService.GetArtikelById(vm.ID).Value;
        if (bo == null) return vm;
        var geladen = MapBoToVm(bo);
        vm.Blokken = geladen.Blokken;
        vm.FaqItems = geladen.FaqItems;
        return vm;
    }

    private IActionResult BewerkenV2(BlogArtikelEditVM edit)
    {
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Bewerken", "BlogBeheer", edit.ID == 0 ? "Nieuw artikel" : "Bewerken") { Parent = BlogNode() };
        return View("BewerkenV2", new BlogEditV2Vm { Edit = edit, PreviewUrl = ViewBag.PreviewUrl as string });
    }
}
