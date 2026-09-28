using System.Data;
using BOCore;
using DALCore.Models;
using FacadeCore.Signing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceCore.Signing;

/// <summary>
/// De kern van de signingmodule (ONDERTEKENEN_VOORSTEL.md §4.1, §5): aanmaken, aanbieden, opvolgen
/// en afsluiten van dossiers, plus alles wat de ondertekenaar via zijn sessie doet. Kent geen HTTP.
/// Principes: bewijs is server-side (§2.2), alles is een snapshot (§2.3), elke overgang is een
/// event (§2.4), geheimen komen hier alleen als parameter voorbij en worden nooit gelogd (§2.5),
/// en elke fout richting ondertekenaar is dezelfde neutrale boodschap (§2.6).
/// </summary>
public sealed class SigningService : ISigningService
{
    private const string GenericError = "Deze ondertekenlink is ongeldig of niet meer beschikbaar.";

    private readonly cpmRunningContext _db;
    private readonly SigningRegistry _registry;
    private readonly ISigningEvidenceStore _evidence;
    private readonly IAssetStorageClient _storage;
    private readonly ISigningNotifier _notifier;
    private readonly ISigningDocumentRenderer _renderer;
    private readonly SigningOptions _options;
    private readonly ILogger<SigningService> _logger;

    public SigningService(
        cpmRunningContext db,
        SigningRegistry registry,
        ISigningEvidenceStore evidence,
        IAssetStorageClient storage,
        ISigningNotifier notifier,
        ISigningDocumentRenderer renderer,
        IOptions<SigningOptions> options,
        ILogger<SigningService> logger)
    {
        _db = db;
        _registry = registry;
        _evidence = evidence;
        _storage = storage;
        _notifier = notifier;
        _renderer = renderer;
        _options = options.Value;
        _logger = logger;
    }

