using System.Globalization;
using BOCore;
using DALCore.Models;
using FacadeCore.Signing;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Signing;

namespace CPMCore.Services.Signing;

/// <summary>
/// De eerste <see cref="ISigningDocumentSource"/>: wijzigingsopdrachten (ONDERTEKENEN_VOORSTEL.md §4.2,
/// §10 fase 1). Dit is de enige klasse in de signingmodule die iets over <c>ChangeOrder</c> weet.
/// - Pakket: de QuestPDF via <see cref="ChangeOrderPdfBuilder"/>, titel/nummer/bedragen voor de
///   ondertekenpagina, en de voorgestelde ondertekenaars (§9.7: het klantaccount en de
///   mede-eigenaars uit ClientContacts, regel ALL). De interne gebruiker past de lijst aan op het
///   startscherm; de bron doet enkel een voorstel.
/// - Vergrendeling (§9.2) is afgeleid: <c>ProjectenController</c> weigert bewerken/verwijderen zolang
///   <c>ISigningService.GetActiveCaseForSourceAsync</c> een dossier teruggeeft. Deze klasse zet daarom
///   géén vlag op de ChangeOrder; <see cref="OnCaseOpenedAsync"/> vult enkel "datum verzonden" in.
/// - Vingerafdruk: alle velden die de inhoud van het document bepalen. Verandert er iets terwijl het
///   dossier loopt, dan weigert de service de handtekening.
/// - Voltooiing: <c>DateAgreement</c> wordt de dag van de laatste handtekening — het veld dat de
///   lijst "Wijzigingsopdrachten" al als "Akkoord" toont.
/// </summary>
public sealed class ChangeOrderSigningSource : ISigningDocumentSource
{
    public const string Key = "ChangeOrder";

    private static readonly TimeZoneInfo Brussels = ResolveBrussels();

    private readonly cpmRunningContext _db;
    private readonly ChangeOrderPdfBuilder _pdf;
    private readonly ILogger<ChangeOrderSigningSource> _logger;

    public ChangeOrderSigningSource(cpmRunningContext db, ChangeOrderPdfBuilder pdf, ILogger<ChangeOrderSigningSource> logger)
    {
        _db = db;
        _pdf = pdf;
        _logger = logger;
    }

    public string DocumentType => Key;
    public string DisplayName => "Wijzigingsopdracht";

    public async Task<SigningDocumentPackage> BuildAsync(int sourceEntityId, int byUserId, CancellationToken ct = default)
    {
        var model = await _pdf.LoadAsync(sourceEntityId, ct)
                    ?? throw new InvalidOperationException($"Wijzigingsopdracht {sourceEntityId} bestaat niet.");
        var pdf = _pdf.Render(model);

        var parties = await SuggestPartiesAsync(model, ct);

        return new SigningDocumentPackage(
            Pdf: pdf,
            FileName: ChangeOrderPdfBuilder.FileName(model),
            Title: $"Wijzigingsopdracht {model.Reference} — {model.ProjectName}",
            DocumentNumber: model.Reference,
            Summary: model.Description,
            AmountExclVat: Math.Round(model.TotalExcl, 2, MidpointRounding.AwayFromZero),
            VatAmount: Math.Round(model.VatAmount, 2, MidpointRounding.AwayFromZero),
            AmountInclVat: Math.Round(model.TotalIncl, 2, MidpointRounding.AwayFromZero),
            ProjectId: model.ProjectId > 0 ? model.ProjectId : null,
            ClientAccountId: model.ClientAccountId,
            SuggestedParties: parties,
            SuggestedRule: (int)SigningRule.All,
            Attachments: Array.Empty<SigningAttachmentInput>());
    }

