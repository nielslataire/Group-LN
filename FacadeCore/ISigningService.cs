namespace FacadeCore;

/// <summary>
/// Ondertekenen zonder CPM-login: elke ondertekenaar krijgt een persoonlijke link (token), bevestigt met een
/// code die per e-mail komt, typt zijn naam en vinkt de akkoordverklaring aan. Het bewijsdossier (naam, tijdstip,
/// IP, user-agent, exacte akkoordtekst, hash van het PDF, referentie) wordt op de handtekening bewaard.
/// Dit is een GEWONE/eenvoudige elektronische handtekening — zie JURIDISCH_ELEKTRONISCH_ONDERTEKENEN.md.
/// De methode-aanduiding is "email-otp"; een itsme-aanbieder kan later dezelfde velden vullen met "itsme".
/// </summary>
public interface ISigningService
{
    /// <summary>De akkoordtekst zoals de ondertekenaar die ziet (en zoals ze bewaard wordt).</summary>
    string ConsentText { get; }

    /// <summary>Maakt (of hervat) de ondertekening van een wijzigingsopdracht: document in de map Contracten, één
    /// handtekening per ondertekenaar met een verse link. Geeft de ruwe tokens terug (enkel hier, nergens bewaard).</summary>
    Task<SigningCreateResult> CreateForChangeOrder(int changeOrderId, SigningCreateDto dto);

    /// <summary>Nieuwe link voor één ondertekenaar (de vorige link vervalt). Enkel zolang niet getekend.</summary>
    Task<SigningIssued?> ReissueLink(int projectId, int signatureId, int validDays = 30);

    Task<SigningInfo?> GetInfo(string token, bool markOpened);
    Task<SigningCodeResult> RequestCode(string token);
    Task<SigningSignResult> Sign(string token, string code, string typedName, bool consent, string? ip, string? userAgent);

    /// <summary>Zet het ondertekende PDF (met handtekeningblok) als nieuwe huidige revisie — mag ook op een bevroren document.</summary>
    Task AttachSignedRevision(int documentId, string storedFilename, long sizeBytes, string? note);

    /// <summary>Status van de ondertekening voor een wijzigingsopdracht (voor de interne pagina).</summary>
    Task<ChangeOrderSigningStatus?> GetChangeOrderStatus(int changeOrderId);

    /// <summary>Bewijsdossier van de getekende handtekeningen van een wijzigingsopdracht (voor het handtekeningblok in het PDF).</summary>
    Task<List<SignatureEvidence>> GetEvidence(int changeOrderId);
}

public class SigningSignerDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Role { get; set; }
    public int? ClientAccountId { get; set; }
}

public class SigningCreateDto
{
    public string StoredFilename { get; set; } = "";
    public string? OriginalFilename { get; set; }
    public long SizeBytes { get; set; }
    /// <summary>SHA-256 (hex) van het PDF-bestand dat ter ondertekening gaat.</summary>
    public string PdfHash { get; set; } = "";
    public List<SigningSignerDto> Signers { get; set; } = new();
    public string? NotifyEmail { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public int ValidDays { get; set; } = 30;
}

public class SigningIssued
{
    public int SignatureId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public DateTime ExpiresOn { get; set; }
    public int DocumentId { get; set; }
    public int ProjectId { get; set; }
}

public class SigningCreateResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public int DocumentId { get; set; }
    public int ProjectId { get; set; }
    public List<SigningIssued> Issued { get; set; } = new();
}

public class SigningInfo
{
    /// <summary>valid | signed</summary>
    public string State { get; set; } = "valid";
    public int SignatureId { get; set; }
    public int DocumentId { get; set; }
    public int ProjectId { get; set; }
    public int? ChangeOrderId { get; set; }
    public string ProjectName { get; set; } = "";
    public string DocumentName { get; set; } = "";
    public string SignerName { get; set; } = "";
    public string? SignerRole { get; set; }
    public string SignerEmailMasked { get; set; } = "";
    public DateTime? SignedDate { get; set; }
    public string? SignedName { get; set; }
    public string? EvidenceRef { get; set; }
    public bool AllSigned { get; set; }
    public DateTime ExpiresOn { get; set; }
    public int? CurrentRevisionId { get; set; }
    public string? CurrentFilename { get; set; }
    public string? SentByName { get; set; }
    public List<SigningOtherSigner> Others { get; set; } = new();
}

public class SigningOtherSigner
{
    public string Name { get; set; } = "";
    public bool Signed { get; set; }
}

public class SigningCodeResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    /// <summary>De ruwe code — enkel bedoeld om meteen per e-mail te versturen, nooit te tonen of te loggen.</summary>
    public string? Code { get; set; }
    public string? Email { get; set; }
    public string? SignerName { get; set; }
    public string? DocumentName { get; set; }
}

public class SigningSignResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public bool AllSigned { get; set; }
    public int DocumentId { get; set; }
    public int ProjectId { get; set; }
    public int? ChangeOrderId { get; set; }
    public string EvidenceRef { get; set; } = "";
    public DateTime SignedAt { get; set; }
    public string SignerName { get; set; } = "";
    public string? SignerEmail { get; set; }
    public string? NotifyEmail { get; set; }
    public string DocumentName { get; set; } = "";
    public string ProjectName { get; set; } = "";
}

public class ChangeOrderSigningStatus
{
    public int DocumentId { get; set; }
    public int ProjectId { get; set; }
    public byte DocumentStatus { get; set; }
    public int? CurrentRevisionId { get; set; }
    public List<ChangeOrderSignerStatus> Signers { get; set; } = new();
}

public class ChangeOrderSignerStatus
{
    public int SignatureId { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    /// <summary>0 wacht, 1 geopend, 2 getekend</summary>
    public byte Status { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? TokenExpiresOn { get; set; }
    public string? EvidenceRef { get; set; }
}

public class SignatureEvidence
{
    public string Name { get; set; } = "";
    public string SignedName { get; set; } = "";
    public DateTime SignedAt { get; set; }
    public string? Method { get; set; }
    public string? Ip { get; set; }
    public string? EvidenceRef { get; set; }
    public string? DocumentHash { get; set; }
    public string? Email { get; set; }
}
