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

        return View(viewName, list); // gebruikt /Views/Shared/Components/Breadcrumbs/Default.cshtml
    }
}
