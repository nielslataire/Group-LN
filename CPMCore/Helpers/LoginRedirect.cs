using Microsoft.AspNetCore.Authentication;

namespace CPMCore.Helpers;

/// <summary>
/// Bouwt de terugkeer-URL naar /Account/Login na een mislukte externe login (Entra of Google).
/// Het logintype (contractor/customer) reist mee in AuthenticationProperties.Items, gezet door
/// AccountController.SignIn, zodat een aannemer na een fout de portaal-layout blijft zien i.p.v.
/// de interne loginpagina.
/// </summary>
public static class LoginRedirect
{
    public const string LoginTypeItem = "cpm:login-type";

    public static string WithError(AuthenticationProperties? properties, string message)
    {
        var url = $"/Account/Login?error={Uri.EscapeDataString(message)}";

        if (properties?.Items.TryGetValue(LoginTypeItem, out var type) == true
            && !string.IsNullOrWhiteSpace(type))
        {
            url += $"&type={Uri.EscapeDataString(type)}";
        }

        if (!string.IsNullOrWhiteSpace(properties?.RedirectUri)
            && properties.RedirectUri.StartsWith('/')
            && !properties.RedirectUri.StartsWith("//", StringComparison.Ordinal))
        {
            url += $"&returnUrl={Uri.EscapeDataString(properties.RedirectUri)}";
        }

        return url;
    }
}

/// <summary>Interne foutcodes die OnTokenValidated via context.Fail doorgeeft aan OnRemoteFailure.</summary>
public static class GoogleLoginErrors
{
    public const string NotLinked = "CPM_GOOGLE_NOT_LINKED";
}
