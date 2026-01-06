using CPMCore.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CPMCore.Services.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var matched = context.User?.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == CpmClaims.Permission)
            && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)) == true;

        if (matched)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}