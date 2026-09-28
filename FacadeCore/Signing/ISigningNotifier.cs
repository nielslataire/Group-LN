namespace FacadeCore.Signing;

/// <summary>
/// Notificatiemails van de signingmodule (ONDERTEKENEN_VOORSTEL.md §5, "E-mails en berichten"):
/// uitnodiging, herinnering, nieuwe link, bevestiging na ondertekening, voltooiing (intern én
/// naar de ondertekenaars), weigering (intern), annulering (naar de ondertekenaars). Bewust
/// gescheiden van de OTP-aflevering (<see cref="IMessageChannel"/>). Templates zitten in code met
/// eigen plaatshouders — niet in de vrij bewerkbare EmailTemplate-tabel, zodat een beheerder
/// nooit per ongeluk de ondertekenlink uit een uitnodiging kan knippen.
/// </summary>
public interface ISigningNotifier
{
    Task SendInvitationAsync(SigningMailDocument document, SigningMailRecipient recipient, string signUrl, CancellationToken ct = default);

    Task SendReminderAsync(SigningMailDocument document, SigningMailRecipient recipient, string signUrl, CancellationToken ct = default);

    Task SendNewLinkAsync(SigningMailDocument document, SigningMailRecipient recipient, string signUrl, CancellationToken ct = default);

    /// <summary>Naar de ondertekenaar zelf, meteen na zijn handtekening (het dossier kan nog op anderen wachten).</summary>
    Task SendSignedConfirmationAsync(SigningMailDocument document, SigningMailRecipient recipient, bool caseCompleted, CancellationToken ct = default);

    /// <summary>Naar elke ondertekenaar zodra het volledige dossier voltooid is: ondertekende PDF als bijlage + downloadlink (§9.5).</summary>
    Task SendCompletedToPartyAsync(SigningMailDocument document, SigningMailRecipient recipient, byte[]? finalPdf, string? downloadUrl, CancellationToken ct = default);

    Task SendCompletedInternalAsync(SigningMailDocument document, IReadOnlyList<SigningMailRecipient> recipients, string internalUrl, CancellationToken ct = default);

    Task SendDeclinedInternalAsync(SigningMailDocument document, IReadOnlyList<SigningMailRecipient> recipients, string declinedBy, string reason, string internalUrl, CancellationToken ct = default);

    /// <summary>Naar ondertekenaars wier link vervalt omdat het dossier geannuleerd/verlopen is, of omdat iemand anders al tekende (ANY-regel).</summary>
    Task SendLinkNoLongerValidAsync(SigningMailDocument document, SigningMailRecipient recipient, string reasonText, CancellationToken ct = default);
}
