namespace FacadeCore.Signing;

// DTO's van de signingmodule (ONDERTEKENEN_VOORSTEL.md §4). Records, zoals ServiceCore.Invoicing:
// geen BOCore-klassen — de module heeft geen VB-tegenhanger nodig en deze types dragen nooit
// geheimen (tokens/codes komen enkel als parameter voorbij en worden nergens bewaard).

/// <summary>Herkomst van een aanroep: wie (intern) of vanuit welke sessie (ondertekenaar), en van waar.</summary>
public sealed record SigningRequestContext(
    string? Ip,
    string? UserAgent,
    Guid? SessionId = null,
    int? ActorUserId = null,
    string? ActorLabel = null);

/// <summary>Eén ondertekenaar zoals de bron (of de interne gebruiker) hem aanlevert.</summary>
public sealed record SigningPartyInput(
    int PartyType,
    int? SourceRefId,
    string DisplayName,
    string? Email,
    string? PhoneMasked,
    string? Capacity,
    int SortOrder);

public sealed record SigningAttachmentInput(string FileName, string ContentType, byte[] Content);

/// <summary>Wat een <see cref="ISigningDocumentSource"/> teruggeeft: de PDF plus alles wat de pagina en het dossier moeten tonen.</summary>
public sealed record SigningDocumentPackage(
    byte[] Pdf,
    string FileName,
    string Title,
    string? DocumentNumber,
    string? Summary,
    decimal? AmountExclVat,
    decimal? VatAmount,
    decimal? AmountInclVat,
    int? ProjectId,
    int? ClientAccountId,
    IReadOnlyList<SigningPartyInput> SuggestedParties,
    int? SuggestedRule,
    IReadOnlyList<SigningAttachmentInput> Attachments);

public sealed record CreateSigningCaseRequest(
    string DocumentType,
    int SourceEntityId,
    IReadOnlyList<SigningPartyInput> Parties,
    int? SigningRule,
    DateTime? ExpiresAt,
    int ByUserId,
    string? ByUserLabel,
    /// <summary>Optioneel vrij bericht van de afzender voor de uitnodigingsmail (en de herinneringen).</summary>
    string? InvitationMessage = null);

public sealed record SigningOperationResult(bool Success, string? Error = null, int? CaseId = null)
{
    public static SigningOperationResult Ok(int? caseId = null) => new(true, null, caseId);
    public static SigningOperationResult Fail(string error) => new(false, error);
}

/// <summary>Uitkomst van het inwisselen van een persoonlijke link. Bij mislukking bewust géén reden.</summary>
public sealed record TokenRedeemResult(bool Success, Guid? SessionId, int? PartyId, int? CaseId, bool DownloadOnly)
{
    public static readonly TokenRedeemResult Invalid = new(false, null, null, null, false);
}

public sealed record SigningDocumentContent(string FileName, string ContentType, byte[] Content, string Sha256);

public sealed record SigningAttachmentView(int DocumentId, string FileName, string ContentType, long ByteLength);

/// <summary>Alles wat de publieke ondertekenpagina nodig heeft, opgehaald via de sessie (nooit via een id in de URL).</summary>
public sealed record SigningSessionView(
    int CaseId,
    int PartyId,
    int CaseStatus,
    int PartyStatus,
    string DocumentTypeLabel,
    string Title,
    string? DocumentNumber,
    string? Summary,
    string? ProjectName,
    decimal? AmountExclVat,
    decimal? VatAmount,
    decimal? AmountInclVat,
    string SignerName,
    string? SignerCapacity,
    string ConsentText,
    bool OtpRequired,
    string? VerificationMethod,
    string? VerificationDestinationMasked,
    bool VerificationPending,
    DateTime? VerificationExpiresAt,
    bool IsVerified,
    DateTime? ExpiresAt,
    bool ProviderHostedUi,
    string? ProviderRedirectUrl,
    IReadOnlyList<SigningAttachmentView> Attachments,
    bool DownloadOnly,
    bool HasFinalDocument,
    // ── Voor de portaalpagina (design-handoff 36): wie nog meetekent, wanneer en de verificatiepagina ──
    Guid PublicVerificationId = default,
    DateTime? SignedAt = null,
    DateTime? CompletedAt = null,
    DateTime? VerifiedAt = null,
    IReadOnlyList<SigningSessionPartyView>? Parties = null,
    string? UnitLabel = null,
    string? IssuerName = null);

/// <summary>Een ondertekenaar zoals de portaalpagina hem toont ("0 van 2 · Lataire Niels · Blanco Mariana").</summary>
public sealed record SigningSessionPartyView(string DisplayName, string? Capacity, bool Signed, bool IsCurrent);

public sealed record VerificationRequestResult(bool Success, string? DestinationMasked, DateTime? ExpiresAt, string? Error);

public sealed record VerificationResult(bool Success, bool Locked, int? AttemptsLeft, string? Error);

public sealed record SignRequest(Guid SessionId, bool ConsentAccepted, byte[]? SignatureImagePng, string IdempotencyKey);

public sealed record SignResult(bool Success, bool AlreadySigned, bool CaseCompleted, string? Error);

