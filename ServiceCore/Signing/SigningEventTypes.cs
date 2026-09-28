namespace ServiceCore.Signing;

/// <summary>Vaste sleutels van de audit trail (ONDERTEKENEN_VOORSTEL.md §7). Nooit hernoemen: ze zitten in de hash-ketting.</summary>
public static class SigningEventTypes
{
    public const string CaseCreated = "CaseCreated";
    public const string DocumentStored = "DocumentStored";
    public const string DocumentStorageMirrorFailed = "DocumentStorageMirrorFailed";
    public const string CaseOpened = "CaseOpened";
    public const string InvitationSent = "InvitationSent";
    public const string ReminderSent = "ReminderSent";
    public const string LinkRegenerated = "LinkRegenerated";
    public const string LinkRevoked = "LinkRevoked";
    public const string LinkOpened = "LinkOpened";
    public const string LinkRejected = "LinkRejected";
    public const string PartyOpened = "PartyOpened";
    public const string DocumentViewed = "DocumentViewed";
    public const string DocumentServed = "DocumentServed";
    public const string VerificationRequested = "VerificationRequested";
    public const string VerificationMessageAccepted = "VerificationMessageAccepted";
    public const string VerificationMessageFailed = "VerificationMessageFailed";
    public const string VerificationMessageDelivered = "VerificationMessageDelivered";
    public const string VerificationFailedAttempt = "VerificationFailedAttempt";
    public const string VerificationLocked = "VerificationLocked";
    public const string VerificationSucceeded = "VerificationSucceeded";
    public const string ConsentAccepted = "ConsentAccepted";
    public const string SignatureImageCaptured = "SignatureImageCaptured";
    public const string PartySigned = "PartySigned";
    public const string PartyDeclined = "PartyDeclined";
    public const string PartyRevoked = "PartyRevoked";
    public const string CaseCompleted = "CaseCompleted";
    public const string FinalDocumentCreated = "FinalDocumentCreated";
    public const string AuditReportCreated = "AuditReportCreated";
    public const string FinalizationFailed = "FinalizationFailed";
    public const string DocumentDownloaded = "DocumentDownloaded";
    public const string CaseCancelled = "CaseCancelled";
    public const string CaseExpired = "CaseExpired";
    public const string ProviderCallbackReceived = "ProviderCallbackReceived";
    public const string RetentionScrubApplied = "RetentionScrubApplied";
    /// <summary>Dossier gesloten zonder voltooiing: PDF-bytes en spiegelkopieën verwijderd, metadata + hash blijven.</summary>
    public const string DocumentContentPurged = "DocumentContentPurged";
    public const string NotificationFailed = "NotificationFailed";
    public const string AuditChainVerified = "AuditChainVerified";
}
