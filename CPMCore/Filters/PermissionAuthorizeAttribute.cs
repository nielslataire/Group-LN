using CPMCore.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CPMCore.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _code;
    private readonly PermissionAccessType _access;

    public PermissionAuthorizeAttribute(string code, PermissionAccessType access = PermissionAccessType.Read)
    {
        _code = code;
        _access = access;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
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
            context.Result = new ForbidResult();
        }
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