    /// <summary>Voorstel: eerst het klantaccount (het contract­adres), daarna elke mede-eigenaar uit de
    /// contacten met een eigen e-mailadres dat nog niet in de lijst zit. Wie geen e-mail heeft komt
    /// tóch in het voorstel — het startscherm toont dan een waarschuwing en laat het aanvullen —
    /// want stil weglaten zou een mede-eigenaar doen verdwijnen.</summary>
    public async Task<IReadOnlyList<SigningPartyInput>> SuggestPartiesAsync(Documents.ChangeOrderPdfModel model, CancellationToken ct)
    {
        var list = new List<SigningPartyInput>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 0;

        var account = await _db.ClientAccount.AsNoTracking().FirstOrDefaultAsync(a => a.Id == model.ClientAccountId, ct);
        if (account is not null)
        {
            list.Add(new SigningPartyInput((int)SigningPartyType.ClientAccount, account.Id, ChangeOrderPdfBuilder.DisplayName(account),
                account.Email?.Trim(), null, "Klant", order++));
            if (!string.IsNullOrWhiteSpace(account.Email)) seen.Add(account.Email.Trim());
        }

        var contacts = await _db.ClientContacts.AsNoTracking()
            .Where(c => c.ClientAccountId == model.ClientAccountId && c.IsCoOwner)
            .OrderByDescending(c => c.IsPrimaryContact).ThenBy(c => c.Id)
            .ToListAsync(ct);
        foreach (var c in contacts)
        {
            var email = c.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email) && !seen.Add(email)) continue;   // zelfde adres als het account: één handtekening volstaat
            var name = string.Join(" ", new[] { c.Forename, c.Name }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(name)) name = c.CompanyName ?? "Mede-eigenaar";
            list.Add(new SigningPartyInput((int)SigningPartyType.ClientContact, c.Id, name, email,
                SigningCrypto.MaskPhone(c.Cellphone), "Mede-eigenaar", order++));
        }

        return list;
    }

    public async Task OnCaseOpenedAsync(int sourceEntityId, int caseId, CancellationToken ct = default)
    {
        var co = await _db.ChangeOrder.FirstOrDefaultAsync(c => c.Id == sourceEntityId, ct);
        if (co is null) return;
        if (co.DateSendToClient is null)
        {
            co.DateSendToClient = Today();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task OnCaseCompletedAsync(int sourceEntityId, int caseId, DateTime completedAtUtc, CancellationToken ct = default)
    {
        var co = await _db.ChangeOrder.FirstOrDefaultAsync(c => c.Id == sourceEntityId, ct);
        if (co is null) return;
        co.DateAgreement = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(completedAtUtc, Brussels));
        co.DateSendToClient ??= co.DateAgreement;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Wijzigingsopdracht {ChangeOrderId} kreeg akkoorddatum {Date} via ondertekendossier {CaseId}.", sourceEntityId, co.DateAgreement, caseId);
    }

    public Task OnCaseClosedAsync(int sourceEntityId, int caseId, int caseStatus, CancellationToken ct = default)
        => Task.CompletedTask;   // vergrendeling is afgeleid van het actieve dossier — niets terug te zetten

    public async Task<string?> ComputeFingerprintAsync(int sourceEntityId, CancellationToken ct = default)
    {
        var co = await _db.ChangeOrder.AsNoTracking()
            .Include(c => c.ChangeOrderDetail)
            .FirstOrDefaultAsync(c => c.Id == sourceEntityId, ct);
        if (co is null) return null;

        var inv = CultureInfo.InvariantCulture;
        var parts = new List<string?>
        {
            co.Id.ToString(inv), co.ClientAccountId.ToString(inv), co.ContractActivityId.ToString(inv),
            co.Description, co.Date.ToString("O", inv), co.ExpirationDate.ToString("O", inv),
            co.Comment, co.ChangeOrderConditions, co.Invoiceable ? "1" : "0",
        };
        foreach (var d in co.ChangeOrderDetail.OrderBy(d => d.Id))
        {
            parts.Add(d.Id.ToString(inv));
            parts.Add(d.Description);
            parts.Add(d.MeasurementType?.ToString(inv));
            parts.Add(d.MeasurementUnit?.ToString(inv));
            parts.Add(d.Number.ToString(inv));
            parts.Add(d.Price.ToString("0.####", inv));
            parts.Add(d.Commission.ToString("0.####", inv));
            parts.Add(d.VatPercentage?.ToString("0.####", inv));
        }
        return SigningCrypto.ComputeFingerprint(parts);
    }

    public async Task<string?> GetProjectNameAsync(int sourceEntityId, CancellationToken ct = default)
        => await _db.ChangeOrder.AsNoTracking()
            .Where(c => c.Id == sourceEntityId)
            .Select(c => c.ContractActivity.Contract.Project.ProjectName)
            .FirstOrDefaultAsync(ct);

    /// <summary>De verkoopverantwoordelijke van het project, anders de projectverantwoordelijke
    /// (Project.AspNetUser). Zonder e-mail: lege lijst — de service logt dat als NotificationFailed.</summary>
    public async Task<IReadOnlyList<SigningMailRecipient>> GetInternalNotificationRecipientsAsync(int sourceEntityId, CancellationToken ct = default)
    {
        var project = await _db.ChangeOrder.AsNoTracking()
            .Where(c => c.Id == sourceEntityId)
            .Select(c => c.ContractActivity.Contract.Project)
            .Include(p => p.SalesResponsibleAspNetUser)
            .Include(p => p.AspNetUser)
            .FirstOrDefaultAsync(ct);
        if (project is null) return Array.Empty<SigningMailRecipient>();

        var user = project.SalesResponsibleAspNetUser ?? project.AspNetUser;
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return Array.Empty<SigningMailRecipient>();
        var name = string.Join(" ", new[] { user.Voornaam, user.Familienaam }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new[] { new SigningMailRecipient(string.IsNullOrWhiteSpace(name) ? user.Email : name, user.Email) };
    }

    private static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Brussels));

    private static TimeZoneInfo ResolveBrussels()
    {
        foreach (var id in new[] { "Europe/Brussels", "Romance Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { /* volgende */ }
        }
        return TimeZoneInfo.Local;
    }
}
