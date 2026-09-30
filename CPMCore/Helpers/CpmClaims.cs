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
    public const string AuthProvider = "cpm:auth-provider"; // zie AuthProviders
}

/// <summary>Waarden van de <see cref="CpmClaims.AuthProvider"/>-claim en van UserGuestInvitation.Provider.</summary>
public static class AuthProviders
{
    public const string Entra  = "Entra";
    public const string Google = "Google";
}

/// <summary>Naam van het tweede OpenID Connect-schema (rechtstreeks naar Google, buiten Entra om).
/// Wordt enkel geregistreerd als Google:ClientId en Google:ClientSecret geconfigureerd zijn.</summary>
public static class GoogleAuthDefaults
{
    public const string Scheme       = "Google";
    public const string CallbackPath = "/signin-google";
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

    /// <summary>Provider waarmee deze sessie is aangemeld; ontbreekt de claim (sessies van vóór
    /// de Google-login) dan is het Entra.</summary>
    public static string GetCpmAuthProvider(this ClaimsPrincipal principal)
        => principal.FindFirstValue(CpmClaims.AuthProvider) ?? AuthProviders.Entra;

    public static bool IsGoogleLogin(this ClaimsPrincipal principal)
        => principal.GetCpmAuthProvider() == AuthProviders.Google;
}