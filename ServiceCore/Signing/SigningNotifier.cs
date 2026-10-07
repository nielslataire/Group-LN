using System.Net;
using FacadeCore;
using FacadeCore.Signing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceCore.Signing;

/// <summary>
/// Notificatiemails van de signingmodule (ONDERTEKENEN_VOORSTEL.md §5). Templates in code met eigen
/// plaatshouders — bewust niet de vrij bewerkbare EmailTemplate-tabel (§1, e-mail). Testmodus
/// (<c>Signing:TestRecipientOverride</c>): alles naar het testadres, echte ontvanger in het
/// onderwerp. Eén fout in een notificatie mag een dossier nooit blokkeren: elke verzending logt
/// en slikt haar eigen fout; de service registreert het resultaat als event.
/// </summary>
public sealed class SigningNotifier : ISigningNotifier
{
    private readonly IEmailSender _email;
    private readonly SigningOptions _options;
    private readonly ILogger<SigningNotifier> _logger;

    public SigningNotifier(IEmailSender email, IOptions<SigningOptions> options, ILogger<SigningNotifier> logger)
    {
        _email = email;
        _options = options.Value;
        _logger = logger;
        if (_options.IsTestMode)
            _logger.LogWarning("Signing draait in TESTMODUS: alle signing-mails gaan naar {TestRecipient}. Dit hoort leeg te zijn in productie.", _options.TestRecipientOverride);
    }

    public Task SendInvitationAsync(SigningMailDocument d, SigningMailRecipient to, string signUrl, CancellationToken ct = default)
        => SendAsync(to, $"Ter ondertekening: {Subject(d)}", Layout(
            $"Beste {H(to.Name)},",
            Message(d) +
            $"<p>{H(d.DocumentTypeLabel)} <strong>{H(d.Title)}</strong>{Number(d)}{Project(d)} staat klaar om elektronisch te ondertekenen.</p>" +
            $"<p>Via onderstaande persoonlijke link bekijkt u het volledige document en ondertekent u het na een verificatiecode.</p>" +
            Button(signUrl, "Document bekijken en ondertekenen") +
            Expiry(d) +
            "<p style=\"color:#5a6b58;font-size:12px\">Deze link is persoonlijk en geeft enkel toegang tot dit document. Stuur hem niet door.</p>"));

    public Task SendReminderAsync(SigningMailDocument d, SigningMailRecipient to, string signUrl, CancellationToken ct = default)
        => SendAsync(to, $"Herinnering — ter ondertekening: {Subject(d)}", Layout(
            $"Beste {H(to.Name)},",
            $"<p>Een herinnering: {H(d.DocumentTypeLabel).ToLowerInvariant()} <strong>{H(d.Title)}</strong>{Number(d)}{Project(d)} wacht nog op uw handtekening.</p>" +
            Message(d) +
            Button(signUrl, "Document bekijken en ondertekenen") +
            Expiry(d)));

    public Task SendNewLinkAsync(SigningMailDocument d, SigningMailRecipient to, string signUrl, CancellationToken ct = default)
        => SendAsync(to, $"Nieuwe ondertekenlink: {Subject(d)}", Layout(
            $"Beste {H(to.Name)},",
            $"<p>U ontvangt een nieuwe persoonlijke link voor <strong>{H(d.Title)}</strong>{Number(d)}. Eerdere links werken niet meer.</p>" +
            Button(signUrl, "Document bekijken en ondertekenen") +
            Expiry(d)));

    public Task SendSignedConfirmationAsync(SigningMailDocument d, SigningMailRecipient to, bool caseCompleted, CancellationToken ct = default)
        => SendAsync(to, $"Bevestiging van uw handtekening: {Subject(d)}", Layout(
            $"Beste {H(to.Name)},",
            $"<p>Uw elektronische handtekening voor <strong>{H(d.Title)}</strong>{Number(d)} is geregistreerd.</p>" +
            (caseCompleted
                ? "<p>Alle vereiste handtekeningen zijn gezet. U ontvangt het ondertekende document in een aparte e-mail.</p>"
                : "<p>Zodra alle betrokkenen ondertekend hebben, ontvangt u het ondertekende document.</p>")));

