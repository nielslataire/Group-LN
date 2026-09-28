using System.Security.Cryptography;
using System.Text;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Documents;

/// <summary>Ondertekenen via persoonlijke link + code per e-mail (zie <see cref="ISigningService"/>).
/// Van token en code wordt enkel een SHA-256 bewaard; de code is per token gezouten en vervalt na 10 minuten,
/// max. 5 pogingen per code en 5 codes per uur.</summary>
public class SigningService : ISigningService
{
    public const int MaxCodeAttempts = 5;
    public const int MaxCodesPerHour = 5;
    public const int CodeValidMinutes = 10;
    /// <summary>Na het tekenen blijft de link nog zo lang werken om het ondertekende PDF op te halen.</summary>
    public const int SignedLinkDays = 90;

    private readonly cpmRunningContext _db;
    private readonly IDocumentService _docs;

    public SigningService(cpmRunningContext db, IDocumentService docs)
    {
        _db = db;
        _docs = docs;
    }

    public string ConsentText =>
        "Ik heb deze wijzigingsopdracht gelezen en ga ermee akkoord. Ik begrijp dat deze bevestiging via mijn persoonlijke link " +
        "en een code die naar mijn e-mailadres werd gestuurd, geldt als mijn handtekening.";

    // ── hulpfuncties ────────────────────────────────────────────────────────────────────────────
    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string CodeHashFor(string tokenHash, string code) => Hash(tokenHash + ":" + code.Trim());

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "";
        var at = email.IndexOf('@');
        var local = email[..at];
        return (local.Length <= 1 ? local : local[..1]) + "***" + email[at..];
    }

    private static bool LooksLikeEmail(string? e) =>
        !string.IsNullOrWhiteSpace(e) && e.Length <= 254 && e.Contains('@') && e.IndexOf('@') > 0 && e.LastIndexOf('.') > e.IndexOf('@') + 1 && !e.Contains(' ');

    private async Task<DocumentSignature?> FindByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 20 || token.Length > 100) return null;
        var hash = Hash(token);
        return await _db.DocumentSignatures
            .Include(s => s.Document).ThenInclude(d => d.Signatures)
            .Include(s => s.Document).ThenInclude(d => d.CurrentRevision)
            .FirstOrDefaultAsync(s => s.TokenHash == hash);
    }

    // ── aanmaken ────────────────────────────────────────────────────────────────────────────────
    public async Task<SigningCreateResult> CreateForChangeOrder(int changeOrderId, SigningCreateDto dto)
    {
        var fail = (string m) => new SigningCreateResult { Ok = false, Message = m };
        var co = await _db.ChangeOrder.AsNoTracking()
            .Include(c => c.ContractActivity).ThenInclude(a => a.Contract)
            .FirstOrDefaultAsync(c => c.Id == changeOrderId);
        if (co == null) return fail("De wijzigingsopdracht bestaat niet meer.");
        var projectId = co.ContractActivity.Contract.ProjectId;

        var signers = dto.Signers.Where(s => !string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.Email)).ToList();
        if (signers.Count == 0) return fail("Voeg minstens één ondertekenaar toe.");
        foreach (var s in signers)
        {
            if (string.IsNullOrWhiteSpace(s.Name)) return fail("Elke ondertekenaar heeft een naam nodig.");
            if (!LooksLikeEmail(s.Email)) return fail($"Het e-mailadres van {s.Name} is niet geldig.");
        }
        if (signers.Select(s => s.Email.Trim().ToLowerInvariant()).Distinct().Count() != signers.Count)
            return fail("Elke ondertekenaar heeft een eigen e-mailadres nodig (de link is persoonlijk).");
        if (string.IsNullOrWhiteSpace(dto.StoredFilename)) return fail("Het PDF-bestand ontbreekt.");

        var existing = await _db.ProjectDocs.AsNoTracking().Where(d => d.ChangeOrderId == changeOrderId)
            .Select(d => new { d.Id, d.Status }).FirstOrDefaultAsync();
        if (existing != null && existing.Status == DocumentStatus.Getekend)
            return fail("Deze wijzigingsopdracht is al ondertekend.");

        var contractenFolder = await _db.DocumentFolders.AsNoTracking().Where(f => f.Code == "contracten").Select(f => f.Id).FirstOrDefaultAsync();
        var name = ("Wijzigingsopdracht — " + (co.Description ?? "").Trim()).TrimEnd(' ', '—');
        if (name.Length > 200) name = name[..200];

        int docId;
        if (existing == null)
        {
            var up = await _docs.Upload(new DocUploadDto
            {
                ProjectId = projectId, Mode = "new", Name = name, FolderId = contractenFolder == 0 ? null : contractenFolder,
                DocDate = DateOnly.FromDateTime(DateTime.Today), StoredFilename = dto.StoredFilename, OriginalFilename = dto.OriginalFilename,
                SizeBytes = dto.SizeBytes, Links = new List<DocLinkRef> { new() { Type = "client", Id = co.ClientAccountId } },
                Status = DocumentStatus.Goedgekeurd, UserId = dto.UserId, UserName = dto.UserName,
                Note = "Ter ondertekening verstuurd"
            });
            if (!up.Ok || !up.Id.HasValue) return fail(up.Message ?? "Het document kon niet aangemaakt worden.");
            docId = up.Id.Value;
            var docRow = await _db.ProjectDocs.FirstAsync(d => d.Id == docId);
            docRow.ChangeOrderId = changeOrderId;
            await _db.SaveChangesAsync();
        }
        else
        {
            // Opnieuw versturen (bv. na een aanpassing): nieuwe revisie met het nieuwe PDF, ondertekenaars beginnen opnieuw.
            docId = existing.Id;
            var up = await _docs.Upload(new DocUploadDto
            {
                ProjectId = projectId, Mode = "revision", DocumentId = docId, StoredFilename = dto.StoredFilename, OriginalFilename = dto.OriginalFilename,
                SizeBytes = dto.SizeBytes, Status = DocumentStatus.Goedgekeurd, UserId = dto.UserId, UserName = dto.UserName, Note = "Opnieuw ter ondertekening verstuurd"
            });
            if (!up.Ok) return fail(up.Message ?? "Het document kon niet bijgewerkt worden.");
        }

        var send = await _docs.SendForSignature(projectId, docId, signers.Select(s => new DocSignatoryDto
        {
            Name = s.Name.Trim(), Role = string.IsNullOrWhiteSpace(s.Role) ? "klant" : s.Role, ClientAccountId = s.ClientAccountId
        }).ToList());
        if (!send.Ok) return fail(send.Message ?? "Ondertekenen kon niet gestart worden.");

        var sigs = await _db.DocumentSignatures.Where(x => x.DocumentId == docId).OrderBy(x => x.SignOrder).ToListAsync();
        var now = DateTime.UtcNow;
        var expires = now.AddDays(Math.Max(1, dto.ValidDays));
        var result = new SigningCreateResult { Ok = true, DocumentId = docId, ProjectId = projectId };
        for (var i = 0; i < sigs.Count && i < signers.Count; i++)
        {
            var token = NewToken();
            var s = sigs[i];
            s.SignerEmail = signers[i].Email.Trim();
            s.NotifyEmail = string.IsNullOrWhiteSpace(dto.NotifyEmail) ? null : dto.NotifyEmail.Trim();
            s.TokenHash = Hash(token);
            s.TokenExpiresOn = expires;
            s.DocumentHash = dto.PdfHash;
            s.CodeAttempts = 0; s.CodeSentCount = 0; s.CodeHash = null; s.CodeExpiresOn = null; s.CodeWindowStart = null;
            result.Issued.Add(new SigningIssued
            {
                SignatureId = s.Id, Name = s.Name, Email = s.SignerEmail, Token = token, ExpiresOn = expires, DocumentId = docId, ProjectId = projectId
            });
        }
        var coRow = await _db.ChangeOrder.FirstAsync(c => c.Id == changeOrderId);
        coRow.DateSendToClient = DateOnly.FromDateTime(DateTime.Today);
        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<SigningIssued?> ReissueLink(int projectId, int signatureId, int validDays = 30)
    {
        var s = await _db.DocumentSignatures.Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == signatureId && x.Document.ProjectId == projectId);
        if (s == null || s.Status == 2 || string.IsNullOrWhiteSpace(s.SignerEmail)) return null;
        var token = NewToken();
        var expires = DateTime.UtcNow.AddDays(Math.Max(1, validDays));
        s.TokenHash = Hash(token);
        s.TokenExpiresOn = expires;
        s.CodeHash = null; s.CodeExpiresOn = null; s.CodeAttempts = 0; s.CodeSentCount = 0; s.CodeWindowStart = null;
        s.SentDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new SigningIssued { SignatureId = s.Id, Name = s.Name, Email = s.SignerEmail!, Token = token, ExpiresOn = expires, DocumentId = s.DocumentId, ProjectId = projectId };
    }

    // ── openbaar: bekijken ──────────────────────────────────────────────────────────────────────
    public async Task<SigningInfo?> GetInfo(string token, bool markOpened)
    {
        var s = await FindByToken(token);
        if (s == null) return null;
        var now = DateTime.UtcNow;
        var expired = s.Status != 2 && s.TokenExpiresOn.HasValue && s.TokenExpiresOn.Value < now;
        if (s.Status == 2 && s.TokenExpiresOn.HasValue && s.TokenExpiresOn.Value < now) return null; // ook de kopie-link is verlopen
        if (markOpened && !expired && s.Status == 0)
        {
            s.Status = 1;
            s.OpenedDate = now;
            await _db.SaveChangesAsync();
        }
        var d = s.Document;
        var projectName = await _db.Project.AsNoTracking().Where(p => p.ProjectId == d.ProjectId).Select(p => p.ProjectName).FirstOrDefaultAsync();
        var sentBy = await _db.DocumentRevisions.AsNoTracking().Where(r => r.DocumentId == d.Id).OrderBy(r => r.RevisionNo).Select(r => r.UploadedByName).FirstOrDefaultAsync();
        return new SigningInfo
        {
            State = s.Status == 2 ? "signed" : expired ? "expired" : "valid",
            SignatureId = s.Id, DocumentId = d.Id, ProjectId = d.ProjectId, ChangeOrderId = d.ChangeOrderId,
            ProjectName = projectName ?? "", DocumentName = d.Name ?? "", SignerName = s.Name, SignerRole = s.Role,
            SignerEmailMasked = MaskEmail(s.SignerEmail), SignedDate = s.SignedDate, SignedName = s.SignedName, EvidenceRef = s.EvidenceRef,
            AllSigned = d.Signatures.All(x => x.Status == 2), ExpiresOn = s.TokenExpiresOn ?? now,
            CurrentRevisionId = d.CurrentRevisionId, CurrentFilename = d.CurrentRevision?.Filename, SentByName = sentBy,
            Others = d.Signatures.Where(x => x.Id != s.Id).OrderBy(x => x.SignOrder).Select(x => new SigningOtherSigner { Name = x.Name, Signed = x.Status == 2 }).ToList()
        };
    }

    // ── openbaar: code aanvragen ────────────────────────────────────────────────────────────────
    public async Task<SigningCodeResult> RequestCode(string token)
    {
        var s = await FindByToken(token);
        if (s == null) return new SigningCodeResult { Ok = false, Message = "Deze link is niet geldig." };
        var now = DateTime.UtcNow;
        if (s.Status == 2) return new SigningCodeResult { Ok = false, Message = "U heeft dit document al ondertekend." };
        if (s.TokenExpiresOn.HasValue && s.TokenExpiresOn.Value < now) return new SigningCodeResult { Ok = false, Message = "Deze link is verlopen." };
        if (string.IsNullOrWhiteSpace(s.SignerEmail)) return new SigningCodeResult { Ok = false, Message = "Er is geen e-mailadres bekend voor deze ondertekenaar." };
        if (s.Document.Status == DocumentStatus.Getekend) return new SigningCodeResult { Ok = false, Message = "Dit document is al volledig ondertekend." };

        if (!s.CodeWindowStart.HasValue || now - s.CodeWindowStart.Value > TimeSpan.FromHours(1))
        {
            s.CodeWindowStart = now;
            s.CodeSentCount = 0;
        }
        if (s.CodeSentCount >= MaxCodesPerHour)
            return new SigningCodeResult { Ok = false, Message = "Er werden al meerdere codes aangevraagd. Probeer het over een uur opnieuw." };

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        s.CodeHash = CodeHashFor(s.TokenHash!, code);
        s.CodeExpiresOn = now.AddMinutes(CodeValidMinutes);
        s.CodeAttempts = 0;
        s.CodeSentCount++;
        await _db.SaveChangesAsync();
        return new SigningCodeResult { Ok = true, Code = code, Email = s.SignerEmail, SignerName = s.Name, DocumentName = s.Document.Name };
    }

    // ── openbaar: tekenen ───────────────────────────────────────────────────────────────────────
    public async Task<SigningSignResult> Sign(string token, string code, string typedName, bool consent, string? ip, string? userAgent)
    {
        var bad = (string m) => new SigningSignResult { Ok = false, Message = m };
        var s = await FindByToken(token);
        if (s == null) return bad("Deze link is niet geldig.");
        var now = DateTime.UtcNow;
        if (s.Status == 2) return bad("U heeft dit document al ondertekend.");
        if (s.TokenExpiresOn.HasValue && s.TokenExpiresOn.Value < now) return bad("Deze link is verlopen.");
        var d = s.Document;
        if (d.Status == DocumentStatus.Getekend) return bad("Dit document is al volledig ondertekend.");
        if (!consent) return bad("Vink aan dat u akkoord gaat om te ondertekenen.");
        typedName = (typedName ?? "").Trim();
        if (typedName.Length < 3) return bad("Typ uw volledige naam om te ondertekenen.");
        if (string.IsNullOrWhiteSpace(code)) return bad("Geef de code in die u per e-mail kreeg.");
        if (string.IsNullOrEmpty(s.CodeHash) || !s.CodeExpiresOn.HasValue || s.CodeExpiresOn.Value < now)
            return bad("Vraag eerst een code aan; de vorige is verlopen of werd nog niet verstuurd.");
        if (s.CodeAttempts >= MaxCodeAttempts)
        {
            s.CodeHash = null; s.CodeExpiresOn = null;
            await _db.SaveChangesAsync();
            return bad("Te veel foute pogingen. Vraag een nieuwe code aan.");
        }

        var expected = Encoding.UTF8.GetBytes(s.CodeHash);
        var given = Encoding.UTF8.GetBytes(CodeHashFor(s.TokenHash!, code));
        if (expected.Length != given.Length || !CryptographicOperations.FixedTimeEquals(expected, given))
        {
            s.CodeAttempts++;
            var left = MaxCodeAttempts - s.CodeAttempts;
            if (left <= 0) { s.CodeHash = null; s.CodeExpiresOn = null; }
            await _db.SaveChangesAsync();
            return bad(left > 0 ? $"De code klopt niet ({left} {(left == 1 ? "poging" : "pogingen")} over)." : "Te veel foute pogingen. Vraag een nieuwe code aan.");
        }

        var evidenceRef = "WO-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        s.Status = 2;
        s.SignedDate = now;
        s.Method = "email-otp";
        s.SignedName = typedName.Length > 150 ? typedName[..150] : typedName;
        s.SignedIp = ip is { Length: > 64 } ? ip[..64] : ip;
        s.SignedUserAgent = userAgent is { Length: > 400 } ? userAgent[..400] : userAgent;
        s.ConsentText = ConsentText;
        s.EvidenceRef = evidenceRef;
        s.CodeHash = null; s.CodeExpiresOn = null;
        s.TokenExpiresOn = now.AddDays(SignedLinkDays);

        var allSigned = d.Signatures.All(x => x.Status == 2 || x.Id == s.Id);
        if (allSigned)
        {
            d.Status = DocumentStatus.Getekend;
            d.ModifiedDate = now;
            if (d.ChangeOrderId.HasValue)
            {
                var co = await _db.ChangeOrder.FirstOrDefaultAsync(c => c.Id == d.ChangeOrderId);
                if (co != null && co.DateAgreement == null) co.DateAgreement = DateOnly.FromDateTime(DateTime.Today);
            }
        }
        await _db.SaveChangesAsync();

        var projectName = await _db.Project.AsNoTracking().Where(p => p.ProjectId == d.ProjectId).Select(p => p.ProjectName).FirstOrDefaultAsync();
        return new SigningSignResult
        {
            Ok = true, AllSigned = allSigned, DocumentId = d.Id, ProjectId = d.ProjectId, ChangeOrderId = d.ChangeOrderId, EvidenceRef = evidenceRef, SignedAt = now,
            SignerName = s.SignedName, SignerEmail = s.SignerEmail, NotifyEmail = s.NotifyEmail, DocumentName = d.Name ?? "", ProjectName = projectName ?? ""
        };
    }

    // ── na het tekenen ──────────────────────────────────────────────────────────────────────────
    public async Task AttachSignedRevision(int documentId, string storedFilename, long sizeBytes, string? note)
    {
        var d = await _db.ProjectDocs.Include(x => x.Revisions).FirstOrDefaultAsync(x => x.Id == documentId);
        if (d == null) return;
        var now = DateTime.UtcNow;
        var rev = new DocumentRevision
        {
            DocumentId = d.Id, RevisionNo = d.Revisions.Count == 0 ? 1 : d.Revisions.Max(r => r.RevisionNo) + 1, Filename = storedFilename,
            OriginalFilename = storedFilename, SizeBytes = sizeBytes, Status = 2, Note = note ?? "Ondertekende versie", UploadedByKind = DocumentUploaderKind.Intern,
            UploadedByName = "Systeem (elektronische ondertekening)", UploadedDate = now, ApprovedDate = now, ApprovedByName = "Systeem"
        };
        _db.DocumentRevisions.Add(rev);
        await _db.SaveChangesAsync();
        foreach (var r in d.Revisions.Where(r => r.Status == 2 && r.Id != rev.Id)) r.Status = 3;
        d.CurrentRevisionId = rev.Id;
        d.Filename = storedFilename;
        d.ModifiedDate = now;
        await _db.SaveChangesAsync();
    }

    public async Task<ChangeOrderSigningStatus?> GetChangeOrderStatus(int changeOrderId)
    {
        var d = await _db.ProjectDocs.AsNoTracking().Include(x => x.Signatures).FirstOrDefaultAsync(x => x.ChangeOrderId == changeOrderId);
        if (d == null) return null;
        return new ChangeOrderSigningStatus
        {
            DocumentId = d.Id, ProjectId = d.ProjectId, DocumentStatus = d.Status, CurrentRevisionId = d.CurrentRevisionId,
            Signers = d.Signatures.OrderBy(s => s.SignOrder).Select(s => new ChangeOrderSignerStatus
            {
                SignatureId = s.Id, Name = s.Name, Email = s.SignerEmail, Status = s.Status, SentDate = s.SentDate, OpenedDate = s.OpenedDate,
                SignedDate = s.SignedDate, TokenExpiresOn = s.TokenExpiresOn, EvidenceRef = s.EvidenceRef
            }).ToList()
        };
    }

    public async Task<List<SignatureEvidence>> GetEvidence(int changeOrderId)
    {
        var d = await _db.ProjectDocs.AsNoTracking().Include(x => x.Signatures).FirstOrDefaultAsync(x => x.ChangeOrderId == changeOrderId);
        if (d == null) return new List<SignatureEvidence>();
        return d.Signatures.Where(s => s.Status == 2 && s.SignedDate.HasValue).OrderBy(s => s.SignOrder).Select(s => new SignatureEvidence
        {
            Name = s.Name, SignedName = s.SignedName ?? s.Name, SignedAt = s.SignedDate!.Value, Method = s.Method, Ip = s.SignedIp,
            EvidenceRef = s.EvidenceRef, DocumentHash = s.DocumentHash, Email = s.SignerEmail
        }).ToList();
    }
}
