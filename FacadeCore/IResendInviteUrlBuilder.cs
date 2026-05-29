namespace FacadeCore;

public interface IResendInviteUrlBuilder
{
    /// <summary>Genereert een gesigneerde URL waarmee de ontvanger een nieuwe uitnodiging kan aanvragen.</summary>
    string? Build(string email, int companyId, string appBaseUrl);

    /// <summary>Valideert de token-parameters. Geeft false als de handtekening ongeldig of verlopen is.</summary>
    bool TryValidate(string email, int companyId, long expiresUtcMs, string signature);
}
