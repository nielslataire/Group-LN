namespace FacadeCore.Signing;

/// <summary>
/// Communicatiekanaal voor OTP-berichten (ONDERTEKENEN_VOORSTEL.md §4.5): "email" (over de
/// bestaande <see cref="IEmailSender"/>) en "sms" (over de geconfigureerde <see cref="ISmsProvider"/>).
/// Enkel afleveren; het kanaal genereert of bewaart nooit een code. Bewust een andere weg dan de
/// notificatiemails (<see cref="ISigningNotifier"/>): een template-fout in een notificatie kan
/// nooit een OTP tegenhouden, en omgekeerd.
/// </summary>
public interface IMessageChannel
{
    /// <summary>"email" / "sms".</summary>
    string Key { get; }

    /// <summary>Sleutel van de onderliggende provider, voor de audit trail: "smtp-o365", "bird", …</summary>
    string ProviderKey { get; }

    bool IsConfigured { get; }

    Task<DeliveryReceipt> SendAsync(OutboundMessage message, CancellationToken ct = default);
}

/// <summary>
/// Verwisselbare SMS-provider (Bird, Twilio, CM.com, …). In v1 is er géén implementatie
/// geregistreerd; <c>SmsChannel</c> meldt dan "niet geconfigureerd". Een provider toevoegen =
/// één adapterklasse + configuratie, zonder de signingflow te raken.
/// </summary>
public interface ISmsProvider
{
    /// <summary>Bv. "bird", "twilio", "cm".</summary>
    string Key { get; }

    Task<DeliveryReceipt> SendAsync(SmsMessage message, CancellationToken ct = default);

    /// <summary>Leest een delivery-status-webhook van deze provider; null wanneer de payload niet van deze provider is of ongeldig ondertekend.</summary>
    Task<DeliveryStatusUpdate?> ParseStatusWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
}
