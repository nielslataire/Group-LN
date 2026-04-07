using BOCore;
using CPMCore.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using FacadeCore;


namespace CPMCore.Filters;

public class PermissionConventionFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionResolver _resolver;
    private readonly IPermissionService _permissionService;

    public PermissionConventionFilter(IPermissionResolver resolver, IPermissionService permissionService)
    {
        _resolver = resolver;
        _permissionService = permissionService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Filters.OfType<IAllowAnonymousFilter>().Any() || context.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any())
            return;

        // Aannemers en klanten hebben geen interne permissies.
        // Ze mogen enkel hun eigen portaal + Account (sign-out, foto) gebruiken.
        var userType = context.HttpContext.User.FindFirst(CPMCore.Helpers.CpmClaims.UserType)?.Value;
        if (userType == "contractor")
        {
            var ctrl = context.ActionDescriptor.RouteValues.TryGetValue("controller", out var cb) ? cb : null;
            var isAllowed = string.Equals(ctrl, "ContractorPortal", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(ctrl, "Account", StringComparison.OrdinalIgnoreCase);
            if (!isAllowed)
                context.Result = new RedirectResult("/Werfportaal");
            return;
        }
        if (userType == "customer")
        {
            var ctrl = context.ActionDescriptor.RouteValues.TryGetValue("controller", out var ca) ? ca : null;
            var isAllowed = string.Equals(ctrl, "CustomerPortal", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(ctrl, "Account", StringComparison.OrdinalIgnoreCase);
            if (!isAllowed)
                context.Result = new RedirectResult("/klantenportaal");
            return;
        }

        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<PermissionAuthorizeAttribute>() != null)
            return;

        var cad = context.ActionDescriptor.RouteValues;
        var controller = cad.TryGetValue("controller", out var c) ? c : null;
        var action = cad.TryGetValue("action", out var a) ? a : null;

        await _permissionService.EnsureLoadedAsync(context.HttpContext.RequestAborted);
        var code = _resolver.Resolve(controller, action);
        var access = _resolver.ResolveAccessType(context.HttpContext.Request.Method, action);

        var allowed = access switch
        {
            PermissionAccessType.Read => _permissionService.HasRead(code),
            PermissionAccessType.Write => _permissionService.HasWrite(code),
            PermissionAccessType.Delete => _permissionService.HasDelete(code),
            _ => false
        };

        if (!allowed)
        {
            var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context.HttpContext);
            const string deniedMessage = "Je hebt geen rechten om deze actie uit te voeren.";
            tempData["Message"] = deniedMessage;
            tempData["MessageType"] = "error";
            tempData["MessageTitle"] = "Geen toegang";

            var request = context.HttpContext.Request;
            var isAjaxRequest = string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            if (isAjaxRequest)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = deniedMessage,
                    redirectUrl = "/Account/AccessDenied"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}