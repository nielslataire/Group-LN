namespace CPMCore.Models;

public class EntraLoginViewModel
{
    public string ReturnUrl { get; set; } = "~";
    public LoginType Type { get; set; } = LoginType.Internal;

    /// <summary>True als het Google-schema geregistreerd is (Google:ClientId/ClientSecret aanwezig)
    /// én dit logintype het mag tonen. Interne medewerkers loggen altijd via Entra in.</summary>
    public bool ShowGoogleLogin { get; set; }
}

public enum LoginType { Internal, Contractor, Customer }