    public Task SendCompletedToPartyAsync(SigningMailDocument d, SigningMailRecipient to, byte[]? finalPdf, string? downloadUrl, CancellationToken ct = default)
    {
        var body = Layout(
            $"Beste {H(to.Name)},",
            $"<p><strong>{H(d.Title)}</strong>{Number(d)}{Project(d)} is door alle betrokkenen elektronisch ondertekend.</p>" +
            (finalPdf is not null ? "<p>Het ondertekende document vindt u als bijlage.</p>" : "") +
            (downloadUrl is not null ? Button(downloadUrl, "Ondertekend document downloaden") +
                $"<p style=\"color:#5a6b58;font-size:12px\">Deze downloadlink is {_options.DownloadLinkDays} dagen geldig.</p>" : ""));
        var attachments = finalPdf is null
            ? null
            : new[] { new EmailAttachment(SafeFileName($"{d.DocumentTypeLabel} {d.DocumentNumber ?? d.CaseId.ToString()} - ondertekend.pdf"), finalPdf, "application/pdf") };
        return SendAsync(to, $"Ondertekend: {Subject(d)}", body, attachments);
    }

    public Task SendCompletedInternalAsync(SigningMailDocument d, IReadOnlyList<SigningMailRecipient> recipients, string internalUrl, CancellationToken ct = default)
        => SendManyAsync(recipients, $"[CPM] Ondertekend: {Subject(d)}", Layout(
            "Beste,",
            $"<p>{H(d.DocumentTypeLabel)} <strong>{H(d.Title)}</strong>{Number(d)}{Project(d)} is door alle betrokkenen ondertekend.</p>" +
            Button(internalUrl, "Dossier openen in CPM")));

    public Task SendDeclinedInternalAsync(SigningMailDocument d, IReadOnlyList<SigningMailRecipient> recipients, string declinedBy, string reason, string internalUrl, CancellationToken ct = default)
        => SendManyAsync(recipients, $"[CPM] Geweigerd: {Subject(d)}", Layout(
            "Beste,",
            $"<p><strong>{H(declinedBy)}</strong> heeft geweigerd om <strong>{H(d.Title)}</strong>{Number(d)}{Project(d)} te ondertekenen.</p>" +
            $"<p>Reden: <em>{H(reason)}</em></p>" +
            Button(internalUrl, "Dossier openen in CPM")));

    public Task SendLinkNoLongerValidAsync(SigningMailDocument d, SigningMailRecipient to, string reasonText, CancellationToken ct = default)
        => SendAsync(to, $"Ondertekenlink niet meer geldig: {Subject(d)}", Layout(
            $"Beste {H(to.Name)},",
            $"<p>De ondertekenlink voor <strong>{H(d.Title)}</strong>{Number(d)} is niet meer geldig. {H(reasonText)}</p>" +
            "<p>Hebt u vragen, neem dan contact op met uw contactpersoon bij Group LN.</p>"));

    // ── Hulpfuncties ─────────────────────────────────────────────────────────────────────────

