namespace FacadeCore.Signing;

/// <summary>
/// Verificatie van de ondertekenaar vóór het definitief ondertekenen (ONDERTEKENEN_VOORSTEL.md
/// §4.4): "EmailOtp" nu, "SmsOtp" later, eventueel sterkere methodes. CPM genereert en valideert
/// de OTP zelf; het kanaal levert enkel af. Meermaals geregistreerd, gekozen op <see cref="Key"/>
/// (= <c>SigningCase.VerificationMethod</c>). De methode kent de sessie- en partijbinding niet zelf;
/// die zit in de OTP-basis en in <see cref="ISigningService"/>.
/// </summary>
public interface IVerificationMethod
{
    /// <summary>Bv. "EmailOtp", "SmsOtp".</summary>
    string Key { get; }

    /// <summary>Weergavenaam voor pagina, mails en auditrapport, bv. "verificatiecode via e-mail".</summary>
    string DisplayName { get; }

    /// <summary>Sleutel van het kanaal dat deze methode gebruikt: "email" / "sms".</summary>
    string ChannelKey { get; }

    /// <summary>Waar de code heen moet. Wordt op het moment van aanvragen bepaald uit de BRON (nooit uit gebruikersinvoer), §6.3.</summary>
    Task<VerificationDestination> ResolveDestinationAsync(int partyType, int? sourceRefId, string? snapshotEmail, CancellationToken ct = default);

    /// <summary>Stelt het bericht op dat het kanaal moet afleveren. De code komt hier één keer voorbij en wordt nergens bewaard.</summary>
    OutboundMessage BuildMessage(string destination, string code, TimeSpan validity, SigningMailDocument document);
}
