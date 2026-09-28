using FacadeCore;
using FacadeCore.Signing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceCore.Signing;

/// <summary>
/// OTP-kanaal "email" over de bestaande <see cref="IEmailSender"/> (ONDERTEKENEN_VOORSTEL.md §4.5).
/// SMTP geeft geen message-id terug, dus <c>ProviderMessageId</c> blijft leeg en de status stopt bij
/// "Accepted" (aanvaard door de SMTP-server). In testmodus (<c>Signing:TestRecipientOverride</c>)
/// gaat het bericht naar het testadres, met de echte bestemming in het onderwerp.
/// </summary>
public sealed class EmailChannel : IMessageChannel
{
    private readonly IEmailSender _email;
    private readonly SigningOptions _options;
    private readonly ILogger<EmailChannel> _logger;

    public EmailChannel(IEmailSender email, IOptions<SigningOptions> options, ILogger<EmailChannel> logger)
    {
        _email = email;
        _options = options.Value;
        _logger = logger;
    }

    public string Key => "email";
    public string ProviderKey => "smtp-o365";
    public bool IsConfigured => true;

    public async Task<DeliveryReceipt> SendAsync(OutboundMessage message, CancellationToken ct = default)
    {
        var to = message.Destination;
        var subject = message.Subject ?? "CPM";
        if (_options.IsTestMode)
        {
            subject = $"[TEST → {to}] {subject}";
            to = _options.TestRecipientOverride!;
        }

        try
        {
            await _email.SendEmailAsync(to, subject, message.Body, fromEmail: _options.FromEmail);
            return new DeliveryReceipt(true, ProviderKey, null, DateTime.UtcNow, "accepted", null);
        }
        catch (Exception ex)
        {
            // Enkel het feit, nooit de inhoud (die bevat de code).
            _logger.LogWarning(ex, "OTP-mail kon niet verstuurd worden via {Provider}.", ProviderKey);
            return new DeliveryReceipt(false, ProviderKey, null, DateTime.UtcNow, "failed", ex.GetType().Name);
        }
    }
}

/// <summary>
/// OTP-kanaal "sms" over de geconfigureerde <see cref="ISmsProvider"/> (<c>Signing:SmsProvider</c>).
/// In v1 is er geen provider geregistreerd: <see cref="IsConfigured"/> is dan false en de service
/// weigert een SMS-verificatie met een nette melding. Een provider toevoegen = één adapterklasse
/// registreren + de sleutel in de configuratie.
/// </summary>
public sealed class SmsChannel : IMessageChannel
{
    private readonly ISmsProvider? _provider;
    private readonly ILogger<SmsChannel> _logger;

    public SmsChannel(IEnumerable<ISmsProvider> providers, IOptions<SigningOptions> options, ILogger<SmsChannel> logger)
    {
        var key = options.Value.SmsProvider;
        _provider = string.IsNullOrWhiteSpace(key)
            ? null
            : providers.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
        _logger = logger;
    }

    public string Key => "sms";
    public string ProviderKey => _provider?.Key ?? "none";
    public bool IsConfigured => _provider is not null;

    public async Task<DeliveryReceipt> SendAsync(OutboundMessage message, CancellationToken ct = default)
    {
        if (_provider is null)
            return new DeliveryReceipt(false, ProviderKey, null, DateTime.UtcNow, "not-configured", "Geen SMS-provider geconfigureerd.");
        try
        {
            return await _provider.SendAsync(new SmsMessage(message.Destination, message.Body), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OTP-sms kon niet verstuurd worden via {Provider}.", ProviderKey);
            return new DeliveryReceipt(false, ProviderKey, null, DateTime.UtcNow, "failed", ex.GetType().Name);
        }
    }
}
