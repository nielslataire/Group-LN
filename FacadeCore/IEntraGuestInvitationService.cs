namespace FacadeCore;

public record GuestInviteResult(bool Success, string? ErrorMessage = null)
{
    public static GuestInviteResult Ok => new(true);
}

public static class GuestInvitationStatus
{
    public const string Draft             = "Draft";
    public const string Invited           = "Invited";
    public const string PendingAcceptance = "PendingAcceptance";
    public const string Redeemed          = "Redeemed";
    public const string Active            = "Active";
    public const string Failed            = "Failed";
    public const string Revoked           = "Revoked";
}

public static class GuestAuditAction
{
    public const string InviteSent      = "InviteSent";
    public const string InviteResent    = "InviteResent";
    public const string InviteReset     = "InviteReset";
    public const string Redeemed        = "Redeemed";
    public const string FirstLogin      = "FirstLogin";
    public const string LoginUpdated    = "LoginUpdated";
    public const string Linked          = "Linked";
    public const string Unlinked        = "Unlinked";
    public const string UserDeactivated = "UserDeactivated";
}

public interface IEntraGuestInvitationService
{
    /// <summary>
    /// Nodigt een gebruiker uit als Entra B2B gast en stuurt branded e-mail.
    /// Idempotent: bestaand record wordt bijgewerkt.
    /// </summary>
    Task<GuestInviteResult> InviteGuestAsync(int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default, string loginPath = "/Account/Login");

    /// <summary>Maakt een nieuw Graph-invite (nieuw redeemUrl) en stuurt branded e-mail opnieuw.</summary>
    Task<GuestInviteResult> ResendInvitationAsync(int userId, int? invitedByUserId, string appBaseUrl, CancellationToken ct = default, string loginPath = "/Account/Login");

    /// <summary>Zet status terug op Revoked en wist OID-koppeling zodat beheerder opnieuw kan uitnodigen.</summary>
    Task<GuestInviteResult> ResetRedemptionAsync(int userId, int? performedByUserId, CancellationToken ct = default);

    /// <summary>Verwijdert het uitnodigingsrecord en de Entra OID-koppeling volledig.</summary>
    Task<GuestInviteResult> UnlinkGuestAsync(int userId, int? performedByUserId, CancellationToken ct = default);

    /// <summary>Verstuurt een voorbeeld uitnodigingsmail zonder Graph-aanroep (voor testdoeleinden).</summary>
    Task<GuestInviteResult> SendTestInviteEmailAsync(string toEmail, string appBaseUrl, CancellationToken ct = default);
}
