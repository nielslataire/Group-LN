using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using ServiceCore;

namespace CPMCore.Filters
{
    /// <summary>Globale vangnet voor databasefouten bij opslaan/verwijderen (typisch "The DELETE statement
    /// conflicted with the REFERENCE constraint …"): i.p.v. de foutpagina krijgt de gebruiker een Nederlandse
    /// melding (TempData-melding + terug naar de vorige pagina, of JSON 409 voor AJAX). De fout wordt altijd
    /// gelogd. Acties die zelf al een specifiekere afhandeling hebben (bv. KlantenController.DeleteClient)
    /// vangen de fout vóór dit filter; dit is de laatste verdedigingslinie voor alle andere acties.</summary>
    public class DbUpdateExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<DbUpdateExceptionFilter> _logger;
        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public DbUpdateExceptionFilter(ILogger<DbUpdateExceptionFilter> logger, ITempDataDictionaryFactory tempDataFactory)
        {
            _logger = logger;
            _tempDataFactory = tempDataFactory;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.ExceptionHandled || context.Exception is not DbUpdateException ex) return;

            var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a ?? "" : "";
            var isDelete = action.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Remove", StringComparison.OrdinalIgnoreCase)
                || action.Contains("Verwijder", StringComparison.OrdinalIgnoreCase)
                || context.HttpContext.Request.Method == "DELETE";
            var message = DbErrorTranslator.ToFriendlyMessage(ex, isDelete ? "verwijderd" : "opgeslagen");
            _logger.LogWarning(ex, "Databasefout in {Controller}.{Action}: {Message}",
                context.ActionDescriptor.RouteValues.TryGetValue("controller", out var ctl) ? ctl : "", action, message);

            var request = context.HttpContext.Request;
            var wantsJson = string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (wantsJson)
            {
                context.Result = new ObjectResult(new { success = false, error = message }) { StatusCode = StatusCodes.Status409Conflict };
            }
            else
            {
                var tempData = _tempDataFactory.GetTempData(context.HttpContext);
                tempData["Message"] = message;
                tempData["MessageType"] = "error";
                tempData["MessageTitle"] = isDelete ? "Niet verwijderd" : "Niet opgeslagen";
                context.Result = new RedirectResult(SafeReturnUrl(context));
            }
            context.ExceptionHandled = true;
        }

        // Alleen terug naar een pagina van deze site (Referer kan van elders komen).
        private static string SafeReturnUrl(ExceptionContext context)
        {
            var referer = context.HttpContext.Request.Headers.Referer.ToString();
            if (Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                && string.Equals(uri.Authority, context.HttpContext.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
                return uri.PathAndQuery;
            return "/";
        }
    }
}