public sealed record DeclineRequest(Guid SessionId, string Reason);

// ── Ondertekenmethode/provider ───────────────────────────────────────────────────────────────

public sealed record SignatureCapabilities(bool HostedUi, bool RequiresOwnOtp, bool SupportsWebhooks, bool ProducesSignedPdf);

public sealed record ProviderStart(bool Success, string? ProviderCaseRef, string? RedirectUrl, string? Error);

public sealed record ProviderCallbackResult(bool Handled, int? CaseId, string? Error);

public sealed record SignatureEvidence(string MethodKey, string Summary, string? DataJson);

/// <summary>Wat een SES-handtekening server-side vaststelt en de provider ter bevestiging krijgt.</summary>
public sealed record SignRequestEvidence(
    int VerificationId,
    string? VerificationMethod,
    DateTime? VerifiedAt,
    DateTime ConsentAcceptedAt,
    string ConsentText,
    string DocumentSha256,
    string? Ip,
    string? UserAgent);

// ── Berichten/kanalen ────────────────────────────────────────────────────────────────────────

public sealed record OutboundMessage(string ChannelKey, string Destination, string? Subject, string Body, bool IsHtml);

public sealed record DeliveryReceipt(bool Accepted, string ProviderKey, string? ProviderMessageId, DateTime AcceptedAt, string? Status, string? Error);

public sealed record SmsMessage(string ToE164, string Text);

public sealed record DeliveryStatusUpdate(string ProviderMessageId, string Status, DateTime? DeliveredAt);

public sealed record VerificationDestination(bool Found, string? Destination, string? Masked, string ChannelKey);

// ── Bewijs/audit ─────────────────────────────────────────────────────────────────────────────

public sealed record SigningEventDraft(
    int CaseId,
    int? PartyId,
    string EventType,
    int ActorType,
    int? ActorUserId,
    string? ActorLabel,
    string? Ip,
    string? UserAgent,
    string? DocumentSha256,
    object? Data);

public sealed record SigningEventView(
    long Id,
    int? PartyId,
    string EventType,
    DateTime OccurredAtUtc,
    int ActorType,
    string? ActorLabel,
    string? IpMasked,
    string? DocumentSha256,
    string? DataJson,
    string EventHash);

public sealed record ChainVerification(bool Valid, int EventCount, long? FirstBrokenEventId, string Message);

// ── Interne statusweergave ───────────────────────────────────────────────────────────────────

public sealed record PartyStatusView(
    int PartyId,
    int PartyType,
    string DisplayName,
    string? EmailMasked,
    string? Capacity,
    int SortOrder,
    int Status,
    Guid PartyVerificationId,
    DateTime? InvitedAt,
    DateTime? FirstOpenedAt,
    DateTime? VerifiedAt,
    DateTime? SignedAt,
    DateTime? DeclinedAt,
    string? DeclineReason,
    int ReminderCount,
    DateTime? LastReminderAt,
    bool HasActiveLink);

public sealed record CaseStatusView(
    int CaseId,
    Guid PublicVerificationId,
    string DocumentType,
    int SourceEntityId,
    int? ProjectId,
    int? ClientAccountId,
    string Title,
    string? DocumentNumber,
    int Status,
    int SigningRule,
    string SignatureMethod,
    bool OtpRequired,
    string? VerificationMethod,
    DateTime CreatedAt,
    DateTime? OpenedAt,
    DateTime? ExpiresAt,
    DateTime? CompletedAt,
    DateTime? ClosedAt,
    string? CloseReason,
    string? OriginalSha256,
    string? FinalSha256,
    string? AuditReportSha256,
    IReadOnlyList<PartyStatusView> Parties);

// ── Definitieve documenten (rendering in CPMCore, fase 2) ────────────────────────────────────

public sealed record SignedPartyInfo(
    string DisplayName,
    string? EmailMasked,
    string? Capacity,
    DateTime SignedAt,
    Guid PartyVerificationId,
    string? VerificationMethod,
    string? VerificationDestinationMasked,
    byte[]? SignatureImagePng);

public sealed record FinalDocumentInput(
    CaseStatusView Case,
    byte[] OriginalPdf,
    string OriginalSha256,
    IReadOnlyList<SignedPartyInfo> Signers,
    string VerificationUrl);

public sealed record AuditReportInput(
    CaseStatusView Case,
    string OriginalSha256,
    string? FinalSha256,
    string ConsentText,
    IReadOnlyList<SignedPartyInfo> Signers,
    IReadOnlyList<SigningEventView> Events,
    ChainVerification Chain,
    string VerificationUrl);

// ── Notificaties ─────────────────────────────────────────────────────────────────────────────

public sealed record SigningMailRecipient(string Name, string Email);

public sealed record SigningMailDocument(
    int CaseId,
    string DocumentTypeLabel,
    string Title,
    string? DocumentNumber,
    string? ProjectName,
    DateTime? ExpiresAt,
    /// <summary>Vrij bericht van de afzender (SigningCase.InvitationMessage) — platte tekst, de notifier
    /// codeert het zelf naar HTML.</summary>
    string? InvitationMessage = null);
