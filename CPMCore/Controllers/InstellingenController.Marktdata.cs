using BOCore;
using CPMCore.Attributes;
using CPMCore.Models;
using CPMCore.Models.Home;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Definition;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ServiceCore.Invoicing.Pdf.Templates;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace CPMCore.Controllers;

/// <summary>Marktdata-status (crawlers/worker). Opgesplitst uit InstellingenController.cs (okt. 2026, structureren) - views in Views/Instellingen/Marktdata/. Zelfde partial class: alle private velden/services van InstellingenController.cs blijven gewoon bruikbaar.</summary>
public partial class InstellingenController
{
    // ─── Marktdata-status ───────────────────────────────────────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsMarketDataStatus)]
    [Breadcrumb("Marktdata-status")]
    public async Task<IActionResult> MarketDataStatus(CancellationToken ct)
    {
        SetPageHeader("bx bx-radar", "Marktdata-status");

        var dashboard    = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var status       = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("MarketDataStatus", "Instellingen", "Marktdata-status") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = status;

        var model = await _marketDataStatus.GetStatusAsync(ct: ct);
        return View(model);
    }
}
