using System.Globalization;
using BOCore;
using CPMCore.Services;
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
    // Volledig gekwalificeerd waar gebruikt: FacadeCore.IDocumentService/DocUploadDto/DocLinkRef liggen
    // in dezelfde root-namespace als FacadeCore.ISigningService (Side B) — "using FacadeCore;" zou hier
    // de al geïmporteerde FacadeCore.Signing.ISigningService ambigu maken. Geen ISigningService-injectie
    // hier: SigningService bouwt zijn ISigningDocumentSource-lijst op (via SigningRegistry) en zou
    // anders een kringafhankelijkheid vormen — het definitieve PDF wordt daarom rechtstreeks via _db
    // gelezen (SigningCase.FinalDocumentId), niet via de service.
    private readonly FacadeCore.IDocumentService _documents;
    private readonly DocStorageService _storage;
    private readonly ILogger<ChangeOrderSigningSource> _logger;

    public ChangeOrderSigningSource(
        cpmRunningContext db,
        ChangeOrderPdfBuilder pdf,
        FacadeCore.IDocumentService documents,
        DocStorageService storage,
        ILogger<ChangeOrderSigningSource> logger)
    {
        _db = db;
        _pdf = pdf;
        _documents = documents;
        _storage = storage;
        _logger = logger;
    }

    public string DocumentType => Key;
    public string DisplayName => "Wijzigingsopdracht";

    public async Task<SigningDocumentPackage> BuildAsync(int sourceEntityId, int byUserId, CancellationToken ct = default)
    {
        var model = await _pdf.LoadAsync(sourceEntityId, ct)
                    ?? throw new InvalidOperationException($"Wijzigingsopdracht {sourceEntityId} bestaat niet.");
        // Het ondertekendossier gebruikt de gl-v2-documentlayout (Niels, 2026-10-06); buiten een HTTP-context bestaat
        // geen cookie om op te beslissen, dus altijd gl-v2. Enkel nieuwe dossiers: bestaande blijven wat ze waren.
        var pdf = _pdf.Render(model, useGlV2Layout: true);

        var parties = await SuggestPartiesAsync(model, ct);

        return new SigningDocumentPackage(
            Pdf: pdf,
            FileName: ChangeOrderPdfBuilder.FileName(model),
            Title: $"Wijzigingsopdracht {model.Reference} — {model.ProjectName}",
            DocumentNumber: model.Reference,
            Summary: string.IsNullOrWhiteSpace(model.Subject) ? null : model.Subject.Trim(),   // onderwerp, niet de omschrijving voor de klant
            AmountExclVat: Math.Round(model.TotalExcl, 2, MidpointRounding.AwayFromZero),
            VatAmount: Math.Round(model.VatAmount, 2, MidpointRounding.AwayFromZero),
            AmountInclVat: Math.Round(model.TotalIncl, 2, MidpointRounding.AwayFromZero),
            ProjectId: model.ProjectId > 0 ? model.ProjectId : null,
            ClientAccountId: model.ClientAccountId,
            SuggestedParties: parties,
            // Standaardvoorkeur van het klantenaccount (migratie 057, "WO ondertekenen door" op
            // Klanten/AddClientAccountV2 en EditProjectV2) — enkel een VOORSTEL: de gebruiker past het op
            // dit startscherm nog aan, en de voltooiingsregel (SigningRuleEvaluator) verandert niet.
            SuggestedRule: await SuggestedRuleAsync(model.ClientAccountId, ct),
            Attachments: Array.Empty<SigningAttachmentInput>());
    }

    /// <summary>Voorstel: eerst het klantaccount (het contract­adres), daarna elke mede-eigenaar uit de
    /// contacten met een eigen e-mailadres dat nog niet in de lijst zit. Wie geen e-mail heeft komt
    /// tóch in het voorstel — het startscherm toont dan een waarschuwing en laat het aanvullen —
    /// want stil weglaten zou een mede-eigenaar doen verdwijnen.</summary>
    private async Task<int> SuggestedRuleAsync(int clientAccountId, CancellationToken ct)
    {
        var rule = await _db.ClientAccount.AsNoTracking().Where(a => a.Id == clientAccountId)
            .Select(a => a.DefaultSigningRule).FirstOrDefaultAsync(ct);
        return rule.HasValue ? rule.Value : (int)SigningRule.All;
    }

    public async Task<IReadOnlyList<SigningPartyInput>> SuggestPartiesAsync(Documents.ChangeOrderPdfModel model, CancellationToken ct)
    {
        var list = new List<SigningPartyInput>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 0;

        var account = await _db.ClientAccount.AsNoTracking().FirstOrDefaultAsync(a => a.Id == model.ClientAccountId, ct);
        if (account is not null)
        {
            // account.Cellphone (migratie 058) i.p.v. hardcoded null: SmsOtpMethod.ResolveDestinationAsync
            // gebruikt dat nummer al als OTP-bestemming voor eigenaar 1 — het startscherm moet 'm dan ook tonen.
            list.Add(new SigningPartyInput((int)SigningPartyType.ClientAccount, account.Id, ChangeOrderPdfBuilder.DisplayName(account),
                account.Email?.Trim(), SigningCrypto.MaskPhone(account.Cellphone), "Klant", order++));
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
            // Achternaam eerst, zelfde volgorde als eigenaar 1 hierboven (ChangeOrderPdfBuilder.DisplayName)
            // en ClientAccountBO.DisplayName — anders staan eigenaar 1 en de mede-eigenaars in één en
            // dezelfde ondertekenaarslijst in een verschillende naamvolgorde.
            var name = string.Join(" ", new[] { c.Name, c.Forename }.Where(s => !string.IsNullOrWhiteSpace(s)));
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
        var co = await _db.ChangeOrder
            .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Project)
            .FirstOrDefaultAsync(c => c.Id == sourceEntityId, ct);
        if (co is null) return;
        co.DateAgreement = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(completedAtUtc, Brussels));
        co.DateSendToClient ??= co.DateAgreement;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Wijzigingsopdracht {ChangeOrderId} kreeg akkoorddatum {Date} via ondertekendossier {CaseId}.", sourceEntityId, co.DateAgreement, caseId);

        await ArchiveSignedDocumentAsync(co, caseId, ct);
    }

    /// <summary>Legt het ondertekende PDF vast als ProjectDocs-revisie (ONDERTEKENEN_VOORTGANG.md,
    /// keuze 5) — best effort: mag de akkoorddatum hierboven nooit blokkeren of ongedaan maken.</summary>
    private async Task ArchiveSignedDocumentAsync(ChangeOrder co, int caseId, CancellationToken ct)
    {
        try
        {
            var signingCase = await _db.SigningCase.AsNoTracking().FirstOrDefaultAsync(c => c.Id == caseId, ct);
            if (signingCase?.FinalDocumentId is not int finalDocId)
            {
                _logger.LogWarning("Dossier {CaseId} is voltooid maar heeft geen definitief document; geen ProjectDocs-revisie.", caseId);
                return;
            }
            var finalDoc = await _db.SigningDocument.AsNoTracking().FirstOrDefaultAsync(d => d.Id == finalDocId, ct);
            if (finalDoc?.Content is null || !SigningCrypto.FixedTimeEqualsHex(SigningCrypto.Sha256Hex(finalDoc.Content), finalDoc.Sha256))
            {
                _logger.LogError("Definitief document {DocumentId} van dossier {CaseId} ontbreekt of komt niet overeen met zijn hash; geen ProjectDocs-revisie.", finalDocId, caseId);
                return;
            }

            var stored = await _storage.UploadAsync(finalDoc.Content, finalDoc.FileName, finalDoc.ContentType, "docs");
            if (stored is null)
            {
                _logger.LogWarning("Ondertekend PDF van dossier {CaseId} kon niet naar de Storage API weggeschreven worden.", caseId);
                return;
            }

            // ChangeOrderId (migratie 054) blijft in gebruik als koppeling — enkel DocumentSignatures wordt uitgefaseerd.
            var existing = await _db.ProjectDocs.FirstOrDefaultAsync(d => d.ChangeOrderId == co.Id, ct);
            var dto = new FacadeCore.DocUploadDto
            {
                ProjectId = co.ContractActivity?.Contract?.Project?.ProjectId ?? existing?.ProjectId ?? 0,
                Mode = existing is null ? "new" : "revision",
                DocumentId = existing?.Id,
                FolderId = existing is null ? await _db.DocumentFolders.Where(f => f.Code == "contracten").Select(f => (int?)f.Id).FirstOrDefaultAsync(ct) : null,
                Name = existing is null ? finalDoc.FileName : null,
                Number = existing is null ? co.PublicNumber : null,
                Links = existing is null
                    ? new List<FacadeCore.DocLinkRef> { new() { Type = "client", Id = co.ClientAccountId } }
                    : new List<FacadeCore.DocLinkRef>(),
                Status = DocumentStatus.Goedgekeurd,
                UploaderKind = DocumentUploaderKind.Intern,
                StoredFilename = stored,
                OriginalFilename = finalDoc.FileName,
                SizeBytes = finalDoc.ByteLength,
                Note = "Ondertekende versie (elektronische ondertekening)",
                UserName = "Systeem (elektronische ondertekening)",
            };

            var result = await _documents.Upload(dto);
            if (!result.Ok || result.Id is not int docId)
            {
                _logger.LogWarning("ProjectDocs-revisie voor dossier {CaseId} kon niet geschreven worden: {Message}", caseId, result.Message);
                return;
            }
            if (existing is null)
            {
                var doc = await _db.ProjectDocs.FirstAsync(d => d.Id == docId, ct);
                doc.ChangeOrderId = co.Id;
                await _db.SaveChangesAsync(ct);
            }
            _logger.LogInformation("Ondertekend PDF van dossier {CaseId} vastgelegd als ProjectDocs {DocumentId} (wijzigingsopdracht {ChangeOrderId}).", caseId, docId, co.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Archiveren van het ondertekende PDF naar Documenten faalde voor dossier {CaseId}.", caseId);
        }
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
