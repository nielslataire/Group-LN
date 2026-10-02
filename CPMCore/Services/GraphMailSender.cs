using Azure.Identity;
using FacadeCore;
using Microsoft.Graph;

namespace CPMCore.Services;

/// <summary>
/// Verstuurt mail echt NAMENS een mailbox via Microsoft Graph (<c>/users/{afzender}/sendMail</c>), zodat de
/// mail uit de mailbox van de ingelogde gebruiker of projectleider vertrekt en in diens "Verzonden items"
/// belandt. Gebruikt dezelfde Entra-app als het inloggen (<c>AzureAd:TenantId/ClientId/ClientSecret</c>) en
/// vereist de Graph-toepassingsmachtiging <c>Mail.Send</c> met beheerderstoestemming. Eerste gebruik: de
/// offertemail (ProjectenController.ChangeOrderQuoteSendV2). Zonder configuratie of zonder recht geeft
/// <see cref="TrySendAsync"/> false/een fout terug en valt de aanroeper terug op <see cref="IEmailSender"/>.
/// Beperk de app in Exchange Online tot een groep mailboxen (New-ApplicationAccessPolicy), anders mag ze
/// namens elke mailbox van de tenant versturen.
/// </summary>
public sealed class GraphMailSender
{
    private readonly GraphServiceClient? _client;
    private readonly ILogger<GraphMailSender> _logger;

    public GraphMailSender(IConfiguration configuration, ILogger<GraphMailSender> logger)
    {
        _logger = logger;
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Graph-mail niet beschikbaar: AzureAd:TenantId/ClientId/ClientSecret ontbreken.");
            return;
        }
        _client = new GraphServiceClient(new ClientSecretCredential(tenantId, clientId, clientSecret), new[] { "https://graph.microsoft.com/.default" });
    }

    public bool IsConfigured => _client != null;

    /// <summary>True als Graph de mail aanvaardde. False (met log) als Graph niet geconfigureerd is of het
    /// versturen faalde (bv. 403: Mail.Send ontbreekt of het toegangsbeleid sluit deze mailbox uit) — de
    /// aanroeper valt dan terug op SMTP.</summary>
    public async Task<bool> TrySendAsync(string fromMailbox, string to, string subject, string html, IEnumerable<EmailAttachment>? attachments, CancellationToken ct)
    {
        if (_client == null || string.IsNullOrWhiteSpace(fromMailbox)) return false;
        try
        {
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = html },
                ToRecipients = new List<Recipient> { new() { EmailAddress = new EmailAddress { Address = to } } },
                Attachments = new MessageAttachmentsCollectionPage(),
            };
            foreach (var a in attachments ?? Array.Empty<EmailAttachment>())
                message.Attachments.Add(new FileAttachment { Name = a.FileName, ContentType = a.ContentType, ContentBytes = a.Content });
            await _client.Users[fromMailbox].SendMail(message, true).Request().PostAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mail versturen via Graph namens {Mailbox} mislukt; terugval op SMTP.", fromMailbox);
            return false;
        }
    }
}
