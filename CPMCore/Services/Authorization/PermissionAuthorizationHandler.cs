using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CPMCore.Services.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role
                && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)) == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}