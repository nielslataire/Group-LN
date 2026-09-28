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
}

public sealed record PublicVerificationView(
    string DocumentTypeLabel,
    string? DocumentNumber,
    int Status,
    DateTime? CompletedAt,
    int SignerCount,
    string? OriginalSha256,
    string? FinalSha256);
