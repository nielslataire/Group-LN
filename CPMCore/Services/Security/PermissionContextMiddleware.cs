namespace CPMCore.Services.Security;

public class PermissionContextMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            await permissionService.EnsureLoadedAsync(context.RequestAborted);
        }

        await _next(context);
    }
}