    private async Task SendManyAsync(IReadOnlyList<SigningMailRecipient> recipients, string subject, string body)
    {
        foreach (var r in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)))
            await SendAsync(r, subject, body);
    }

    private async Task SendAsync(SigningMailRecipient to, string subject, string body, IEnumerable<EmailAttachment>? attachments = null)
    {
        if (string.IsNullOrWhiteSpace(to.Email)) return;
        var address = to.Email;
        if (_options.IsTestMode)
        {
            subject = $"[TEST → {address}] {subject}";
            address = _options.TestRecipientOverride!;
        }
        try
        {
            await _email.SendEmailAsync(address, subject, body, attachments, fromEmail: _options.FromEmail);
        }
        catch (Exception ex)
        {
            // Nooit de body loggen (bevat de link). Wel het feit, zodat de service een event kan schrijven.
            _logger.LogWarning(ex, "Signing-notificatie '{Subject}' kon niet verstuurd worden.", subject);
            throw new SigningNotificationException(subject, ex);
        }
    }

    private static string Subject(SigningMailDocument d)
        => string.IsNullOrWhiteSpace(d.DocumentNumber) ? d.Title : $"{d.Title} ({d.DocumentNumber})";

    private static string Number(SigningMailDocument d)
        => string.IsNullOrWhiteSpace(d.DocumentNumber) ? "" : $" (nr. {H(d.DocumentNumber)})";

    private static string Project(SigningMailDocument d)
        => string.IsNullOrWhiteSpace(d.ProjectName) ? "" : $" voor project <strong>{H(d.ProjectName)}</strong>";

    /// <summary>Het vrije bericht van de afzender (21d, veld BERICHT): platte tekst, hier HTML-gecodeerd
    /// (nooit als opmaak vertrouwd) met behoud van de regeleinden. Leeg als er geen bericht is — de mail
    /// is dan exact wat hij altijd was.</summary>
    private static string Message(SigningMailDocument d)
        => string.IsNullOrWhiteSpace(d.InvitationMessage)
            ? ""
            : $"<p style=\"margin:14px 0;padding:12px 14px;background:#F2F5EF;border-radius:8px\">{H(d.InvitationMessage.Trim()).Replace("\r\n", "\n").Replace("\n", "<br/>")}</p>";

    private static string Expiry(SigningMailDocument d)
        => d.ExpiresAt is null ? "" : $"<p>U kunt ondertekenen tot <strong>{d.ExpiresAt.Value.ToLocalTime():dd/MM/yyyy}</strong>.</p>";

    /// <summary>Knop die in elk mailprogramma werkt: een tabelcel met <c>bgcolor</c> én <c>background-color</c> en de opvulling op
    /// de cel zelf. Outlook (Word-engine) en veel webviews negeren padding/achtergrond op een inline-block <c>&lt;a&gt;</c>, waardoor
    /// enkel een gekleurde tekstbalk overbleef; de link in de cel blijft daarom kaal (kleur, lettertype, geen onderstreping).</summary>
    private static string Button(string url, string label)
    {
        var href = H(url);
        var text = H(label);
        const string green = "#00532D";
        const string font = "'IBM Plex Sans',Segoe UI,Helvetica,Arial,sans-serif";
        return
            "<table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:22px 0;border-collapse:separate\"><tr>" +
            $"<td align=\"center\" bgcolor=\"{green}\" style=\"background-color:{green};border-radius:8px;padding:13px 24px\">" +
            $"<a href=\"{href}\" target=\"_blank\" style=\"color:#ffffff;font-family:{font};font-size:14px;font-weight:600;line-height:20px;text-decoration:none\">" +
            $"<font color=\"#ffffff\">{text}</font></a>" +
            "</td></tr></table>" +
            $"<p style=\"color:#5a6b58;font-size:12px\">Werkt de knop niet? Kopieer deze link in uw browser:<br/>{href}</p>";
    }

    /// <summary>Sobere HTML-omslag in de huisstijlkleuren (gl-v2-tokens: primary #00532D, ink #2C3B2A, muted #5a6b58).</summary>
    private static string Layout(string greeting, string inner)
        => "<div style=\"font-family:'IBM Plex Sans',Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;color:#2C3B2A;line-height:1.5;max-width:620px\">" +
           $"<p>{greeting}</p>{inner}" +
           "<p style=\"margin-top:26px\">Met vriendelijke groeten,<br/>Group LN</p>" +
           "<hr style=\"border:0;border-top:1px solid #dde1d9;margin:22px 0\"/>" +
           "<p style=\"color:#76807a;font-size:11px\">Deze e-mail werd automatisch verstuurd door CPM, het projectbeheer van Group LN.</p>" +
           "</div>";

    /// <summary>De huisstijl-omslag van de signingmails, ook bruikbaar voor andere mails van CPM (bv. de
    /// offerte aan de klant) zodat alle klantmails er hetzelfde uitzien.</summary>
    public static string MailLayout(string greeting, string inner) => Layout(greeting, inner);

    private static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    private static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}

/// <summary>Een notificatie kon niet verstuurd worden; de aanroeper beslist (meestal: event schrijven en doorgaan).</summary>
public sealed class SigningNotificationException : Exception
{
    public SigningNotificationException(string subject, Exception inner)
        : base($"Signing-notificatie kon niet verstuurd worden: {subject}", inner) { }
}
