namespace FacadeCore;

/// <summary>
/// Stuurt een portaaluitnodiging naar gebruikers van een bedrijf die een notificatie
/// hebben ontvangen — enkel als ze nog niet actief zijn op het portaal.
/// Geïmplementeerd in CPMCore zodat ServiceCore hier niet van afhangt.
/// </summary>
public interface IPortalInviteNotifier
{
    /// <summary>
    /// Zoekt alle portal-gebruikers voor <paramref name="companyId"/> die relevant zijn
    /// voor het gegeven project en stuurt hen een uitnodiging indien nodig.
    /// Volledige beheerders worden altijd uitgenodigd.
    /// Beperkte gebruikers alleen als hun ContactId overeenkomt met de SiteManagerContact
    /// van het contract voor <paramref name="projectId"/>.
    /// </summary>
    Task EnsureRelevantUsersInvitedAsync(
        int companyId,
        int projectId,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Nodigt een specifieke ontvanger (op basis van e-mail) uit voor het portaal.
    /// Maakt de gebruiker en de bedrijfstoegang aan indien nog niet aanwezig.
    /// Doet niets als de uitnodiging al actief is.
    /// </summary>
    Task EnsureRecipientInvitedAsync(
        int companyId,
        string email,
        string firstName,
        string lastName,
        bool isFullAdmin,
        int? contactId,
        string appBaseUrl,
        int? invitedByUserId,
        CancellationToken ct = default);
}
