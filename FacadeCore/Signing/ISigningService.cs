namespace FacadeCore.Signing;

/// <summary>
/// Aanmaken en beheren van ondertekeningsprocedures (ONDERTEKENEN_VOORSTEL.md §4.1) plus de
/// handelingen van de ondertekenaar zelf (§5.2). Kent geen HTTP: de controllers geven een
/// <see cref="SigningRequestContext"/> mee. Alle statusovergangen lopen onder RowVersion-controle;
/// elke overgang is een event in de audit trail.
/// </summary>
public interface ISigningService
{
    // ── Intern beheer ────────────────────────────────────────────────────────────────────────
    Task<SigningOperationResult> CreateCaseAsync(CreateSigningCaseRequest request, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningOperationResult> OpenCaseAsync(int caseId, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningOperationResult> CancelCaseAsync(int caseId, string reason, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningOperationResult> SendReminderAsync(int partyId, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningOperationResult> RegenerateLinkAsync(int partyId, SigningRequestContext ctx, CancellationToken ct = default);
    Task<CaseStatusView?> GetCaseStatusAsync(int caseId, CancellationToken ct = default);
    Task<CaseStatusView?> GetActiveCaseForSourceAsync(string documentType, int sourceEntityId, CancellationToken ct = default);

    /// <summary>Het meest recente voltooide dossier voor deze bron, of null. Gebruikt om verwijderen te
    /// blokkeren zodra een bron ooit rechtsgeldig ondertekend werd (Niels, 2026-09-28) — de hash-ketting
    /// is met opzet niet-verwijderbaar (append-only trigger), dus de bron die ze bewijst mag ook niet
    /// zomaar verdwijnen (en `ProjectDocs.ChangeOrderId` heeft geen ON DELETE CASCADE: verwijderen zou
    /// anders gewoon op een FK-fout stuklopen). Dit is enkel een applicatie-check, geen DB-constraint —
    /// een beheerder blijft via SSMS vrij om dit alsnog te doen.</summary>
    Task<CaseStatusView?> GetCompletedCaseForSourceAsync(string documentType, int sourceEntityId, CancellationToken ct = default);
    Task<IReadOnlyList<CaseStatusView>> ListCasesAsync(int? projectId, int? status, int take = 200, CancellationToken ct = default);
    Task<IReadOnlyList<SigningEventView>> GetEventsAsync(int caseId, CancellationToken ct = default);
    Task<ChainVerification> VerifyAuditChainAsync(int caseId, CancellationToken ct = default);

    /// <summary>Intern downloaden (origineel / ondertekend / auditrapport / bijlage), altijd na hash-controle; wordt zelf als event geregistreerd.</summary>
    Task<SigningDocumentContent?> GetDocumentAsync(int caseId, int documentKind, int? documentId, SigningRequestContext ctx, CancellationToken ct = default);

    // ── Ondertekenaar (publieke pagina, enkel via sessie) ─────────────────────────────────────
    Task<TokenRedeemResult> RedeemTokenAsync(string rawToken, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningSessionView?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<SigningDocumentContent?> GetSessionDocumentAsync(Guid sessionId, int? attachmentDocumentId, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningDocumentContent?> GetSessionFinalDocumentAsync(Guid sessionId, bool auditReport, SigningRequestContext ctx, CancellationToken ct = default);
    Task RecordDocumentViewedAsync(Guid sessionId, SigningRequestContext ctx, CancellationToken ct = default);
    Task<VerificationRequestResult> RequestVerificationAsync(Guid sessionId, SigningRequestContext ctx, CancellationToken ct = default);
    Task<VerificationResult> VerifyCodeAsync(Guid sessionId, string code, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SignResult> SignAsync(SignRequest request, SigningRequestContext ctx, CancellationToken ct = default);
    Task<SigningOperationResult> DeclineAsync(DeclineRequest request, SigningRequestContext ctx, CancellationToken ct = default);

    // ── Publieke verificatiepagina (§9.4): enkel niet-gevoelige velden ────────────────────────
    Task<PublicVerificationView?> GetPublicVerificationAsync(Guid publicVerificationId, CancellationToken ct = default);

    // ── Achtergrond (SigningHostedService) ────────────────────────────────────────────────────
    Task<int> ExpireOverdueCasesAsync(CancellationToken ct = default);
    Task<int> SendDueRemindersAsync(CancellationToken ct = default);
    Task<int> ApplyRetentionScrubAsync(CancellationToken ct = default);

    /// <summary>Dossiers die regel-compleet zijn (alle vereiste handtekeningen staan er al) maar nooit
    /// voltooid raakten omdat een eerdere <c>TryFinalizeAsync</c> faalde — <c>ExpireOverdueCasesAsync</c>
    /// probeert dit ook, maar enkel voor dossiers waarvan de termijn al verstreken is; dit vangt de
    /// rest (fase 3, ONDERTEKENEN_VOORTGANG.md).</summary>
    Task<int> RetryStuckFinalizationsAsync(CancellationToken ct = default);
}

public sealed record PublicVerificationView(
    string DocumentTypeLabel,
    string? DocumentNumber,
    int Status,
    DateTime? CompletedAt,
    int SignerCount,
    string? OriginalSha256,
    string? FinalSha256);
