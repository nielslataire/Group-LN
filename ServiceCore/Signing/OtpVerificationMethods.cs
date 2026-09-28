using System.Net;
using BOCore;
using DALCore.Models;
using FacadeCore.Signing;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Signing;

/// <summary>
/// Verificatiecode via e-mail (ONDERTEKENEN_VOORSTEL.md §4.4). De bestemming is het e-mailadres uit
/// de snapshot van de ondertekenaar (dat is ook waar de link naartoe ging — v1 bewijst dus
/// controle over die mailbox op het ogenblik van ondertekenen, geen tweede factor; zie §6.9).
/// Genereren/valideren van de code zit NIET hier maar in <see cref="SigningService"/>; deze klasse
/// zegt enkel wáár en hóe het bericht heen moet.
/// </summary>
public sealed class EmailOtpMethod : IVerificationMethod
{
    public string Key => "EmailOtp";
    public string DisplayName => "verificatiecode via e-mail";
    public string ChannelKey => "email";

    public Task<VerificationDestination> ResolveDestinationAsync(int partyType, int? sourceRefId, string? snapshotEmail, CancellationToken ct = default)
    {
        var email = snapshotEmail?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(new VerificationDestination(false, null, null, ChannelKey));
        return Task.FromResult(new VerificationDestination(true, email, SigningCrypto.MaskEmail(email), ChannelKey));
    }

    public OutboundMessage BuildMessage(string destination, string code, TimeSpan validity, SigningMailDocument document)
    {
        var minutes = Math.Max(1, (int)Math.Round(validity.TotalMinutes));
        var title = WebUtility.HtmlEncode(document.Title);
        var number = string.IsNullOrWhiteSpace(document.DocumentNumber) ? "" : $" ({WebUtility.HtmlEncode(document.DocumentNumber)})";
        var body = $@"
<p>Uw verificatiecode voor het ondertekenen van <strong>{title}</strong>{number}:</p>
<p style=""font-size:28px;letter-spacing:6px;font-weight:700;margin:18px 0"">{code}</p>
<p>De code is {minutes} minuten geldig en werkt enkel in het venster waarin u ze aanvroeg.</p>
<p style=""color:#5a6b58;font-size:12px"">Vroeg u deze code niet aan? Dan hoeft u niets te doen; zonder de code kan niemand ondertekenen.</p>";
        return new OutboundMessage(ChannelKey, destination, $"Uw verificatiecode: {code}", body, true);
    }
}

/// <summary>
/// Verificatiecode via SMS — de toekomstige tweede factor (§6.3). Het gsm-nummer wordt op het
/// moment van aanvragen uit de BRONRECORD gelezen (ClientContacts.Cellphone of, voor een
/// klantaccount, het primaire contact), nooit uit gebruikersinvoer. Werkt pas zodra een
/// <see cref="ISmsProvider"/> geregistreerd en geconfigureerd is; tot dan meldt <c>SmsChannel</c>
/// "niet geconfigureerd" en weigert de service de aanvraag netjes.
/// </summary>
public sealed class SmsOtpMethod : IVerificationMethod
{
    private readonly cpmRunningContext _db;

    public SmsOtpMethod(cpmRunningContext db) { _db = db; }

    public string Key => "SmsOtp";
    public string DisplayName => "verificatiecode via sms";
    public string ChannelKey => "sms";

    public async Task<VerificationDestination> ResolveDestinationAsync(int partyType, int? sourceRefId, string? snapshotEmail, CancellationToken ct = default)
    {
        string? phone = null;
        if (sourceRefId is int id)
        {
            if (partyType == (int)SigningPartyType.ClientContact)
            {
                phone = await _db.ClientContacts.AsNoTracking().Where(c => c.Id == id).Select(c => c.Cellphone).FirstOrDefaultAsync(ct);
            }
            else if (partyType == (int)SigningPartyType.ClientAccount)
            {
                // Migratie 058: het account (eigenaar 1) heeft nu een eigen Cellphone — die wint. Tot
                // dan bestond er geen accountnummer en viel de code voor eigenaar 1 se HANDTEKENING op
                // het gsm-nummer van een willekeurige contactpersoon/mede-eigenaar van dat account —
                // functioneel een andere persoon. Die terugval blijft enkel voor accounts zonder eigen
                // nummer (bestaande data), zodat lopende dossiers niet plots zonder sms-kanaal vallen.
                phone = await _db.ClientAccount.AsNoTracking()
                    .Where(a => a.Id == id && !string.IsNullOrEmpty(a.Cellphone))
                    .Select(a => a.Cellphone)
                    .FirstOrDefaultAsync(ct);

                if (string.IsNullOrWhiteSpace(phone))
                {
                    phone = await _db.ClientContacts.AsNoTracking()
                        .Where(c => c.ClientAccountId == id && !string.IsNullOrEmpty(c.Cellphone))
                        .OrderByDescending(c => c.IsPrimaryContact).ThenBy(c => c.Id)
                        .Select(c => c.Cellphone)
                        .FirstOrDefaultAsync(ct);
                }
            }
        }

        var e164 = NormalizeToE164(phone);
        if (e164 is null) return new VerificationDestination(false, null, null, ChannelKey);
        return new VerificationDestination(true, e164, SigningCrypto.MaskPhone(e164), ChannelKey);
    }

    public OutboundMessage BuildMessage(string destination, string code, TimeSpan validity, SigningMailDocument document)
    {
        var minutes = Math.Max(1, (int)Math.Round(validity.TotalMinutes));
        var text = $"CPM verificatiecode: {code} (geldig {minutes} min). Deel deze code met niemand.";
        return new OutboundMessage(ChannelKey, destination, null, text, false);
    }

    /// <summary>Belgische nummers naar +32…; internationale nummers met + blijven; al de rest is ongeldig.</summary>
    internal static string? NormalizeToE164(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length < 8) return null;
        if (raw.TrimStart().StartsWith('+')) return "+" + digits;
        if (digits.StartsWith("0032")) return "+" + digits[2..];
        if (digits.StartsWith("32") && digits.Length >= 11) return "+" + digits;
        if (digits.StartsWith('0')) return "+32" + digits[1..];
        return null;
    }
}