    private static DateTime Now => DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Intern beheer
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    public async Task<SigningOperationResult> CreateCaseAsync(CreateSigningCaseRequest request, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var source = _registry.TrySource(request.DocumentType);
        if (source is null) return SigningOperationResult.Fail($"Onbekend documenttype '{request.DocumentType}'.");

        var policy = await _db.SigningPolicy.AsNoTracking()
            .FirstOrDefaultAsync(p => p.DocumentType == request.DocumentType && p.IsActive, ct);
        if (policy is null) return SigningOperationResult.Fail($"Geen actief ondertekenbeleid voor '{request.DocumentType}'.");

        var alreadyActive = await _db.SigningCase.AnyAsync(c =>
            c.DocumentType == request.DocumentType && c.SourceEntityId == request.SourceEntityId
            && (c.Status == (int)SigningCaseStatus.Draft || c.Status == (int)SigningCaseStatus.Open), ct);
        if (alreadyActive) return SigningOperationResult.Fail("Er loopt al een ondertekeningsprocedure voor dit document. Annuleer die eerst.");

        // Beide moeten bestaan vóór er iets bewaard wordt: een dossier dat naar een onbekende
        // methode verwijst zou pas bij het ondertekenen ontploffen.
        _registry.Provider(policy.SignatureMethod);
        if (policy.OtpRequired)
        {
            if (string.IsNullOrWhiteSpace(policy.VerificationMethod))
                return SigningOperationResult.Fail("Het beleid vereist OTP maar noemt geen verificatiemethode.");
            _registry.Verification(policy.VerificationMethod);
        }

        SigningDocumentPackage package;
        try
        {
            package = await source.BuildAsync(request.SourceEntityId, request.ByUserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Documentbron {DocumentType} kon geen pakket bouwen voor bron {SourceId}.", request.DocumentType, request.SourceEntityId);
            return SigningOperationResult.Fail("Het document kon niet opgebouwd worden.");
        }
        if (package.Pdf is null || package.Pdf.Length == 0)
            return SigningOperationResult.Fail("Het document leverde een lege PDF op.");

        // Vingerafdruk van de bron, ná het bouwen van de PDF: wijzigt de bron daarna nog (ondanks
        // de vergrendeling), dan weigert SignAsync de handtekening.
        string? fingerprint = null;
        try { fingerprint = await source.ComputeFingerprintAsync(request.SourceEntityId, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Documentbron {DocumentType} kon geen vingerafdruk berekenen voor bron {SourceId}.", request.DocumentType, request.SourceEntityId); }

        var parties = (request.Parties.Count > 0 ? request.Parties : package.SuggestedParties).ToList();
        if (parties.Count == 0) return SigningOperationResult.Fail("Er is geen enkele ondertekenaar opgegeven.");
        foreach (var p in parties)
        {
            if (string.IsNullOrWhiteSpace(p.DisplayName)) return SigningOperationResult.Fail("Elke ondertekenaar heeft een naam nodig.");
            if (p.PartyType != (int)SigningPartyType.InternalUser && !IsPlausibleEmail(p.Email))
                return SigningOperationResult.Fail($"Ondertekenaar '{p.DisplayName}' heeft geen geldig e-mailadres. Vul het aan bij de klant of het contact en probeer opnieuw.");
        }

        var rule = request.SigningRule ?? package.SuggestedRule ?? policy.SigningRule;
        if (!Enum.IsDefined(typeof(SigningRule), rule)) return SigningOperationResult.Fail("Ongeldige ondertekenregel.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var signingCase = new SigningCase
        {
            PublicVerificationId = Guid.NewGuid(),
            DocumentType = request.DocumentType,
            SourceEntityId = request.SourceEntityId,
            ProjectId = package.ProjectId,
            ClientAccountId = package.ClientAccountId,
            Title = Truncate(package.Title, 300)!,
            DocumentNumber = Truncate(package.DocumentNumber, 50),
            Summary = package.Summary,
            AmountExclVat = package.AmountExclVat,
            VatAmount = package.VatAmount,
            AmountInclVat = package.AmountInclVat,
            Status = (int)SigningCaseStatus.Draft,
            SigningRule = rule,
            SignatureMethod = policy.SignatureMethod,
            OtpRequired = policy.OtpRequired,
            VerificationMethod = policy.OtpRequired ? policy.VerificationMethod : null,
            OtpValiditySeconds = policy.OtpValiditySeconds,
            OtpMaxAttempts = policy.OtpMaxAttempts,
            ConsentTextSnapshot = policy.ConsentText,
            SourceFingerprint = Truncate(fingerprint, 64),
            CreatedByUserId = request.ByUserId,
            CreatedAt = Now,
            ExpiresAt = request.ExpiresAt,
        };
        _db.SigningCase.Add(signingCase);
        await _db.SaveChangesAsync(ct);

        var original = await StoreDocumentAsync(signingCase.Id, null, SigningDocumentKind.Original,
            package.FileName, "application/pdf", package.Pdf, request.ByUserId, ctx, ct);
        signingCase.OriginalDocumentId = original.Id;

        foreach (var attachment in package.Attachments)
        {
            await StoreDocumentAsync(signingCase.Id, null, SigningDocumentKind.Attachment,
                attachment.FileName, attachment.ContentType, attachment.Content, request.ByUserId, ctx, ct);
        }

        var order = 0;
        foreach (var p in parties.OrderBy(p => p.SortOrder))
        {
            _db.SigningParty.Add(new SigningParty
            {
                SigningCaseId = signingCase.Id,
                SortOrder = order++,
                PartyType = p.PartyType,
                SourceRefId = p.SourceRefId,
                DisplayName = Truncate(p.DisplayName, 200)!,
                Email = Truncate(p.Email?.Trim(), 256),
                PhoneMasked = Truncate(p.PhoneMasked, 40),
                Capacity = Truncate(p.Capacity, 200),
                Status = (int)SigningPartyStatus.Pending,
                PartyVerificationId = Guid.NewGuid(),
                ConsentTextSnapshot = policy.ConsentText,
            });
        }
        await _db.SaveChangesAsync(ct);

        await _evidence.AppendAsync(new SigningEventDraft(signingCase.Id, null, SigningEventTypes.CaseCreated,
            (int)SigningActorType.Internal, request.ByUserId, request.ByUserLabel ?? ctx.ActorLabel, ctx.Ip, ctx.UserAgent, original.Sha256,
            new { documentType = request.DocumentType, sourceEntityId = request.SourceEntityId, rule, parties = parties.Count, signatureMethod = policy.SignatureMethod, otpRequired = policy.OtpRequired }), ct);

        await tx.CommitAsync(ct);
        return SigningOperationResult.Ok(signingCase.Id);
    }

    public async Task<SigningOperationResult> OpenCaseAsync(int caseId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var signingCase = await LoadCaseAsync(caseId, ct);
        if (signingCase is null) return SigningOperationResult.Fail("Dossier niet gevonden.");
        if (signingCase.Status != (int)SigningCaseStatus.Draft) return SigningOperationResult.Fail("Dit dossier is al aangeboden of afgesloten.");

        var source = _registry.Source(signingCase.DocumentType);
        var provider = _registry.Provider(signingCase.SignatureMethod);
        var policy = await _db.SigningPolicy.AsNoTracking().FirstOrDefaultAsync(p => p.DocumentType == signingCase.DocumentType, ct);
        var linkDays = policy?.LinkValidityDays ?? 30;

        var start = await provider.StartAsync(ToView(signingCase), ct);
        if (!start.Success) return SigningOperationResult.Fail(start.Error ?? "De ondertekenmethode kon niet gestart worden.");

        signingCase.Status = (int)SigningCaseStatus.Open;
        signingCase.OpenedAt = Now;
        signingCase.ExpiresAt ??= Now.AddDays(linkDays);
        signingCase.ProviderCaseRef = start.ProviderCaseRef;
        await _db.SaveChangesAsync(ct);

        await _evidence.AppendAsync(Draft(signingCase.Id, null, SigningEventTypes.CaseOpened, ctx, signingCase.OriginalDocument?.Sha256,
            new { expiresAt = signingCase.ExpiresAt, rule = signingCase.SigningRule }), ct);

        try { await source.OnCaseOpenedAsync(signingCase.SourceEntityId, signingCase.Id, ct); }
        catch (Exception ex) { _logger.LogError(ex, "Bron {DocumentType}/{SourceId} kon niet vergrendeld worden.", signingCase.DocumentType, signingCase.SourceEntityId); }

        var outcome = SigningRuleEvaluator.Evaluate((SigningRule)signingCase.SigningRule, RuleStates(signingCase));
        foreach (var partyId in outcome.PartiesToInvite)
        {
            var party = signingCase.Parties.First(p => p.Id == partyId);
            await InvitePartyAsync(signingCase, party, SigningTokenPurpose.Invite, ctx, ct);
        }

        return SigningOperationResult.Ok(signingCase.Id);
    }

    public async Task<SigningOperationResult> CancelCaseAsync(int caseId, string reason, SigningRequestContext ctx, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return SigningOperationResult.Fail("Een reden is verplicht.");
        var signingCase = await LoadCaseAsync(caseId, ct);
        if (signingCase is null) return SigningOperationResult.Fail("Dossier niet gevonden.");
        if (signingCase.Status is not ((int)SigningCaseStatus.Draft or (int)SigningCaseStatus.Open))
            return SigningOperationResult.Fail("Dit dossier is al afgesloten.");

        await CloseCaseAsync(signingCase, SigningCaseStatus.Cancelled, reason.Trim(), ctx,
            "Het dossier werd geannuleerd door Group LN.", SigningEventTypes.CaseCancelled, ct);
        return SigningOperationResult.Ok(signingCase.Id);
    }

    public async Task<SigningOperationResult> SendReminderAsync(int partyId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var party = await _db.SigningParty.Include(p => p.SigningCase).ThenInclude(c => c.OriginalDocument)
            .Include(p => p.SigningCase).ThenInclude(c => c.Parties)
            .FirstOrDefaultAsync(p => p.Id == partyId, ct);
        if (party is null) return SigningOperationResult.Fail("Ondertekenaar niet gevonden.");
        if (party.SigningCase.Status != (int)SigningCaseStatus.Open) return SigningOperationResult.Fail("Het dossier is niet (meer) open.");
        if (!CanBeReminded(party)) return SigningOperationResult.Fail("Deze ondertekenaar kan geen herinnering meer krijgen.");

        var sent = await InvitePartyAsync(party.SigningCase, party, SigningTokenPurpose.Reminder, ctx, ct);
        return sent ? SigningOperationResult.Ok(party.SigningCaseId) : SigningOperationResult.Fail("De herinnering kon niet verstuurd worden.");
    }

    public async Task<SigningOperationResult> RegenerateLinkAsync(int partyId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var party = await _db.SigningParty.Include(p => p.SigningCase).ThenInclude(c => c.OriginalDocument)
            .Include(p => p.SigningCase).ThenInclude(c => c.Parties)
            .Include(p => p.AccessTokens)
            .FirstOrDefaultAsync(p => p.Id == partyId, ct);
        if (party is null) return SigningOperationResult.Fail("Ondertekenaar niet gevonden.");
        if (party.SigningCase.Status != (int)SigningCaseStatus.Open) return SigningOperationResult.Fail("Het dossier is niet (meer) open.");
        if (!CanBeReminded(party)) return SigningOperationResult.Fail("Voor deze ondertekenaar kan geen nieuwe link meer gemaakt worden.");

        var revoked = RevokeTokens(party, "Nieuwe link uitgegeven");
        await _db.SaveChangesAsync(ct);
        if (revoked > 0)
            await _evidence.AppendAsync(Draft(party.SigningCaseId, party.Id, SigningEventTypes.LinkRevoked, ctx, null, new { count = revoked, reason = "Nieuwe link" }), ct);

        var sent = await InvitePartyAsync(party.SigningCase, party, SigningTokenPurpose.Regenerated, ctx, ct);
        return sent ? SigningOperationResult.Ok(party.SigningCaseId) : SigningOperationResult.Fail("De nieuwe link kon niet verstuurd worden.");
    }

    public async Task<CaseStatusView?> GetCaseStatusAsync(int caseId, CancellationToken ct = default)
    {
        var signingCase = await QueryCases().FirstOrDefaultAsync(c => c.Id == caseId, ct);
        return signingCase is null ? null : ToView(signingCase);
    }

    public async Task<CaseStatusView?> GetActiveCaseForSourceAsync(string documentType, int sourceEntityId, CancellationToken ct = default)
    {
        var signingCase = await QueryCases().FirstOrDefaultAsync(c =>
            c.DocumentType == documentType && c.SourceEntityId == sourceEntityId
            && (c.Status == (int)SigningCaseStatus.Draft || c.Status == (int)SigningCaseStatus.Open), ct);
        return signingCase is null ? null : ToView(signingCase);
    }

    public async Task<CaseStatusView?> GetCompletedCaseForSourceAsync(string documentType, int sourceEntityId, CancellationToken ct = default)
    {
        var signingCase = await QueryCases()
            .Where(c => c.DocumentType == documentType && c.SourceEntityId == sourceEntityId && c.Status == (int)SigningCaseStatus.Completed)
            .OrderByDescending(c => c.CompletedAt)
            .FirstOrDefaultAsync(ct);
        return signingCase is null ? null : ToView(signingCase);
    }

    public async Task<IReadOnlyList<CaseStatusView>> ListCasesAsync(int? projectId, int? status, int take = 200, CancellationToken ct = default)
    {
        var q = QueryCases();
        if (projectId.HasValue) q = q.Where(c => c.ProjectId == projectId);
        if (status.HasValue) q = q.Where(c => c.Status == status);
        var list = await q.OrderByDescending(c => c.CreatedAt).Take(Math.Clamp(take, 1, 1000)).ToListAsync(ct);
        return list.Select(ToView).ToList();
    }

    public Task<IReadOnlyList<SigningEventView>> GetEventsAsync(int caseId, CancellationToken ct = default)
        => _evidence.ListAsync(caseId, ct);

    public async Task<ChainVerification> VerifyAuditChainAsync(int caseId, CancellationToken ct = default)
    {
        var result = await _evidence.VerifyAsync(caseId, ct);
        var exists = await _db.SigningCase.AnyAsync(c => c.Id == caseId, ct);
        if (exists)
            await _evidence.AppendAsync(new SigningEventDraft(caseId, null, SigningEventTypes.AuditChainVerified, (int)SigningActorType.System, null, null, null, null, null,
                new { valid = result.Valid, events = result.EventCount, firstBroken = result.FirstBrokenEventId }), ct);
        return result;
    }

    public async Task<SigningDocumentContent?> GetDocumentAsync(int caseId, int documentKind, int? documentId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var q = _db.SigningDocument.AsNoTracking().Where(d => d.SigningCaseId == caseId && d.Kind == documentKind);
        if (documentId.HasValue) q = q.Where(d => d.Id == documentId);
        var doc = await q.OrderByDescending(d => d.Id).FirstOrDefaultAsync(ct);
        var content = VerifiedContent(doc);
        if (content is null) return null;
        await _evidence.AppendAsync(Draft(caseId, null, SigningEventTypes.DocumentDownloaded, ctx, doc!.Sha256, new { kind = documentKind, documentId = doc.Id, fileName = doc.FileName }), ct);
        return content;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Ondertekenaar (enkel via sessie)
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    public async Task<TokenRedeemResult> RedeemTokenAsync(string rawToken, SigningRequestContext ctx, CancellationToken ct = default)
    {
        if (!SigningCrypto.LooksLikeToken(rawToken)) return TokenRedeemResult.Invalid;
        var hash = SigningCrypto.HashToken(rawToken);

        var token = await _db.SigningAccessToken
            .Include(t => t.SigningParty).ThenInclude(p => p.SigningCase)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null)
        {
            // Geen dossier gekend → geen event mogelijk; enkel een logregel met de IP-hash.
            _logger.LogInformation("Onbekende ondertekenlink aangeboden (ip-hash {IpHash}).", SigningCrypto.HashPii(ctx.Ip));
            return TokenRedeemResult.Invalid;
        }

        var party = token.SigningParty;
        var signingCase = party.SigningCase;
        var downloadOnly = token.Purpose == (int)SigningTokenPurpose.Download;
        string? rejectReason = null;
        if (token.RevokedAt is not null) rejectReason = "revoked";
        else if (token.ExpiresAt <= Now) rejectReason = "expired";
        else if (downloadOnly && signingCase.Status != (int)SigningCaseStatus.Completed) rejectReason = "case-not-completed";
        else if (!downloadOnly && signingCase.Status != (int)SigningCaseStatus.Open) rejectReason = "case-not-open";
        else if (!downloadOnly && party.Status is (int)SigningPartyStatus.Signed or (int)SigningPartyStatus.Declined or (int)SigningPartyStatus.Revoked or (int)SigningPartyStatus.Expired) rejectReason = "party-closed";

        if (rejectReason is not null)
        {
            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.LinkRejected, ctx, null, new { reason = rejectReason, tokenId = token.Id }, SigningActorType.Party), ct);
            return TokenRedeemResult.Invalid;
        }

        token.SessionId = Guid.NewGuid();
        token.SessionIssuedAt = Now;
        token.LastUsedAt = Now;
        token.FirstUsedAt ??= Now;
        token.UseCount++;

        var firstOpen = false;
        if (!downloadOnly && party.Status == (int)SigningPartyStatus.Invited)
        {
            party.Status = (int)SigningPartyStatus.Opened;
            firstOpen = true;
        }
        if (party.FirstOpenedAt is null) { party.FirstOpenedAt = Now; firstOpen = true; }
        await _db.SaveChangesAsync(ct);

        if (firstOpen)
            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.PartyOpened, ctx, signingCase.OriginalDocumentId is null ? null : await OriginalShaAsync(signingCase.Id, ct), null, SigningActorType.Party), ct);
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.LinkOpened, ctx, null, new { tokenId = token.Id, purpose = token.Purpose, useCount = token.UseCount }, SigningActorType.Party), ct);

        return new TokenRedeemResult(true, token.SessionId, party.Id, signingCase.Id, downloadOnly);
    }

    public async Task<SigningSessionView?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(sessionId, allowDownload: true, ct);
        if (s is null) return null;
        var (token, party, signingCase) = s.Value;

        var source = _registry.TrySource(signingCase.DocumentType);
        var projectName = source is null ? null : await SafeProjectNameAsync(source, signingCase.SourceEntityId, ct);
        var provider = _registry.Provider(signingCase.SignatureMethod);

        var latest = await _db.SigningVerification.AsNoTracking()
            .Where(v => v.SigningPartyId == party.Id && v.SessionId == sessionId)
            .OrderByDescending(v => v.Id).FirstOrDefaultAsync(ct);
        var pending = latest is not null && latest.Status is (int)SigningVerificationStatus.Sent or (int)SigningVerificationStatus.Accepted or (int)SigningVerificationStatus.Delivered && latest.ExpiresAt > Now;
        var verified = await HasFreshVerificationAsync(party.Id, sessionId, ct);

        string? destinationMasked = latest?.DestinationMasked;
        if (destinationMasked is null && signingCase.OtpRequired && signingCase.VerificationMethod is not null)
        {
            var method = _registry.Verification(signingCase.VerificationMethod);
            var dest = await method.ResolveDestinationAsync(party.PartyType, party.SourceRefId, party.Email, ct);
            destinationMasked = dest.Masked;
        }

        var attachments = await _db.SigningDocument.AsNoTracking()
            .Where(d => d.SigningCaseId == signingCase.Id && d.Kind == (int)SigningDocumentKind.Attachment)
            .OrderBy(d => d.Id)
            .Select(d => new SigningAttachmentView(d.Id, d.FileName, d.ContentType, d.ByteLength))
            .ToListAsync(ct);

        return new SigningSessionView(
            signingCase.Id, party.Id, signingCase.Status, party.Status,
            source?.DisplayName ?? signingCase.DocumentType,
            signingCase.Title, signingCase.DocumentNumber, signingCase.Summary, projectName,
            signingCase.AmountExclVat, signingCase.VatAmount, signingCase.AmountInclVat,
            party.DisplayName, party.Capacity,
            party.ConsentTextSnapshot ?? signingCase.ConsentTextSnapshot ?? string.Empty,
            signingCase.OtpRequired, signingCase.VerificationMethod, destinationMasked,
            pending, pending ? latest!.ExpiresAt : null, verified,
            signingCase.ExpiresAt,
            provider.Capabilities.HostedUi, null,
            attachments,
            token.Purpose == (int)SigningTokenPurpose.Download,
            signingCase.FinalDocumentId is not null);
    }

    public async Task<SigningDocumentContent?> GetSessionDocumentAsync(Guid sessionId, int? attachmentDocumentId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(sessionId, allowDownload: true, ct);
        if (s is null) return null;
        var (_, party, signingCase) = s.Value;

        SigningDocument? doc = attachmentDocumentId is int attId
            ? await _db.SigningDocument.AsNoTracking().FirstOrDefaultAsync(d => d.Id == attId && d.SigningCaseId == signingCase.Id && d.Kind == (int)SigningDocumentKind.Attachment, ct)
            : await _db.SigningDocument.AsNoTracking().FirstOrDefaultAsync(d => d.Id == signingCase.OriginalDocumentId, ct);
        var content = VerifiedContent(doc);
        if (content is null) return null;

        // Eén DocumentServed per sessie per bestand — een PDF-viewer vraagt het bestand soms meermaals op.
        var marker = $"\"sessionId\":\"{sessionId}\"";
        var already = await _db.SigningEvent.AsNoTracking().AnyAsync(e => e.SigningPartyId == party.Id && e.EventType == SigningEventTypes.DocumentServed && e.DataJson != null && e.DataJson.Contains(marker) && e.DocumentSha256 == doc!.Sha256, ct);
        if (!already)
            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.DocumentServed, ctx, doc!.Sha256, new { sessionId, documentId = doc.Id, kind = doc.Kind }, SigningActorType.Party), ct);
        return content;
    }

    public async Task<SigningDocumentContent?> GetSessionFinalDocumentAsync(Guid sessionId, bool auditReport, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(sessionId, allowDownload: true, ct);
        if (s is null) return null;
        var (_, party, signingCase) = s.Value;
        if (signingCase.Status != (int)SigningCaseStatus.Completed) return null;
        var docId = auditReport ? signingCase.AuditReportDocumentId : signingCase.FinalDocumentId;
        if (docId is null) return null;
        var doc = await _db.SigningDocument.AsNoTracking().FirstOrDefaultAsync(d => d.Id == docId, ct);
        var content = VerifiedContent(doc);
        if (content is null) return null;
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.DocumentDownloaded, ctx, doc!.Sha256, new { sessionId, documentId = doc.Id, kind = doc.Kind }, SigningActorType.Party), ct);
        return content;
    }

    public async Task RecordDocumentViewedAsync(Guid sessionId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(sessionId, allowDownload: false, ct);
        if (s is null) return;
        var (_, party, signingCase) = s.Value;
        var marker = $"\"sessionId\":\"{sessionId}\"";
        var already = await _db.SigningEvent.AsNoTracking().AnyAsync(e => e.SigningPartyId == party.Id && e.EventType == SigningEventTypes.DocumentViewed && e.DataJson != null && e.DataJson.Contains(marker), ct);
        if (already) return;
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.DocumentViewed, ctx, await OriginalShaAsync(signingCase.Id, ct), new { sessionId, note = "client-signaal; DocumentServed is het serverbewijs" }, SigningActorType.Party), ct);
    }

    public async Task<VerificationRequestResult> RequestVerificationAsync(Guid sessionId, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(sessionId, allowDownload: false, ct);
        if (s is null) return new VerificationRequestResult(false, null, null, GenericError);
        var (_, party, signingCase) = s.Value;
        if (signingCase.Status != (int)SigningCaseStatus.Open || !IsPartyOpenForSigning(party))
            return new VerificationRequestResult(false, null, null, GenericError);
        if (!signingCase.OtpRequired || string.IsNullOrWhiteSpace(signingCase.VerificationMethod))
            return new VerificationRequestResult(false, null, null, "Voor dit document is geen verificatiecode nodig.");

        // Rate limit per ondertekenaar (§6.4): max. 3 aanvragen per 10 minuten, los van de IP-limiet in de middleware.
        var since = Now.AddMinutes(-10);
        var recent = await _db.SigningVerification.CountAsync(v => v.SigningPartyId == party.Id && v.RequestedAt >= since, ct);
        if (recent >= 3) return new VerificationRequestResult(false, null, null, "U vroeg net al een code aan. Wacht enkele minuten en probeer opnieuw.");

        var method = _registry.Verification(signingCase.VerificationMethod);
        var channel = _registry.Channel(method.ChannelKey);
        if (!channel.IsConfigured) return new VerificationRequestResult(false, null, null, "Deze verificatiemethode is momenteel niet beschikbaar.");

        var destination = await method.ResolveDestinationAsync(party.PartyType, party.SourceRefId, party.Email, ct);
        if (!destination.Found || destination.Destination is null)
            return new VerificationRequestResult(false, null, null, "Er is geen bestemming gekend om de code naartoe te sturen. Neem contact op met Group LN.");

        // Vorige, nog open codes van deze ondertekenaar vervallen.
        var open = await _db.SigningVerification.Where(v => v.SigningPartyId == party.Id
            && (v.Status == (int)SigningVerificationStatus.Sent || v.Status == (int)SigningVerificationStatus.Accepted || v.Status == (int)SigningVerificationStatus.Delivered)).ToListAsync(ct);
        foreach (var v in open) v.Status = (int)SigningVerificationStatus.Superseded;

        var code = SigningCrypto.GenerateOtp(_options.OtpLength);
        var requestedAt = Now;
        var validity = TimeSpan.FromSeconds(Math.Max(60, signingCase.OtpValiditySeconds));
        var verification = new SigningVerification
        {
            SigningPartyId = party.Id,
            Method = method.Key,
            ChannelKey = channel.Key,
            ProviderKey = channel.ProviderKey,
            DestinationMasked = destination.Masked,
            CodeHmac = SigningCrypto.ComputeOtpHmac(_options.GetOtpHmacKeyBytes(), signingCase.Id, party.Id, requestedAt, code),
            SessionId = sessionId,
            RequestedAt = requestedAt,
            RequestedIp = Truncate(ctx.Ip, 45),
            ExpiresAt = requestedAt.Add(validity),
            MaxAttempts = Math.Max(1, signingCase.OtpMaxAttempts),
            Status = (int)SigningVerificationStatus.Sent,
        };
        _db.SigningVerification.Add(verification);
        await _db.SaveChangesAsync(ct);
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.VerificationRequested, ctx, null,
            new { verificationId = verification.Id, method = method.Key, channel = channel.Key, provider = channel.ProviderKey, destination = destination.Masked, sessionId }, SigningActorType.Party), ct);

        var mailDoc = await MailDocumentAsync(signingCase, ct);
        var receipt = await channel.SendAsync(method.BuildMessage(destination.Destination, code, validity, mailDoc), ct);
        // De code zelf bestaat vanaf hier nergens meer.

        if (receipt.Accepted)
        {
            verification.Status = (int)SigningVerificationStatus.Accepted;
            verification.ProviderMessageId = Truncate(receipt.ProviderMessageId, 200);
            verification.ProviderStatus = Truncate(receipt.Status, 100);
            verification.ProviderAcceptedAt = receipt.AcceptedAt;
            await _db.SaveChangesAsync(ct);
            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.VerificationMessageAccepted, ctx, null,
                new { verificationId = verification.Id, provider = receipt.ProviderKey, providerMessageId = receipt.ProviderMessageId, status = receipt.Status }, SigningActorType.Provider), ct);
            return new VerificationRequestResult(true, destination.Masked, verification.ExpiresAt, null);
        }

        verification.Status = (int)SigningVerificationStatus.Failed;
        verification.ProviderStatus = Truncate(receipt.Status, 100);
        await _db.SaveChangesAsync(ct);
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.VerificationMessageFailed, ctx, null,
            new { verificationId = verification.Id, provider = receipt.ProviderKey, status = receipt.Status, error = receipt.Error }, SigningActorType.Provider), ct);
        return new VerificationRequestResult(false, destination.Masked, null, "De verificatiecode kon niet verstuurd worden. Probeer het straks opnieuw.");
    }

    public async Task<VerificationResult> VerifyCodeAsync(Guid sessionId, string code, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(sessionId, allowDownload: false, ct);
        if (s is null) return new VerificationResult(false, false, null, GenericError);
        var (_, party, signingCase) = s.Value;
        if (signingCase.Status != (int)SigningCaseStatus.Open || !IsPartyOpenForSigning(party))
            return new VerificationResult(false, false, null, GenericError);

        // Rate limit op controleren (§6.4): max. 10 pogingen per 10 minuten per ondertekenaar, over alle codes heen.
        var since = Now.AddMinutes(-10);
        var attemptsRecent = await _db.SigningVerification.Where(v => v.SigningPartyId == party.Id && v.RequestedAt >= since).SumAsync(v => v.AttemptCount, ct);
        if (attemptsRecent >= 10) return new VerificationResult(false, true, 0, "Te veel pogingen. Vraag een nieuwe code aan.");

        var verification = await _db.SigningVerification
            .Where(v => v.SigningPartyId == party.Id && v.SessionId == sessionId
                && (v.Status == (int)SigningVerificationStatus.Sent || v.Status == (int)SigningVerificationStatus.Accepted || v.Status == (int)SigningVerificationStatus.Delivered))
            .OrderByDescending(v => v.Id).FirstOrDefaultAsync(ct);
        if (verification is null) return new VerificationResult(false, false, null, "Er is geen geldige code. Vraag een nieuwe code aan.");

        if (verification.ExpiresAt <= Now)
        {
            verification.Status = (int)SigningVerificationStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return new VerificationResult(false, false, null, "De code is verlopen. Vraag een nieuwe code aan.");
        }

        verification.AttemptCount++;
        var expected = SigningCrypto.ComputeOtpHmac(_options.GetOtpHmacKeyBytes(), signingCase.Id, party.Id, verification.RequestedAt, code);
        if (!SigningCrypto.FixedTimeEqualsHex(expected, verification.CodeHmac))
        {
            var left = verification.MaxAttempts - verification.AttemptCount;
            if (left <= 0)
            {
                verification.Status = (int)SigningVerificationStatus.Failed;
                await _db.SaveChangesAsync(ct);
                await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.VerificationLocked, ctx, null, new { verificationId = verification.Id, attempts = verification.AttemptCount }, SigningActorType.Party), ct);
                return new VerificationResult(false, true, 0, "Te veel foute pogingen. Vraag een nieuwe code aan.");
            }
            await _db.SaveChangesAsync(ct);
            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.VerificationFailedAttempt, ctx, null, new { verificationId = verification.Id, attempt = verification.AttemptCount, left }, SigningActorType.Party), ct);
            return new VerificationResult(false, false, left, $"De code klopt niet. Nog {left} poging{(left == 1 ? "" : "en")}.");
        }

        verification.Status = (int)SigningVerificationStatus.Verified;
        verification.VerifiedAt = Now;
        verification.VerifiedIp = Truncate(ctx.Ip, 45);
        party.Status = (int)SigningPartyStatus.Verified;
        party.VerifiedAt = Now;
        await _db.SaveChangesAsync(ct);
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.VerificationSucceeded, ctx, null,
            new { verificationId = verification.Id, method = verification.Method, channel = verification.ChannelKey, destination = verification.DestinationMasked, sessionId }, SigningActorType.Party), ct);
        return new VerificationResult(true, false, null, null);
    }

    public async Task<SignResult> SignAsync(SignRequest request, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var s = await ResolveSessionAsync(request.SessionId, allowDownload: false, ct);
        if (s is null) return new SignResult(false, false, false, GenericError);
        var (_, partyRef, _) = s.Value;

        // Vers en getrackt laden: dit is de ene overgang die onder een transactie met RowVersion moet.
        var signingCase = await LoadCaseAsync(partyRef.SigningCaseId, ct);
        var party = signingCase?.Parties.FirstOrDefault(p => p.Id == partyRef.Id);
        if (signingCase is null || party is null) return new SignResult(false, false, false, GenericError);

        // Idempotentie (§6.7): dezelfde sessie die dezelfde POST herhaalt krijgt hetzelfde antwoord.
        if (party.Status == (int)SigningPartyStatus.Signed && string.Equals(party.SignIdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal))
            return new SignResult(true, true, signingCase.Status == (int)SigningCaseStatus.Completed, null);

        if (signingCase.Status != (int)SigningCaseStatus.Open) return new SignResult(false, false, false, GenericError);
        if (signingCase.ExpiresAt is not null && signingCase.ExpiresAt <= Now) return new SignResult(false, false, false, GenericError);
        if (!IsPartyOpenForSigning(party)) return new SignResult(false, false, false, GenericError);
        if (!request.ConsentAccepted) return new SignResult(false, false, false, "U moet de akkoordverklaring aanvaarden om te ondertekenen.");

        SigningVerification? verification = null;
        if (signingCase.OtpRequired)
        {
            if (party.Status != (int)SigningPartyStatus.Verified) return new SignResult(false, false, false, "Verifieer eerst uw identiteit met de verificatiecode.");
            var freshSince = Now.AddMinutes(-_options.SigningSessionMinutes);
            verification = await _db.SigningVerification
                .Where(v => v.SigningPartyId == party.Id && v.SessionId == request.SessionId && v.Status == (int)SigningVerificationStatus.Verified && v.ConsumedAt == null && v.VerifiedAt >= freshSince)
                .OrderByDescending(v => v.Id).FirstOrDefaultAsync(ct);
            if (verification is null) return new SignResult(false, false, false, "Uw verificatie is verlopen. Vraag een nieuwe code aan.");
        }

        var original = await _db.SigningDocument.AsNoTracking().FirstOrDefaultAsync(d => d.Id == signingCase.OriginalDocumentId, ct);
        if (original?.Content is null || !SigningCrypto.FixedTimeEqualsHex(SigningCrypto.Sha256Hex(original.Content), original.Sha256))
        {
            _logger.LogCritical("Origineel document van dossier {CaseId} ontbreekt of komt niet overeen met zijn hash.", signingCase.Id);
            return new SignResult(false, false, false, GenericError);
        }

        var source = _registry.Source(signingCase.DocumentType);
        if (!string.IsNullOrEmpty(signingCase.SourceFingerprint))
        {
            string? current = null;
            try { current = await source.ComputeFingerprintAsync(signingCase.SourceEntityId, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Vingerafdruk van bron {DocumentType}/{SourceId} kon niet herberekend worden.", signingCase.DocumentType, signingCase.SourceEntityId); }
            if (!SigningCrypto.FixedTimeEqualsHex(current, signingCase.SourceFingerprint))
            {
                _logger.LogWarning("Bron {DocumentType}/{SourceId} is gewijzigd sinds het aanbieden; ondertekening geweigerd.", signingCase.DocumentType, signingCase.SourceEntityId);
                return new SignResult(false, false, false, "Dit document is intussen gewijzigd. Group LN moet een nieuwe ondertekeningsprocedure starten.");
            }
        }

        if (request.SignatureImagePng is not null && !IsAcceptablePng(request.SignatureImagePng))
            return new SignResult(false, false, false, "De handtekening kon niet gelezen worden. Teken ze opnieuw.");

        var provider = _registry.Provider(signingCase.SignatureMethod);
        var now = Now;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            party.Status = (int)SigningPartyStatus.Signed;
            party.SignedAt = now;
            party.SignedIp = Truncate(ctx.Ip, 45);
            party.SignedUserAgent = Truncate(ctx.UserAgent, 500);
            party.SignIdempotencyKey = Truncate(request.IdempotencyKey, 100);
            party.ConsentAcceptedAt = now;
            party.ConsentTextSnapshot ??= signingCase.ConsentTextSnapshot;
            if (verification is not null)
            {
                verification.Status = (int)SigningVerificationStatus.Consumed;
                verification.ConsumedAt = now;
            }
            await _db.SaveChangesAsync(ct);   // RowVersion-conflict → DbUpdateConcurrencyException

            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.ConsentAccepted, ctx, original.Sha256,
                new { consentSha256 = SigningCrypto.Sha256Hex(party.ConsentTextSnapshot ?? string.Empty), sessionId = request.SessionId }, SigningActorType.Party), ct);

            if (request.SignatureImagePng is not null)
            {
                var image = await StoreDocumentAsync(signingCase.Id, party.Id, SigningDocumentKind.SignatureImage,
                    $"handtekening-{party.PartyVerificationId:N}.png", "image/png", request.SignatureImagePng, null, ctx, ct, quiet: true);
                party.SignatureImageDocumentId = image.Id;
                await _db.SaveChangesAsync(ct);
                await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.SignatureImageCaptured, ctx, original.Sha256, new { documentId = image.Id, sha256 = image.Sha256, bytes = image.ByteLength }, SigningActorType.Party), ct);
            }

            var evidence = await provider.CompleteAsync(ToView(signingCase), ToPartyView(party),
                new SignRequestEvidence(verification?.Id ?? 0, verification?.Method, verification?.VerifiedAt, now, party.ConsentTextSnapshot ?? string.Empty, original.Sha256, ctx.Ip, ctx.UserAgent), ct);
            await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.PartySigned, ctx, original.Sha256,
                new { method = evidence.MethodKey, summary = evidence.Summary, evidence = evidence.DataJson, partyVerificationId = party.PartyVerificationId, sessionId = request.SessionId }, SigningActorType.Party), ct);

            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            _logger.LogWarning("Gelijktijdige ondertekening voor partij {PartyId} geweigerd (RowVersion).", party.Id);
            return new SignResult(false, false, false, "Deze ondertekening werd al verwerkt.");
        }

        // ── Na de handtekening: regel toepassen, anderen uitnodigen/intrekken, eventueel finaliseren ──
        var completed = false;
        try
        {
            signingCase = (await LoadCaseAsync(signingCase.Id, ct))!;
            var outcome = SigningRuleEvaluator.Evaluate((SigningRule)signingCase.SigningRule, RuleStates(signingCase));
            var mailDoc = await MailDocumentAsync(signingCase, ct);

            foreach (var revokeId in outcome.PartiesToRevoke)
            {
                var other = signingCase.Parties.First(p => p.Id == revokeId);
                await _db.Entry(other).Collection(p => p.AccessTokens).LoadAsync(ct);
                RevokeTokens(other, "Iemand anders heeft al ondertekend (regel: één volstaat)");
                other.Status = (int)SigningPartyStatus.Revoked;
                await _db.SaveChangesAsync(ct);
                await _evidence.AppendAsync(Draft(signingCase.Id, other.Id, SigningEventTypes.PartyRevoked, ctx, null, new { reason = "any-rule-satisfied" }, SigningActorType.System), ct);
                await TryNotifyAsync(() => _notifier.SendLinkNoLongerValidAsync(mailDoc, new SigningMailRecipient(other.DisplayName, other.Email ?? ""), "Een andere betrokkene heeft het document al ondertekend; uw handtekening is niet meer nodig.", ct), signingCase.Id, other.Id, ct);
            }
            foreach (var inviteId in outcome.PartiesToInvite)
            {
                var next = signingCase.Parties.First(p => p.Id == inviteId);
                await InvitePartyAsync(signingCase, next, SigningTokenPurpose.Invite, ctx, ct);
            }
            if (outcome.IsComplete)
                completed = await TryFinalizeAsync(signingCase.Id, ctx, ct);

            await TryNotifyAsync(() => _notifier.SendSignedConfirmationAsync(mailDoc, new SigningMailRecipient(party.DisplayName, party.Email ?? ""), completed, ct), signingCase.Id, party.Id, ct);
        }
        catch (Exception ex)
        {
            // De handtekening zelf is al vastgelegd; wat hier misloopt herstelt de achtergrondjob (finaliseren) of de beheerder (uitnodigingen).
            _logger.LogError(ex, "Nabewerking na ondertekening van partij {PartyId} faalde.", party.Id);
        }

        return new SignResult(true, false, completed, null);
    }

    public async Task<SigningOperationResult> DeclineAsync(DeclineRequest request, SigningRequestContext ctx, CancellationToken ct = default)
    {
        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length == 0) return SigningOperationResult.Fail("Geef een reden op.");

        var s = await ResolveSessionAsync(request.SessionId, allowDownload: false, ct);
        if (s is null) return SigningOperationResult.Fail(GenericError);
        var (_, partyRef, _) = s.Value;
        var signingCase = await LoadCaseAsync(partyRef.SigningCaseId, ct);
        var party = signingCase?.Parties.FirstOrDefault(p => p.Id == partyRef.Id);
        if (signingCase is null || party is null || signingCase.Status != (int)SigningCaseStatus.Open || !IsPartyOpenForSigning(party))
            return SigningOperationResult.Fail(GenericError);

        party.Status = (int)SigningPartyStatus.Declined;
        party.DeclinedAt = Now;
        party.DeclineReason = Truncate(reason, 1000);
        await _db.Entry(party).Collection(p => p.AccessTokens).LoadAsync(ct);
        RevokeTokens(party, "Geweigerd");
        await _db.SaveChangesAsync(ct);
        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, SigningEventTypes.PartyDeclined, ctx, await OriginalShaAsync(signingCase.Id, ct), new { reason = party.DeclineReason }, SigningActorType.Party), ct);

        var mailDoc = await MailDocumentAsync(signingCase, ct);
        if (!SigningRuleEvaluator.CanStillComplete((SigningRule)signingCase.SigningRule, RuleStates(signingCase)))
        {
            await CloseCaseAsync(signingCase, SigningCaseStatus.Declined, $"Geweigerd door {party.DisplayName}: {reason}", ctx,
                "Een betrokkene heeft geweigerd te ondertekenen; het dossier is gesloten.", SigningEventTypes.PartyDeclined, ct, skipEvent: true);
        }

        var source = _registry.TrySource(signingCase.DocumentType);
        if (source is not null)
        {
            var internals = await SafeInternalRecipientsAsync(source, signingCase.SourceEntityId, ct);
            await TryNotifyAsync(() => _notifier.SendDeclinedInternalAsync(mailDoc, internals, party.DisplayName, reason, InternalUrl(signingCase.Id), ct), signingCase.Id, party.Id, ct);
        }
        return SigningOperationResult.Ok(signingCase.Id);
    }

    public async Task<PublicVerificationView?> GetPublicVerificationAsync(Guid publicVerificationId, CancellationToken ct = default)
    {
        var c = await QueryCases().FirstOrDefaultAsync(x => x.PublicVerificationId == publicVerificationId, ct);
        if (c is null) return null;
        var source = _registry.TrySource(c.DocumentType);
        return new PublicVerificationView(
            source?.DisplayName ?? c.DocumentType, c.DocumentNumber, c.Status, c.CompletedAt,
            c.Parties.Count(p => p.Status == (int)SigningPartyStatus.Signed),
            c.OriginalDocument?.Sha256, c.FinalDocument?.Sha256);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Achtergrond
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    public async Task<int> ExpireOverdueCasesAsync(CancellationToken ct = default)
    {
        var ids = await _db.SigningCase.Where(c => c.Status == (int)SigningCaseStatus.Open && c.ExpiresAt != null && c.ExpiresAt <= Now).Select(c => c.Id).ToListAsync(ct);
        var count = 0;
        foreach (var id in ids)
        {
            var signingCase = await LoadCaseAsync(id, ct);
            if (signingCase is null || signingCase.Status != (int)SigningCaseStatus.Open) continue;
            // Eerst nog proberen te finaliseren: alle handtekeningen kunnen er al staan terwijl een eerdere finalisatie faalde.
            if (SigningRuleEvaluator.Evaluate((SigningRule)signingCase.SigningRule, RuleStates(signingCase)).IsComplete)
            {
                if (await TryFinalizeAsync(id, SystemContext, ct)) { count++; continue; }
            }
            await CloseCaseAsync(signingCase, SigningCaseStatus.Expired, "Termijn verstreken", SystemContext,
                "De termijn om te ondertekenen is verstreken.", SigningEventTypes.CaseExpired, ct);
            count++;
        }
        return count;
    }

    public async Task<int> SendDueRemindersAsync(CancellationToken ct = default)
    {
        var policies = await _db.SigningPolicy.AsNoTracking().Where(p => p.IsActive && p.ReminderAfterDays != null).ToListAsync(ct);
        if (policies.Count == 0) return 0;
        var types = policies.Select(p => p.DocumentType).ToList();
        var open = await QueryCases(tracking: true)
            .Where(c => c.Status == (int)SigningCaseStatus.Open && types.Contains(c.DocumentType) && (c.ExpiresAt == null || c.ExpiresAt > Now))
            .ToListAsync(ct);

        var sent = 0;
        foreach (var signingCase in open)
        {
            var policy = policies.First(p => p.DocumentType == signingCase.DocumentType);
            foreach (var party in signingCase.Parties.Where(CanBeReminded))
            {
                if (party.ReminderCount >= policy.MaxReminders || party.InvitedAt is null) continue;
                var due = party.ReminderCount == 0
                    ? party.InvitedAt.Value.AddDays(policy.ReminderAfterDays!.Value) <= Now
                    : policy.ReminderRepeatDays is int repeat && party.LastReminderAt is not null && party.LastReminderAt.Value.AddDays(repeat) <= Now;
                if (!due) continue;
                if (await InvitePartyAsync(signingCase, party, SigningTokenPurpose.Reminder, SystemContext, ct)) sent++;
            }
        }
        return sent;
    }

    public async Task<int> ApplyRetentionScrubAsync(CancellationToken ct = default)
    {
        var due = await _db.SigningCase.Where(c => c.RetentionUntil != null && c.RetentionUntil <= Now && c.RetentionScrubbedAt == null
            && c.Status != (int)SigningCaseStatus.Draft && c.Status != (int)SigningCaseStatus.Open).ToListAsync(ct);
        foreach (var signingCase in due)
        {
            // Enkel Ip/UserAgent op events (de trigger laat precies dat toe) en de handtekeningafbeeldingen.
            var events = await _db.SigningEvent.Where(e => e.SigningCaseId == signingCase.Id && (e.Ip != null || e.UserAgent != null)).ToListAsync(ct);
            foreach (var e in events) { e.Ip = null; e.UserAgent = null; }
            var images = await _db.SigningDocument.Where(d => d.SigningCaseId == signingCase.Id && d.Kind == (int)SigningDocumentKind.SignatureImage && d.Content != null).ToListAsync(ct);
            foreach (var d in images) d.Content = null;
            signingCase.RetentionScrubbedAt = Now;
            await _db.SaveChangesAsync(ct);
            await _evidence.AppendAsync(new SigningEventDraft(signingCase.Id, null, SigningEventTypes.RetentionScrubApplied, (int)SigningActorType.System, null, null, null, null, null,
                new { eventsScrubbed = events.Count, signatureImagesRemoved = images.Count }), ct);
        }
        return due.Count;
    }

    /// <summary>Vangnet naast <see cref="ExpireOverdueCasesAsync"/>: dossiers die regel-compleet zijn
    /// maar nog niet verlopen, waarvan een eerdere finalisatie faalde. Zelfde herevaluatie + dezelfde
    /// idempotente <see cref="TryFinalizeAsync"/> als daar.</summary>
    public async Task<int> RetryStuckFinalizationsAsync(CancellationToken ct = default)
    {
        var ids = await _db.SigningCase.Where(c => c.Status == (int)SigningCaseStatus.Open && c.CompletedAt == null).Select(c => c.Id).ToListAsync(ct);
        var count = 0;
        foreach (var id in ids)
        {
            var signingCase = await LoadCaseAsync(id, ct);
            if (signingCase is null || signingCase.Status != (int)SigningCaseStatus.Open) continue;
            if (!SigningRuleEvaluator.Evaluate((SigningRule)signingCase.SigningRule, RuleStates(signingCase)).IsComplete) continue;
            if (await TryFinalizeAsync(id, SystemContext, ct)) count++;
        }
        return count;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Kern: finaliseren, sluiten, uitnodigen, documenten
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Idempotent: maakt het ondertekende document en het auditrapport en zet het dossier op Completed. False = (nog) niet gelukt; de achtergrondjob probeert opnieuw.</summary>
    private async Task<bool> TryFinalizeAsync(int caseId, SigningRequestContext ctx, CancellationToken ct)
    {
        var signingCase = await LoadCaseAsync(caseId, ct);
        if (signingCase is null) return false;
        if (signingCase.Status == (int)SigningCaseStatus.Completed) return true;
        if (signingCase.Status != (int)SigningCaseStatus.Open) return false;
        if (!SigningRuleEvaluator.Evaluate((SigningRule)signingCase.SigningRule, RuleStates(signingCase)).IsComplete) return false;

        var original = await _db.SigningDocument.AsNoTracking().FirstOrDefaultAsync(d => d.Id == signingCase.OriginalDocumentId, ct);
        if (VerifiedContent(original) is null) { _logger.LogCritical("Finaliseren van dossier {CaseId}: origineel ontbreekt of hash klopt niet.", caseId); return false; }

        var signers = await SignedPartyInfosAsync(signingCase, ct);
        var view = ToView(signingCase);
        var policy = await _db.SigningPolicy.AsNoTracking().FirstOrDefaultAsync(p => p.DocumentType == signingCase.DocumentType, ct);
        var verifyUrl = VerifyUrl(signingCase.PublicVerificationId);

        try
        {
            var finalPdf = await _renderer.ComposeFinalPdfAsync(new FinalDocumentInput(view, original!.Content, original.Sha256, signers, verifyUrl), ct);
            var finalDoc = await StoreDocumentAsync(signingCase.Id, null, SigningDocumentKind.Final,
                $"{SafeFileName(signingCase.Title)}-ondertekend.pdf", "application/pdf", finalPdf, null, ctx, ct, quiet: true);
            signingCase.FinalDocumentId = finalDoc.Id;
            signingCase.Status = (int)SigningCaseStatus.Completed;
            signingCase.CompletedAt = Now;
            signingCase.RetentionUntil = policy?.RetentionDays is int days ? Now.AddDays(days) : null;
            await _db.SaveChangesAsync(ct);
            await _evidence.AppendAsync(Draft(signingCase.Id, null, SigningEventTypes.FinalDocumentCreated, ctx, finalDoc.Sha256, new { documentId = finalDoc.Id, bytes = finalDoc.ByteLength }, SigningActorType.System), ct);
            await _evidence.AppendAsync(Draft(signingCase.Id, null, SigningEventTypes.CaseCompleted, ctx, finalDoc.Sha256, new { signers = signers.Count, originalSha256 = original.Sha256, finalSha256 = finalDoc.Sha256 }, SigningActorType.System), ct);

            // Het rapport bevat alle events tot en met CaseCompleted; zijn eigen aanmaak-event volgt erna.
            var events = await _evidence.ListAsync(signingCase.Id, ct);
            var chain = await _evidence.VerifyAsync(signingCase.Id, ct);
            var report = await _renderer.RenderAuditReportAsync(new AuditReportInput(ToView(signingCase), original.Sha256, finalDoc.Sha256,
                signingCase.ConsentTextSnapshot ?? string.Empty, signers, events, chain, verifyUrl), ct);
            var reportDoc = await StoreDocumentAsync(signingCase.Id, null, SigningDocumentKind.AuditReport,
                $"{SafeFileName(signingCase.Title)}-auditrapport.pdf", "application/pdf", report, null, ctx, ct, quiet: true);
            signingCase.AuditReportDocumentId = reportDoc.Id;
            await _db.SaveChangesAsync(ct);
            await _evidence.AppendAsync(Draft(signingCase.Id, null, SigningEventTypes.AuditReportCreated, ctx, reportDoc.Sha256, new { documentId = reportDoc.Id, bytes = reportDoc.ByteLength, chainValid = chain.Valid }, SigningActorType.System), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Finaliseren van dossier {CaseId} faalde; wordt later opnieuw geprobeerd.", caseId);
            await _evidence.AppendAsync(Draft(signingCase.Id, null, SigningEventTypes.FinalizationFailed, ctx, null, new { error = ex.GetType().Name }, SigningActorType.System), ct);
            if (signingCase.Status == (int)SigningCaseStatus.Completed) return true;   // document lukte, enkel het rapport niet
            return false;
        }

        var source = _registry.TrySource(signingCase.DocumentType);
        if (source is not null)
        {
            try { await source.OnCaseCompletedAsync(signingCase.SourceEntityId, signingCase.Id, signingCase.CompletedAt!.Value, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Bron {DocumentType}/{SourceId} kon niet bijgewerkt worden na voltooiing.", signingCase.DocumentType, signingCase.SourceEntityId); }
        }

        // Notificaties (§9.5): ondertekende PDF als bijlage + downloadlink per ondertekenaar, plus intern.
        var mailDoc = await MailDocumentAsync(signingCase, ct);
        var finalContent = (await _db.SigningDocument.AsNoTracking().Where(d => d.Id == signingCase.FinalDocumentId).Select(d => d.Content).FirstOrDefaultAsync(ct));
        foreach (var party in signingCase.Parties.Where(p => p.Status == (int)SigningPartyStatus.Signed && !string.IsNullOrWhiteSpace(p.Email)))
        {
            var (raw, _) = await IssueTokenAsync(party, SigningTokenPurpose.Download, Now.AddDays(_options.DownloadLinkDays), null, ct);
            await TryNotifyAsync(() => _notifier.SendCompletedToPartyAsync(mailDoc, new SigningMailRecipient(party.DisplayName, party.Email!), finalContent, SignUrl(raw), ct), signingCase.Id, party.Id, ct);
        }
        if (source is not null)
        {
            var internals = await SafeInternalRecipientsAsync(source, signingCase.SourceEntityId, ct);
            await TryNotifyAsync(() => _notifier.SendCompletedInternalAsync(mailDoc, internals, InternalUrl(signingCase.Id), ct), signingCase.Id, null, ct);
        }
        return true;
    }

    private async Task CloseCaseAsync(SigningCase signingCase, SigningCaseStatus status, string reason, SigningRequestContext ctx, string partyMessage, string eventType, CancellationToken ct, bool skipEvent = false)
    {
        signingCase.Status = (int)status;
        signingCase.ClosedAt = Now;
        signingCase.ClosedByUserId = ctx.ActorUserId;
        signingCase.CloseReason = Truncate(reason, 1000);

        var toNotify = new List<SigningParty>();
        foreach (var party in signingCase.Parties)
        {
            await _db.Entry(party).Collection(p => p.AccessTokens).LoadAsync(ct);
            RevokeTokens(party, reason);
            if (party.Status is (int)SigningPartyStatus.Signed or (int)SigningPartyStatus.Declined) continue;
            var wasInvited = party.Status != (int)SigningPartyStatus.Pending;
            party.Status = status == SigningCaseStatus.Expired ? (int)SigningPartyStatus.Expired : (int)SigningPartyStatus.Revoked;
            if (wasInvited) toNotify.Add(party);
        }
        await _db.SaveChangesAsync(ct);
        if (!skipEvent)
            await _evidence.AppendAsync(Draft(signingCase.Id, null, eventType, ctx, null, new { reason, status = (int)status }), ct);

        try { await _registry.Provider(signingCase.SignatureMethod).CancelAsync(ToView(signingCase), ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Provider {Provider} kon dossier {CaseId} niet annuleren.", signingCase.SignatureMethod, signingCase.Id); }

        var source = _registry.TrySource(signingCase.DocumentType);
        if (source is not null)
        {
            try { await source.OnCaseClosedAsync(signingCase.SourceEntityId, signingCase.Id, (int)status, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Bron {DocumentType}/{SourceId} kon niet ontgrendeld worden.", signingCase.DocumentType, signingCase.SourceEntityId); }
        }

        var mailDoc = await MailDocumentAsync(signingCase, ct);
        foreach (var party in toNotify.Where(p => !string.IsNullOrWhiteSpace(p.Email)))
            await TryNotifyAsync(() => _notifier.SendLinkNoLongerValidAsync(mailDoc, new SigningMailRecipient(party.DisplayName, party.Email!), partyMessage, ct), signingCase.Id, party.Id, ct);

        await PurgeUnsignedDocumentsAsync(signingCase, ctx, ct);
    }

    /// <summary>
    /// Enkel dossiers die effectief ondertekend werden houden hun PDF-bytes. Sluit een dossier
    /// zonder voltooiing (verlopen, geweigerd, geannuleerd), dan worden de bytes van alle bestanden
    /// verwijderd — in SQL én de spiegelkopie in de Storage API. De metadata (naam, grootte, SHA-256)
    /// blijft staan zodat de audit trail leesbaar en de hash aantoonbaar blijft.
    /// </summary>
    private async Task PurgeUnsignedDocumentsAsync(SigningCase signingCase, SigningRequestContext ctx, CancellationToken ct)
    {
        var docs = await _db.SigningDocument.Where(d => d.SigningCaseId == signingCase.Id && (d.Content != null || d.StorageFileName != null)).ToListAsync(ct);
        if (docs.Count == 0) return;
        var mirrorsDeleted = 0;
        foreach (var doc in docs)
        {
            if (doc.StorageFileName is not null && _storage.IsConfigured)
            {
                if (await _storage.DeleteAsync(_options.StorageFolder, doc.StorageFileName, ct)) mirrorsDeleted++;
                doc.StorageFileName = null;
            }
            doc.Content = null;
        }
        await _db.SaveChangesAsync(ct);
        await _evidence.AppendAsync(Draft(signingCase.Id, null, SigningEventTypes.DocumentContentPurged, ctx, null,
            new { documents = docs.Count, mirrorsDeleted, reason = signingCase.Status }, SigningActorType.System), ct);
    }

    /// <summary>Nieuw token + mail voor één ondertekenaar. Het token wordt altijd bewaard, ook als de mail faalt (dan kan een herinnering het alsnog bezorgen).</summary>
    private async Task<bool> InvitePartyAsync(SigningCase signingCase, SigningParty party, SigningTokenPurpose purpose, SigningRequestContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(party.Email))
        {
            _logger.LogWarning("Partij {PartyId} heeft geen e-mailadres; uitnodiging overgeslagen.", party.Id);
            return false;
        }
        var expires = signingCase.ExpiresAt ?? Now.AddDays(30);
        var (raw, token) = await IssueTokenAsync(party, purpose, expires, ctx.ActorUserId, ct);

        if (party.Status == (int)SigningPartyStatus.Pending) party.Status = (int)SigningPartyStatus.Invited;
        party.InvitedAt ??= Now;
        if (purpose == SigningTokenPurpose.Reminder) { party.ReminderCount++; party.LastReminderAt = Now; }
        await _db.SaveChangesAsync(ct);

        var mailDoc = await MailDocumentAsync(signingCase, ct);
        var recipient = new SigningMailRecipient(party.DisplayName, party.Email);
        var url = SignUrl(raw);
        var eventType = purpose switch
        {
            SigningTokenPurpose.Reminder => SigningEventTypes.ReminderSent,
            SigningTokenPurpose.Regenerated => SigningEventTypes.LinkRegenerated,
            _ => SigningEventTypes.InvitationSent,
        };
        var sent = await TryNotifyAsync(() => purpose switch
        {
            SigningTokenPurpose.Reminder => _notifier.SendReminderAsync(mailDoc, recipient, url, ct),
            SigningTokenPurpose.Regenerated => _notifier.SendNewLinkAsync(mailDoc, recipient, url, ct),
            _ => _notifier.SendInvitationAsync(mailDoc, recipient, url, ct),
        }, signingCase.Id, party.Id, ct);

        await _evidence.AppendAsync(Draft(signingCase.Id, party.Id, eventType, ctx, await OriginalShaAsync(signingCase.Id, ct),
            new { tokenId = token.Id, tokenExpiresAt = token.ExpiresAt, email = SigningCrypto.MaskEmail(party.Email), sent, testMode = _options.IsTestMode }), ct);
        return sent;
    }

    private async Task<(string Raw, SigningAccessToken Token)> IssueTokenAsync(SigningParty party, SigningTokenPurpose purpose, DateTime expiresAt, int? byUserId, CancellationToken ct)
    {
        var raw = SigningCrypto.GenerateToken(_options.TokenBytes);
        var token = new SigningAccessToken
        {
            SigningPartyId = party.Id,
            TokenHash = SigningCrypto.HashToken(raw),
            Purpose = (int)purpose,
            CreatedAt = Now,
            CreatedByUserId = byUserId,
            ExpiresAt = expiresAt,
        };
        _db.SigningAccessToken.Add(token);
        await _db.SaveChangesAsync(ct);
        return (raw, token);
    }

    private int RevokeTokens(SigningParty party, string reason)
    {
        var n = 0;
        foreach (var t in party.AccessTokens.Where(t => t.RevokedAt == null && t.Purpose != (int)SigningTokenPurpose.Download))
        {
            t.RevokedAt = Now;
            t.RevokedReason = Truncate(reason, 500);
            t.SessionId = null;
            n++;
        }
        return n;
    }

    private async Task<SigningDocument> StoreDocumentAsync(int caseId, int? partyId, SigningDocumentKind kind, string fileName, string contentType, byte[] content, int? byUserId, SigningRequestContext ctx, CancellationToken ct, bool quiet = false)
    {
        var sha = SigningCrypto.Sha256Hex(content);
        var safeName = SafeFileName(fileName);
        // Spiegel naar de Storage API (§9.1); SQL blijft de bewijsbron. Falen = event, geen blokkade.
        string? storageName = null;
        if (_storage.IsConfigured)
            storageName = await _storage.UploadAsync(content, $"{caseId}-{(int)kind}-{Guid.NewGuid():N}{Path.GetExtension(safeName)}", contentType, _options.StorageFolder, ct);

        var doc = new SigningDocument
        {
            SigningCaseId = caseId,
            SigningPartyId = partyId,
            Kind = (int)kind,
            FileName = Truncate(safeName, 260)!,
            ContentType = contentType,
            ByteLength = content.LongLength,
            Sha256 = sha,
            StorageFileName = storageName,
            Content = content,
            CreatedAt = Now,
            CreatedByUserId = byUserId,
        };
        _db.SigningDocument.Add(doc);
        await _db.SaveChangesAsync(ct);

        if (!quiet)
            await _evidence.AppendAsync(Draft(caseId, partyId, SigningEventTypes.DocumentStored, ctx, sha, new { documentId = doc.Id, kind = (int)kind, fileName = doc.FileName, bytes = doc.ByteLength, mirrored = storageName is not null }), ct);
        if (_storage.IsConfigured && storageName is null)
            await _evidence.AppendAsync(Draft(caseId, partyId, SigningEventTypes.DocumentStorageMirrorFailed, ctx, sha, new { documentId = doc.Id }, SigningActorType.System), ct);
        return doc;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Hulpfuncties
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static readonly SigningRequestContext SystemContext = new(null, null);

    private IQueryable<SigningCase> QueryCases(bool tracking = false)
    {
        var q = _db.SigningCase
            .Include(c => c.Parties).ThenInclude(p => p.AccessTokens)
            .Include(c => c.OriginalDocument)
            .Include(c => c.FinalDocument)
            .Include(c => c.AuditReportDocument)
            .AsQueryable();
        return tracking ? q : q.AsNoTracking();
    }

    private Task<SigningCase?> LoadCaseAsync(int caseId, CancellationToken ct)
        => QueryCases(tracking: true).FirstOrDefaultAsync(c => c.Id == caseId, ct);

    /// <summary>Token → partij → dossier via de sessie; verlengt de sessie glijdend. Null = neutrale fout.</summary>
    private async Task<(SigningAccessToken Token, SigningParty Party, SigningCase Case)?> ResolveSessionAsync(Guid sessionId, bool allowDownload, CancellationToken ct)
    {
        if (sessionId == Guid.Empty) return null;
        var token = await _db.SigningAccessToken
            .Include(t => t.SigningParty).ThenInclude(p => p.SigningCase)
            .FirstOrDefaultAsync(t => t.SessionId == sessionId && t.RevokedAt == null, ct);
        if (token is null) return null;
        if (!allowDownload && token.Purpose == (int)SigningTokenPurpose.Download) return null;
        var lastUsed = token.LastUsedAt ?? token.SessionIssuedAt ?? DateTime.MinValue;
        if (lastUsed.AddMinutes(_options.SigningSessionMinutes) <= Now || token.ExpiresAt <= Now) return null;
        token.LastUsedAt = Now;
        await _db.SaveChangesAsync(ct);
        return (token, token.SigningParty, token.SigningParty.SigningCase);
    }

    private async Task<bool> HasFreshVerificationAsync(int partyId, Guid sessionId, CancellationToken ct)
    {
        var freshSince = Now.AddMinutes(-_options.SigningSessionMinutes);
        return await _db.SigningVerification.AsNoTracking().AnyAsync(v => v.SigningPartyId == partyId && v.SessionId == sessionId
            && v.Status == (int)SigningVerificationStatus.Verified && v.ConsumedAt == null && v.VerifiedAt >= freshSince, ct);
    }

    private static bool IsPartyOpenForSigning(SigningParty party)
        => party.Status is (int)SigningPartyStatus.Invited or (int)SigningPartyStatus.Opened or (int)SigningPartyStatus.Verified;

    private static bool CanBeReminded(SigningParty party) => IsPartyOpenForSigning(party);

    private static IReadOnlyList<PartyRuleState> RuleStates(SigningCase signingCase)
        => signingCase.Parties.Select(p => new PartyRuleState(p.Id, p.SortOrder, (SigningPartyStatus)p.Status)).ToList();

    private async Task<string?> OriginalShaAsync(int caseId, CancellationToken ct)
        => await _db.SigningCase.AsNoTracking().Where(c => c.Id == caseId).Select(c => c.OriginalDocument != null ? c.OriginalDocument.Sha256 : null).FirstOrDefaultAsync(ct);

    /// <summary>Bytes enkel uitleveren wanneer de herberekende SHA-256 klopt (§5.2 stap 3).</summary>
    private SigningDocumentContent? VerifiedContent(SigningDocument? doc)
    {
        if (doc?.Content is null) return null;
        var actual = SigningCrypto.Sha256Hex(doc.Content);
        if (!SigningCrypto.FixedTimeEqualsHex(actual, doc.Sha256))
        {
            _logger.LogCritical("Document {DocumentId} van dossier {CaseId}: inhoud komt niet overeen met de opgeslagen hash.", doc.Id, doc.SigningCaseId);
            return null;
        }
        return new SigningDocumentContent(doc.FileName, doc.ContentType, doc.Content, doc.Sha256);
    }

    private async Task<IReadOnlyList<SignedPartyInfo>> SignedPartyInfosAsync(SigningCase signingCase, CancellationToken ct)
    {
        var result = new List<SignedPartyInfo>();
        foreach (var p in signingCase.Parties.Where(p => p.Status == (int)SigningPartyStatus.Signed).OrderBy(p => p.SignedAt))
        {
            byte[]? image = p.SignatureImageDocumentId is null ? null
                : await _db.SigningDocument.AsNoTracking().Where(d => d.Id == p.SignatureImageDocumentId).Select(d => d.Content).FirstOrDefaultAsync(ct);
            var verification = await _db.SigningVerification.AsNoTracking().Where(v => v.SigningPartyId == p.Id && v.Status == (int)SigningVerificationStatus.Consumed)
                .OrderByDescending(v => v.Id).FirstOrDefaultAsync(ct);
            result.Add(new SignedPartyInfo(p.DisplayName, SigningCrypto.MaskEmail(p.Email), p.Capacity, p.SignedAt ?? Now, p.PartyVerificationId,
                verification?.Method, verification?.DestinationMasked, image));
        }
        return result;
    }

    private async Task<SigningMailDocument> MailDocumentAsync(SigningCase signingCase, CancellationToken ct)
    {
        var source = _registry.TrySource(signingCase.DocumentType);
        var projectName = source is null ? null : await SafeProjectNameAsync(source, signingCase.SourceEntityId, ct);
        return new SigningMailDocument(signingCase.Id, source?.DisplayName ?? signingCase.DocumentType, signingCase.Title, signingCase.DocumentNumber, projectName, signingCase.ExpiresAt);
    }

    private async Task<string?> SafeProjectNameAsync(ISigningDocumentSource source, int sourceEntityId, CancellationToken ct)
    {
        try { return await source.GetProjectNameAsync(sourceEntityId, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Projectnaam ophalen faalde voor {DocumentType}/{SourceId}.", source.DocumentType, sourceEntityId); return null; }
    }

    private async Task<IReadOnlyList<SigningMailRecipient>> SafeInternalRecipientsAsync(ISigningDocumentSource source, int sourceEntityId, CancellationToken ct)
    {
        try { return await source.GetInternalNotificationRecipientsAsync(sourceEntityId, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Interne ontvangers ophalen faalde voor {DocumentType}/{SourceId}.", source.DocumentType, sourceEntityId); return Array.Empty<SigningMailRecipient>(); }
    }

    /// <summary>Een notificatie mag een dossier nooit blokkeren: fout → event + false.</summary>
    private async Task<bool> TryNotifyAsync(Func<Task> send, int caseId, int? partyId, CancellationToken ct)
    {
        try { await send(); return true; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notificatie voor dossier {CaseId}/partij {PartyId} faalde.", caseId, partyId);
            try
            {
                await _evidence.AppendAsync(new SigningEventDraft(caseId, partyId, "NotificationFailed", (int)SigningActorType.System, null, null, null, null, null, new { error = ex.GetType().Name }), ct);
            }
            catch (Exception inner) { _logger.LogError(inner, "Event NotificationFailed kon niet geschreven worden."); }
            return false;
        }
    }

    private static SigningEventDraft Draft(int caseId, int? partyId, string type, SigningRequestContext ctx, string? documentSha, object? data, SigningActorType? actor = null)
    {
        var actorType = actor ?? (ctx.ActorUserId.HasValue ? SigningActorType.Internal : ctx.SessionId.HasValue ? SigningActorType.Party : SigningActorType.System);
        return new SigningEventDraft(caseId, partyId, type, (int)actorType, ctx.ActorUserId, ctx.ActorLabel, ctx.Ip, ctx.UserAgent, documentSha, data);
    }

    // MERGE 28/09/2026: "/ondertekenen" is (nog) de route van SigningController (de link-per-e-mail-
    // flow van de andere pc, ONDERTEKENEN_VOORTGANG.md "Samenloop"). Fase 2 van deze module gebruikt
    // voorlopig "/tekenen" om die niet te breken; de cutover (route overnemen, hun flow verwijderen)
    // is een latere, bewuste stap.
    private string SignUrl(string rawToken) => $"{_options.PublicBaseUrl!.TrimEnd('/')}/tekenen/{rawToken}";
    private string InternalUrl(int caseId) => $"{_options.PublicBaseUrl!.TrimEnd('/')}/SigningAdmin/Dossier/{caseId}";
    private string VerifyUrl(Guid publicId) => $"{_options.PublicBaseUrl!.TrimEnd('/')}/verifieer/{publicId:D}";

    private CaseStatusView ToView(SigningCase c) => new(
        c.Id, c.PublicVerificationId, c.DocumentType, c.SourceEntityId, c.ProjectId, c.ClientAccountId,
        c.Title, c.DocumentNumber, c.Status, c.SigningRule, c.SignatureMethod, c.OtpRequired, c.VerificationMethod,
        c.CreatedAt, c.OpenedAt, c.ExpiresAt, c.CompletedAt, c.ClosedAt, c.CloseReason,
        c.OriginalDocument?.Sha256, c.FinalDocument?.Sha256, c.AuditReportDocument?.Sha256,
        c.Parties.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).Select(ToPartyView).ToList());

    private static PartyStatusView ToPartyView(SigningParty p) => new(
        p.Id, p.PartyType, p.DisplayName, SigningCrypto.MaskEmail(p.Email), p.Capacity, p.SortOrder, p.Status, p.PartyVerificationId,
        p.InvitedAt, p.FirstOpenedAt, p.VerifiedAt, p.SignedAt, p.DeclinedAt, p.DeclineReason, p.ReminderCount, p.LastReminderAt,
        p.AccessTokens.Any(t => t.RevokedAt == null && t.ExpiresAt > Now && t.Purpose != (int)SigningTokenPurpose.Download));

    private static bool IsPlausibleEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { return new System.Net.Mail.MailAddress(email.Trim()).Address.Contains('@'); }
        catch { return false; }
    }

    private bool IsAcceptablePng(byte[] data)
    {
        if (data.Length < 100 || data.Length > _options.MaxSignatureImageBytes) return false;
        // PNG-handtekening: 89 50 4E 47 0D 0A 1A 0A
        return data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
    }

    private static string SafeFileName(string name)
    {
        var chars = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => chars.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? "document" : (cleaned.Length > 120 ? cleaned[..120] : cleaned);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
