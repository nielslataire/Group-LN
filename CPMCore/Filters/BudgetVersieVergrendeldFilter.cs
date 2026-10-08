using System;
using System.Collections.Generic;
using System.Linq;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CPMCore.Filters;

/// <summary>
/// Een definitieve budgetversie is alleen-lezen (design-handoff 39a/39j, migratie 078): elke schrijvende POST van de budgetwizard op
/// zo'n versie wordt geweigerd. De versie wordt gezocht in de gebonden argumenten (parameter <c>versieId</c>/<c>doelVersieId</c> of een
/// model/DTO met <c>VersieId</c>/<c>BudgetVersieId</c>). Acties die de status zelf beheren staan op de uitzonderingslijst.
/// </summary>
public sealed class BudgetVersieVergrendeldFilter : IActionFilter
{
    // Acties die een definitieve versie wél mogen aanraken (status wijzigen, kopiëren, lezen via POST).
    private static readonly HashSet<string> Uitzonderingen = new(StringComparer.OrdinalIgnoreCase)
    {
        "BudgetVersieOntgrendelen", "BudgetVersieDefinitief", "BudgetVersieAfronden", "BudgetVersieActiveren", "BudgetNieuweVersie",
        "HerstelVersie", "BudgetKopieer", "DownloadBudgetExcel", "DownloadBudgetPdf"
    };

    private const string ItemsSleutel = "BudgetVersieId";
    private readonly IBudgetService _budget;
    private readonly cpmRunningContext _db;
    public BudgetVersieVergrendeldFilter(IBudgetService budget, cpmRunningContext db) { _budget = budget; _db = db; }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method)) return;
        var actie = context.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a : null;
        if (actie == null || Uitzonderingen.Contains(actie)) return;
        if (!(actie.StartsWith("Budget", StringComparison.OrdinalIgnoreCase) || actie.StartsWith("SaveBudget", StringComparison.OrdinalIgnoreCase)
              || actie is "SaveActivityLijnen" or "SetNacalcReferenties" or "DoorzettenVerkoopNaarUnits" or "BlankVerkoopRij")) return;

        var versieId = VindVersieId(context.ActionArguments);
        if (versieId is null) return;
        context.HttpContext.Items[ItemsSleutel] = versieId.Value;
        if (!_budget.IsVergrendeld(versieId.Value)) return;

        const string melding = "Deze versie is definitief en alleen-lezen. Maak ze eerst niet-definitief, of werk verder in een kopie als nieuwe versie.";
        var req = context.HttpContext.Request;
        var isAjax = req.Headers["X-Requested-With"] == "XMLHttpRequest"
                     || (req.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase)
                     || (req.Headers["Accept"].ToString()).Contains("json", StringComparison.OrdinalIgnoreCase);
        context.Result = isAjax
            ? new JsonResult(new { success = false, message = melding, vergrendeld = true })
            : new RedirectToActionResult("BudgetResultaat", "Projecten", new { versieId = versieId.Value }) { };
        if (!isAjax && context.Controller is Microsoft.AspNetCore.Mvc.Controller c) c.TempData["Error"] = melding;
    }

    /// <summary>"Laatst gewijzigd" (overzicht 39a): na elke geslaagde schrijvende budgetactie het tijdstip op de versie zetten.
    /// Statusacties tellen niet mee; mislukte of geweigerde acties (JSON success=false) evenmin.</summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null || !context.HttpContext.Items.TryGetValue(ItemsSleutel, out var o) || o is not int versieId) return;
        if (context.Result is JsonResult { Value: { } v } && v.GetType().GetProperty("success")?.GetValue(v) is false) return;
        var actie = context.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a : null;
        if (actie == null || Uitzonderingen.Contains(actie) || actie.StartsWith("BudgetWaarschuwing", StringComparison.OrdinalIgnoreCase)) return;
        try { _db.BudgetVersie.Where(x => x.Id == versieId).ExecuteUpdate(x => x.SetProperty(p => p.GewijzigdOp, DateTime.Now).SetProperty(p => p.TotaalKosten, (decimal?)null)); }
        catch { /* bewaren van het tijdstip mag nooit een geslaagde opslag laten falen */ }
    }

    private static int? VindVersieId(IDictionary<string, object> args)
    {
        foreach (var kv in args)
        {
            if (kv.Value is int i && (kv.Key.Equals("versieId", StringComparison.OrdinalIgnoreCase) || kv.Key.Equals("doelVersieId", StringComparison.OrdinalIgnoreCase))) return i;
        }
        foreach (var kv in args)
        {
            if (kv.Value == null || kv.Value is string || kv.Value.GetType().IsPrimitive) continue;
            foreach (var naam in new[] { "VersieId", "BudgetVersieId" })
            {
                var p = kv.Value.GetType().GetProperty(naam);
                if (p?.PropertyType == typeof(int) && p.GetValue(kv.Value) is int v && v > 0) return v;
            }
        }
        return null;
    }
}
