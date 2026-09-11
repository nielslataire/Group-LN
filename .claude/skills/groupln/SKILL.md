---
name: "groupln"
description: "GroupLN CPMCore/WWWCOPRO house conventions and scaffolding — apply patterns that took real investigation to pin down, without re-deriving them from the code every time. Currently: breadcrumb (wire up a CPMCore controller action's breadcrumb correctly)."
user-invocable: true
argument-hint: "breadcrumb <controller/action> <label path, e.g. Instellingen/Website/Cookiebanner>"
---

# GroupLN house conventions

Project-specific scaffolding for the **CPMCore** (ASP.NET Core admin app) and **WWWCOPRO** (classic ASP.NET MVC5 public site) codebases at `E:\TFS`. Each command below encodes a mechanism that took real digging through the code to pin down correctly. Use the recipe here instead of re-deriving it from scratch.

## Commands

| Command | What it does |
|---|---|
| `breadcrumb <controller/action> <label path>` | Adds a correctly-wired CPMCore breadcrumb to a controller action |

No argument, or a command not listed here: show this table and ask what to add — don't guess at a new convention.

---

## `breadcrumb`

### The mechanism — two parallel systems, only one renders

CPMCore has **two independent breadcrumb code paths**. They look similar and are easy to confuse; only one of them is ever visible on screen.

**1. `ViewData["BreadcrumbNode"]` + `SmartBreadcrumbs.Nodes.MvcBreadcrumbNode` — this is the one that renders.**

[Views/Shared/_Layout.cshtml](../../../CPMCore/Views/Shared/_Layout.cshtml) calls `@await Component.InvokeAsync("Breadcrumbs", new { viewName = "Default" })`, invoking [ViewComponents/BreadcrumbsViewComponent.cs](../../../CPMCore/ViewComponents/BreadcrumbsViewComponent.cs). That component does:

```csharp
var leaf = ViewContext.ViewData["BreadcrumbNode"] as BreadcrumbNode ?? _manager.GetNode(nodeKey);
```

`_manager` is a `SmartBreadcrumbs.BreadcrumbManager`, registered via `builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options => { })` in [Program.cs](../../../CPMCore/Program.cs) with empty options. That registration auto-scans for the *library's own* `SmartBreadcrumbs.Attributes.Breadcrumb` attribute — nothing in this codebase uses that attribute, so `_manager.GetNode(...)` is always effectively empty and the fallback never fires in practice.

**This means: an action that doesn't set `ViewData["BreadcrumbNode"]` renders no breadcrumb at all.** The component walks `.Parent` from the leaf up to the root and renders root → leaf.

**2. `[CPMCore.Attributes.BreadcrumbAttribute]` (used as the short `[Breadcrumb("Title")]`) + `[BreadcrumbActionFilter]` — this one is dead code for rendering.**

[Helpers/BreadcrumbAttribute.cs](../../../CPMCore/Helpers/BreadcrumbAttribute.cs) defines a trivial custom attribute — `namespace CPMCore.Attributes` despite living in the `Helpers/` folder — that just carries a `Title` string. **Nothing reads that property.** `[BreadcrumbActionFilter]` (applied class-wide on `BaseController`, in [Attributes/BreadcrumbActionFilter.cs](../../../CPMCore/Attributes/BreadcrumbActionFilter.cs)) separately builds a URL-path-derived list into `ViewBag.Breadcrumbs`, but its controller-type lookup is hardcoded to `Assembly.GetCallingAssembly().GetType("test.Controllers." + name)` — a namespace prefix (`test.Controllers`) that doesn't exist in this project, so it always throws, is caught, and returns null. `_Layout.cshtml` never reads `ViewBag.Breadcrumbs` anywhere. Dead end, both halves.

Every existing feature (`HomeHeroProjectController`, `InstellingenController.MarketDataStatus`, `Bouwindexen`, `KostprijsMaterialen`, `BudgetFormules`, `InvoiceTemplates`, `IssuerCompanies`, …) still applies `[Breadcrumb("Title")]` on the action anyway, purely for cosmetic consistency with the rest of the codebase — **keep doing that too**, so the action's attribute list still reads the way every other action's does. Just never rely on it to produce the visible breadcrumb; `ViewData["BreadcrumbNode"]` is the only thing that matters for what the user sees.

### Recipe

Given a controller/action and a desired label path (e.g. `Instellingen/Website/Cookiebanner` for `CookieConsentStatsController.Index`):

1. Usings: `using CPMCore.Attributes;` (for `[Breadcrumb]`) and `using SmartBreadcrumbs.Nodes;` (for `MvcBreadcrumbNode`).
2. Put `[Breadcrumb("<leaf label>")]` on the action, next to its existing `[HttpGet]` / `[CPMCore.Filters.PermissionRead(...)]` attributes.
3. At the top of the action body — before any early return, so it's set on every path that renders a view — build one `MvcBreadcrumbNode` per path segment, always rooted at Dashboard, each `Parent` pointing at the previous node:

   ```csharp
   var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
   var instellingenIndex = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
   // ...one node per intermediate segment...
   var leaf = new MvcBreadcrumbNode("<ActionName>", "<ControllerName>", "<Leaf label>") { Parent = <previous node> };
   ViewData["BreadcrumbNode"] = leaf;
   ```

4. **A middle segment that has no page of its own** — e.g. "Website" is only a section label on `Instellingen/Index` ([Views/Instellingen/Index.cshtml](../../../CPMCore/Views/Instellingen/Index.cshtml)'s `settings-section-label`), not a real route — still needs a valid `(action, controller)` pair, since `MvcBreadcrumbNode` always generates its link from one. Point it back at `("Index", "Instellingen")`, just with that section's own label as the title. It's a real, clickable node (goes to the Instellingen overview); the extra label is honest even without a dedicated URL.
5. **Default to the short chain.** Almost every other feature under Instellingen (`HomeHeroProject`, `Blog`, `EmailTemplates`, `Vacatures`, `MarketDataStatus`, `Bouwindexen`, `KostprijsMaterialen`, …) goes straight `Dashboard → Instellingen → Feature`, skipping its section label entirely. Only add the extra section-label segment (step 4) when the user explicitly asks for it by name — otherwise match the shorter, more common two-level pattern so the new breadcrumb doesn't stick out from its siblings. If asked to add a section segment, mention in your reply that it makes this one page's breadcrumb a level deeper than its siblings in the same section.

### Reference implementations

- **3-level chain with a section-label segment:** `CookieConsentStatsController.Index` — [CPMCore/Controllers/CookieConsentStatsController.cs](../../../CPMCore/Controllers/CookieConsentStatsController.cs) (`Dashboard → Instellingen → Website → Cookiebanner`).
- **Common 2-level chain:** `InstellingenController.MarketDataStatus` — [CPMCore/Controllers/InstellingenController.cs](../../../CPMCore/Controllers/InstellingenController.cs) (`Dashboard → Instellingen → Marktdata-status`).
