using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs;
using SmartBreadcrumbs.Nodes;

public class BreadcrumbsViewComponent : ViewComponent
{
    private readonly BreadcrumbManager _manager;
    public BreadcrumbsViewComponent(BreadcrumbManager manager) => _manager = manager;

    public IViewComponentResult Invoke(string viewName = "Default")
    {
        // Bepaal de key zoals de TagHelper doet
        var rv = ViewContext.ActionDescriptor.RouteValues;
        rv.TryGetValue("page", out var page);
        var nodeKey = !string.IsNullOrEmpty(page) ? page : $"{rv["controller"]}.{rv["action"]}";

        // Pak de laatst bekende node (of manual override uit ViewData)
        var leaf = ViewContext.ViewData["BreadcrumbNode"] as BreadcrumbNode ?? _manager.GetNode(nodeKey);

        // Zet pad naar een lijst (root -> leaf)
        var list = new List<BreadcrumbNode>();
        for (var n = leaf; n != null; n = n.Parent) list.Add(n);
        list.Reverse();

        // Opt-in: een pagina waarvan de kruimel bewust bij een OVERKOEPELENDE pagina stopt (gl-v2: de titel
        // wordt niet herhaald, bv. Projecten/Toevoegen eindigt op "Projecten") wil dat die laatste kruimel
        // toch klikbaar is — de laatste kruimel is normaal de huidige pagina en dus gewone tekst.
        ViewData["LastCrumbIsLink"] = ViewContext.ViewData["BreadcrumbLastIsLink"] as bool? == true;

        return View(viewName, list); // gebruikt /Views/Shared/Components/Breadcrumbs/Default.cshtml
    }
}
