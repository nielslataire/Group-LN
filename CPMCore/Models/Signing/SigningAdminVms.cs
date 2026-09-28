using BOCore;
using FacadeCore.Signing;
using ServiceCore.Signing;

namespace CPMCore.Models.Signing;

/// <summary>Startscherm: één brondocument ter ondertekening aanbieden (SigningAdmin/Start).</summary>
public sealed class SigningStartVm
{
    public string DocumentType { get; set; } = "";
    public int SourceId { get; set; }
    public string DocumentTypeLabel { get; set; } = "";
    public string Title { get; set; } = "";
    public string? DocumentNumber { get; set; }
    public string? Summary { get; set; }
    public decimal? AmountExclVat { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? AmountInclVat { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsCoordinationProject { get; set; }
    public int? ClientAccountId { get; set; }

    public List<SigningStartPartyVm> Parties { get; set; } = new();
    public int SigningRule { get; set; } = (int)BOCore.SigningRule.All;
    public DateOnly ExpiresOn { get; set; }
    public int LinkValidityDays { get; set; } = 30;

    public bool OtpRequired { get; set; }
    public string? VerificationMethodLabel { get; set; }
    public string ConsentText { get; set; } = "";
    public bool IsTestMode { get; set; }
    public string? TestRecipient { get; set; }
    public string? BackUrl { get; set; }
    public string? PreviewUrl { get; set; }
}

public sealed class SigningStartPartyVm
{
    public bool Include { get; set; } = true;
    public int PartyType { get; set; }
    public int? SourceRefId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneMasked { get; set; }
    public string? Capacity { get; set; }
}

/// <summary>Wat het startscherm terugpost — bewust los van het weergavemodel.</summary>
public sealed class SigningStartPostVm
{
    public string DocumentType { get; set; } = "";
    public int SourceId { get; set; }
    public int SigningRule { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public List<SigningStartPartyVm> Parties { get; set; } = new();
    public string? BackUrl { get; set; }
}

/// <summary>Dossierpagina (SigningAdmin/Dossier/{id}).</summary>
public sealed class SigningDossierVm
{
    public CaseStatusView Case { get; set; } = null!;
    public IReadOnlyList<SigningEventView> Events { get; set; } = Array.Empty<SigningEventView>();
    public string DocumentTypeLabel { get; set; } = "";
    public string? ProjectName { get; set; }
    public bool IsCoordinationProject { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceLabel { get; set; }
    public bool CanWrite { get; set; }
    public bool IsTestMode { get; set; }
    public string VerificationUrl { get; set; } = "";
}

/// <summary>Overzicht (SigningAdmin/Index), optioneel per project.</summary>
public sealed class SigningIndexVm
{
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsCoordinationProject { get; set; }
    public int? StatusFilter { get; set; }
    public IReadOnlyList<CaseStatusView> Cases { get; set; } = Array.Empty<CaseStatusView>();
    public IReadOnlyDictionary<string, string> DocumentTypeLabels { get; set; } = new Dictionary<string, string>();
}

/// <summary>Nederlandse labels + gl-v2-badgeklassen voor de signing-enums en eventtypes. Eén plek, zodat
/// de dossierpagina, het overzicht en de legacy lijst "Wijzigingsopdrachten" dezelfde woorden gebruiken.</summary>
public static class SigningLabels
{
    public static string CaseStatus(int status) => (SigningCaseStatus)status switch
    {
        SigningCaseStatus.Draft => "Nog niet aangeboden",
        SigningCaseStatus.Open => "Ter ondertekening",
        SigningCaseStatus.Completed => "Ondertekend",
        SigningCaseStatus.Declined => "Geweigerd",
        SigningCaseStatus.Expired => "Verlopen",
        SigningCaseStatus.Cancelled => "Geannuleerd",
        _ => "Onbekend"
    };

    public static string CaseStatusBadge(int status) => (SigningCaseStatus)status switch
    {
        SigningCaseStatus.Open => "is-attention",
        SigningCaseStatus.Completed => "is-positive",
        SigningCaseStatus.Declined => "is-blocked",
        SigningCaseStatus.Expired => "is-blocked",
        SigningCaseStatus.Cancelled => "is-inactive",
        _ => "is-neutral"
    };

    public static string PartyStatus(int status) => (SigningPartyStatus)status switch
    {
        SigningPartyStatus.Pending => "Wacht op zijn beurt",
        SigningPartyStatus.Invited => "Uitgenodigd",
        SigningPartyStatus.Opened => "Link geopend",
        SigningPartyStatus.Verified => "Identiteit geverifieerd",
        SigningPartyStatus.Signed => "Ondertekend",
        SigningPartyStatus.Declined => "Geweigerd",
        SigningPartyStatus.Expired => "Verlopen",
        SigningPartyStatus.Revoked => "Ingetrokken",
        _ => "Onbekend"
    };

    public static string PartyStatusBadge(int status) => (SigningPartyStatus)status switch
    {
        SigningPartyStatus.Signed => "is-positive",
        SigningPartyStatus.Declined or SigningPartyStatus.Expired => "is-blocked",
        SigningPartyStatus.Revoked => "is-inactive",
        SigningPartyStatus.Pending => "is-neutral",
        _ => "is-attention"
    };

    public static string Rule(int rule) => (SigningRule)rule switch
    {
        SigningRule.All => "Iedereen moet ondertekenen",
        SigningRule.Any => "Eén handtekening volstaat",
        SigningRule.Ordered => "In volgorde, één na één",
        _ => "Onbekend"
    };

    public static string PartyType(int type) => (SigningPartyType)type switch
    {
        SigningPartyType.ClientAccount => "Klant",
        SigningPartyType.ClientContact => "Contact",
        SigningPartyType.InternalUser => "Group LN",
        SigningPartyType.External => "Extern",
        _ => ""
    };

    public static string ActorType(int type) => (SigningActorType)type switch
    {
        SigningActorType.System => "Systeem",
        SigningActorType.Internal => "Group LN",
        SigningActorType.Party => "Ondertekenaar",
        SigningActorType.Provider => "Provider",
        _ => ""
    };

    public static string VerificationMethod(string? key) => key switch
    {
        "email-otp" => "Code per e-mail",
        "sms-otp" => "Code per sms",
        null or "" => "Geen",
        _ => key
    };

    public static string DocumentKind(int kind) => (SigningDocumentKind)kind switch
    {
        SigningDocumentKind.Original => "Aangeboden document",
        SigningDocumentKind.Final => "Ondertekend document",
        SigningDocumentKind.AuditReport => "Auditrapport",
        SigningDocumentKind.Attachment => "Bijlage",
        SigningDocumentKind.SignatureImage => "Handtekeningbeeld",
        _ => ""
    };

    private static readonly Dictionary<string, string> EventLabels = new(StringComparer.Ordinal)
    {
        [SigningEventTypes.CaseCreated] = "Dossier aangemaakt",
        [SigningEventTypes.DocumentStored] = "Document opgeslagen (hash vastgelegd)",
        [SigningEventTypes.DocumentStorageMirrorFailed] = "Spiegelkopie naar opslag mislukt",
        [SigningEventTypes.CaseOpened] = "Aangeboden ter ondertekening",
        [SigningEventTypes.InvitationSent] = "Uitnodiging verstuurd",
        [SigningEventTypes.ReminderSent] = "Herinnering verstuurd",
        [SigningEventTypes.LinkRegenerated] = "Nieuwe link uitgegeven",
        [SigningEventTypes.LinkRevoked] = "Link ingetrokken",
        [SigningEventTypes.LinkOpened] = "Link geopend",
        [SigningEventTypes.LinkRejected] = "Ongeldige link geweigerd",
        [SigningEventTypes.PartyOpened] = "Ondertekenaar opende het dossier",
        [SigningEventTypes.DocumentViewed] = "Document bekeken",
        [SigningEventTypes.DocumentServed] = "Document aangeleverd aan ondertekenaar",
        [SigningEventTypes.VerificationRequested] = "Verificatiecode aangevraagd",
        [SigningEventTypes.VerificationMessageAccepted] = "Verificatiebericht aanvaard door provider",
        [SigningEventTypes.VerificationMessageFailed] = "Verificatiebericht kon niet verstuurd worden",
        [SigningEventTypes.VerificationMessageDelivered] = "Verificatiebericht afgeleverd",
        [SigningEventTypes.VerificationFailedAttempt] = "Foute verificatiecode",
        [SigningEventTypes.VerificationLocked] = "Verificatie geblokkeerd (te veel pogingen)",
        [SigningEventTypes.VerificationSucceeded] = "Identiteit geverifieerd",
        [SigningEventTypes.ConsentAccepted] = "Akkoordverklaring aanvaard",
        [SigningEventTypes.SignatureImageCaptured] = "Handtekening getekend",
        [SigningEventTypes.PartySigned] = "Ondertekend",
        [SigningEventTypes.PartyDeclined] = "Geweigerd door ondertekenaar",
        [SigningEventTypes.PartyRevoked] = "Ondertekenaar ingetrokken",
        [SigningEventTypes.CaseCompleted] = "Dossier voltooid — alle handtekeningen gezet",
        [SigningEventTypes.FinalDocumentCreated] = "Ondertekend document opgemaakt",
        [SigningEventTypes.AuditReportCreated] = "Auditrapport opgemaakt",
        [SigningEventTypes.FinalizationFailed] = "Afwerking mislukt (wordt opnieuw geprobeerd)",
        [SigningEventTypes.DocumentDownloaded] = "Document gedownload",
        [SigningEventTypes.CaseCancelled] = "Dossier geannuleerd",
        [SigningEventTypes.CaseExpired] = "Dossier verlopen",
        [SigningEventTypes.ProviderCallbackReceived] = "Melding van provider ontvangen",
        [SigningEventTypes.RetentionScrubApplied] = "Bewaartermijn: IP/browsergegevens gewist",
        [SigningEventTypes.DocumentContentPurged] = "Documentinhoud verwijderd (dossier niet voltooid)",
        [SigningEventTypes.NotificationFailed] = "Melding kon niet verstuurd worden",
        [SigningEventTypes.AuditChainVerified] = "Audit trail gecontroleerd",
    };

    public static string Event(string eventType) => EventLabels.TryGetValue(eventType, out var l) ? l : eventType;

    /// <summary>Events die op de tijdlijn opvallen (fout/afwijking) — rood puntje i.p.v. groen.</summary>
    public static bool IsEventAlert(string eventType) => eventType is
        SigningEventTypes.DocumentStorageMirrorFailed or SigningEventTypes.LinkRejected or
        SigningEventTypes.VerificationMessageFailed or SigningEventTypes.VerificationFailedAttempt or
        SigningEventTypes.VerificationLocked or SigningEventTypes.PartyDeclined or SigningEventTypes.FinalizationFailed or
        SigningEventTypes.CaseCancelled or SigningEventTypes.CaseExpired or SigningEventTypes.NotificationFailed;
}
