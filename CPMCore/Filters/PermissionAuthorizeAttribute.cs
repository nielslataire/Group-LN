using BOCore;
using CPMCore.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using FacadeCore;

namespace CPMCore.Filters;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _code;
    private readonly PermissionAccessType _access;

    public PermissionAuthorizeAttribute(string code, PermissionAccessType access = PermissionAccessType.Read)
    {
        _code = code;
        _access = access;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        await permissionService.EnsureLoadedAsync(context.HttpContext.RequestAborted);

        var allowed = _access switch
        {
            PermissionAccessType.Read => permissionService.HasRead(_code),
            PermissionAccessType.Write => permissionService.HasWrite(_code),
            PermissionAccessType.Delete => permissionService.HasDelete(_code),
            _ => false
        };

        if (!allowed)
        {
            const string deniedMessage = "Je hebt geen rechten om deze actie uit te voeren.";
            var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context.HttpContext);
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
            return;
        }

        await next();
    }
}

public class PermissionReadAttribute : PermissionAuthorizeAttribute
{
    public PermissionReadAttribute(string code) : base(code, PermissionAccessType.Read) { }
}

public class PermissionWriteAttribute : PermissionAuthorizeAttribute
{
    public PermissionWriteAttribute(string code) : base(code, PermissionAccessType.Write) { }
}

public class PermissionDeleteAttribute : PermissionAuthorizeAttribute
{
    public PermissionDeleteAttribute(string code) : base(code, PermissionAccessType.Delete) { }
}