using System.Security.Claims;

namespace CPMCore.Helpers;

public static class CpmClaims
{
    public const string UserId = "cpm:user-id";
    public const string UserCode = "cpm:user-code";
    public const string DisplayName = "cpm:display-name";
    public const string Email = "cpm:email";
    public const string EntraObjectId = "cpm:entra-oid";
    public const string Permission = "cpm:permission";
    public const string UserType = "cpm:user-type"; // "internal" | "contractor" | "customer"
}

public static class ClaimsPrincipalExtensions
{
    public static int? GetCpmUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(CpmClaims.UserId);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetCpmUserCode(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CpmClaims.UserCode);

    public static string? GetCpmDisplayName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CpmClaims.DisplayName) ?? principal.Identity?.Name;

    public static string? GetCpmEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CpmClaims.Email);

    public static string? GetCpmEntraObjectId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CpmClaims.EntraObjectId);

    public static string GetCpmUserType(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CpmClaims.UserType) ?? "internal";

    public static bool IsContractor(this ClaimsPrincipal principal)
        => principal.GetCpmUserType() == "contractor";
}