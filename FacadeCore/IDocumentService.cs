using DALCore.Models;

namespace FacadeCore;

/// <summary>
/// Documentenmodule (design-handoff 17a-17e): één document → 0-n koppelingen (eenheid / klant / leverancier),
/// revisies, delen per portaal, ondertekening, verwachte documenten en offertegunning.
/// De portalen (klant/leverancier) komen later; <see cref="GetPortalDocuments"/> en de revisie-/aanvraagvelden
/// (UploadedByKind, SeenInPortalDate, ...) zijn er al klaar voor.
/// </summary>
public interface IDocumentService
{
    // ---- Lezen ----
    Task<DocOverview> GetOverview(int projectId, DocFilter filter);
    Task<DocDetail?> GetDetail(int projectId, int documentId);
    Task<DocRequestDetail?> GetRequestDetail(int projectId, int requestId);
    Task<DocCard> GetCard(DocCardScope scope);
    /// <summary>Wat een klant/leverancier in zijn portaal ziet (enkel huidige, goedgekeurde revisie, enkel gedeelde documenten).</summary>
    Task<DocPortalView> GetPortalDocuments(DocPortalAudience audience);
    Task<DocRevisionFile?> GetRevisionFile(int projectId, int revisionId);
    Task<List<DocFolderNav>> GetFolders();

    // ---- Schrijven: documenten en revisies ----
    /// <summary>Nieuw document (met eerste revisie) of nieuwe revisie op een bestaand document / vervulling van een verwacht document.</summary>
    Task<DocResult> Upload(DocUploadDto dto);
    Task<DocResult> UpdateDocument(int projectId, int documentId, DocUpdateDto dto, string? userId);
    Task<DocResult> ApproveRevision(int projectId, int revisionId, string? userId, string? userName);
    Task<DocResult> RejectRevision(int projectId, int revisionId, string? note, string? userId, string? userName);
    Task<DocResult> DeleteDocument(int projectId, int documentId);

    // ---- Koppelingen en delen ----
    Task<DocResult> AddLink(int projectId, int documentId, string type, int targetId, bool share);
    Task<DocResult> RemoveLink(int projectId, int documentId, int linkId);
    Task<DocResult> SetShare(int projectId, int documentId, string audience, bool share);
    Task<DocResult> SetLinkShare(int projectId, int documentId, int linkId, bool share);

    // ---- Verwachte / aangevraagde documenten ----
    Task<DocResult> CreateRequest(DocRequestDto dto, string? userId, string? userName);
    Task<DocResult> UpdateRequest(int projectId, int requestId, DocRequestDto dto);
    Task<DocResult> MarkRequested(int projectId, int requestId, string? userId, string? userName);
    Task<DocResult> MarkReminded(int projectId, int requestId);
    Task<DocResult> SetRequestNotRequired(int projectId, int requestId, bool notRequired);
    Task<DocResult> DeleteRequest(int projectId, int requestId);
    /// <summary>Maakt uit de sjablonen de verwachte documenten aan per eenheid/project (idempotent). Geeft het aantal nieuwe.</summary>
    Task<int> GenerateExpected(int projectId, string? userId);
    Task<(string? Email, string? Name)> GetRequestRecipient(int projectId, int requestId);

    // ---- Ondertekening (contracten) ----
    Task<DocResult> SendForSignature(int projectId, int documentId, List<DocSignatoryDto> signatories);
    Task<DocResult> MarkSigned(int projectId, int signatureId, string method);
    Task<DocResult> MarkSignatureReminded(int projectId, int signatureId);

    // ---- Offertes ----
    Task<DocResult> Award(int projectId, int documentId, string? userId, string? userName);
}

public class DocResult
{
    public bool Ok { get; set; } = true;
    public string? Message { get; set; }
    public int? Id { get; set; }
    public static DocResult Success(string? message = null, int? id = null) => new() { Ok = true, Message = message, Id = id };
    public static DocResult Fail(string message) => new() { Ok = false, Message = message };
}

public class DocFilter
{
    /// <summary>Mapcode (plannen, verkoop, ...) of null = alle documenten.</summary>
    public string? Folder { get; set; }
    /// <summary>goedkeuring | vervalt | ontbreekt | gedeeld | null.</summary>
    public string? Smart { get; set; }
    public int? UnitId { get; set; }
    public int? ClientAccountId { get; set; }
    public int? CompanyId { get; set; }
}

public class DocOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Sub { get; set; }
    /// <summary>Eenheid: het id van de koper (ClientAccount) — voor "Fam. X is koper van Lot 1 — ook koppelen?".</summary>
    public int? Ref { get; set; }
}

public class DocFolderNav
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
    public string ViewKind { get; set; } = "default";
    public int Count { get; set; }
    public int Missing { get; set; }
}

public class DocSmartNav
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public int Count { get; set; }
    /// <summary>attention | danger | neutral</summary>
    public string Tone { get; set; } = "neutral";
}

public class DocScopeCounts
{
    public int All { get; set; }
    public int Project { get; set; }
    public int Units { get; set; }
    public int Clients { get; set; }
    public int Suppliers { get; set; }
}

public class DocChip
{
    /// <summary>project | unit | client | company</summary>
    public string Type { get; set; } = "project";
    public int? Id { get; set; }
    public string Label { get; set; } = "";
    public bool Shared { get; set; }
    /// <summary>Id van de DocumentLink (voor ontkoppelen/delen); 0 voor project.</summary>
    public int LinkId { get; set; }
}

public class DocSignatureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "";
    public string? Role { get; set; }
    /// <summary>0 wacht, 1 geopend, 2 getekend</summary>
    public byte Status { get; set; }
    public string? Method { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? ReminderDate { get; set; }
}

public class DocRow
{
    /// <summary>doc | request</summary>
    public string Kind { get; set; } = "doc";
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? SubLine { get; set; }
    public string FolderCode { get; set; } = "";
    public string FolderName { get; set; } = "";
    public string Ext { get; set; } = "";
    public List<DocChip> Chips { get; set; } = new();
    public string RevLabel { get; set; } = "—";
    /// <summary>Huidige revisie (voor "Openen").</summary>
    public int? RevisionId { get; set; }
    public int PendingRevisions { get; set; }
    /// <summary>Statuscode voor CSS: concept | pending | ok | signed | signing | submitted | awarded | notawarded | missing | requested | expired | expiring | notrequired</summary>
    public string StatusKey { get; set; } = "ok";
    public string StatusLabel { get; set; } = "";
    public DateOnly? ExpiresOn { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool ShareClient { get; set; }
    public bool ShareSupplier { get; set; }
    public decimal? Amount { get; set; }
    public string? Perceel { get; set; }
    public string? Responsible { get; set; }
    public string? DueText { get; set; }
    public int SignedCount { get; set; }
    public int SignTotal { get; set; }
    public List<DocSignatureDto> Signatures { get; set; } = new();
    public bool IsMissing { get; set; }
    /// <summary>Concept van de leverancier / nog niet ingediend: telt niet mee.</summary>
    public bool IsDraft { get; set; }
    public int? UnitId { get; set; }
    public int? ClientAccountId { get; set; }
    public int? CompanyId { get; set; }
    public string Audiences { get; set; } = "";
    public string Scopes { get; set; } = "";
    public string SearchText { get; set; } = "";
}

public class DocGroup
{
    public string Key { get; set; } = "";
    /// <summary>folder | unit | client | company | project</summary>
    public string Kind { get; set; } = "folder";
    public string Label { get; set; } = "";
    public string? Sub { get; set; }
    public string? Icon { get; set; }
    public int Covered { get; set; }
    public int CoverageTotal { get; set; }
    public bool ShowCoverage { get; set; }
    public int MissingCount { get; set; }
    public List<DocRow> Rows { get; set; } = new();
}

public class DocOverview
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int Total { get; set; }
    public string? ActiveFolder { get; set; }
    public string? ActiveSmart { get; set; }
    public string ViewKind { get; set; } = "default";
    public DocFolderNav? ActiveFolderNav { get; set; }
    public List<DocFolderNav> Folders { get; set; } = new();
    public List<DocSmartNav> Smart { get; set; } = new();
    public DocScopeCounts Scope { get; set; } = new();
    public List<DocGroup> Groups { get; set; } = new();
    public List<DocOption> Units { get; set; } = new();
    public List<DocOption> Clients { get; set; } = new();
    public List<DocOption> Companies { get; set; } = new();
    public List<string> Percelen { get; set; } = new();
    public int SharedCount { get; set; }
    public int RowCount { get; set; }
    public bool HasTemplates { get; set; }
}

public class DocRevisionDto
{
    public int Id { get; set; }
    public int No { get; set; }
    public string Label { get; set; } = "";
    /// <summary>0 concept, 1 ter goedkeuring, 2 goedgekeurd, 3 vervangen/afgewezen</summary>
    public byte Status { get; set; }
    public bool IsCurrent { get; set; }
    public string? Note { get; set; }
    public decimal? Amount { get; set; }
    public decimal? AmountDelta { get; set; }
    public string? UploadedByName { get; set; }
    public byte UploadedByKind { get; set; }
    public DateTime UploadedDate { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public long? SizeBytes { get; set; }
    public string? OriginalFilename { get; set; }
}

public class DocOfferCompare
{
    public int DocumentId { get; set; }
    public string Company { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsThis { get; set; }
    public byte Status { get; set; }
}

public class DocDetail
{
    public int ProjectId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Ext { get; set; } = "";
    public string FolderCode { get; set; } = "";
    public string FolderName { get; set; } = "";
    public string ViewKind { get; set; } = "default";
    public int FolderId { get; set; }
    public string? SubLine { get; set; }
    public string StatusKey { get; set; } = "ok";
    public string StatusLabel { get; set; } = "";
    public byte Status { get; set; }
    public string? DocumentNumber { get; set; }
    public string? AuthoredBy { get; set; }
    public DateOnly? DocumentDate { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? Perceel { get; set; }
    public bool ShareAllBuyers { get; set; }
    public bool IsFrozen { get; set; }
    public int? CurrentRevisionId { get; set; }
    public int? PreviewRevisionId { get; set; }
    public string? PreviewLabel { get; set; }
    public List<DocChip> Chips { get; set; } = new();
    public List<DocRevisionDto> Revisions { get; set; } = new();
    public int PendingRevisionId { get; set; }
    public List<DocSignatureDto> Signatures { get; set; } = new();
    public bool ShareClientOn { get; set; }
    public bool HasClientAudience { get; set; }
    public List<DocOfferCompare> Offers { get; set; } = new();
    public decimal? CurrentAmount { get; set; }
    public string? UnitEffectHint { get; set; }
    public string? SupplierName { get; set; }
}

public class DocRequestDetail
{
    public int ProjectId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FolderName { get; set; } = "";
    public int FolderId { get; set; }
    public string? UnitLabel { get; set; }
    public int? UnitId { get; set; }
    public string StatusKey { get; set; } = "missing";
    public string StatusLabel { get; set; } = "";
    public byte Status { get; set; }
    public string? ResponsibleName { get; set; }
    public string? ResponsibleEmail { get; set; }
    public string ResponsibleKind { get; set; } = "intern";
    public int? ResponsibleCompanyId { get; set; }
    public int? ResponsibleClientAccountId { get; set; }
    public bool ShareAfter { get; set; }
    public DateTime? RequestedDate { get; set; }
    public string? RequestedByName { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? DueLabel { get; set; }
    public int? ReminderDaysBefore { get; set; }
    public int? ExpiryYears { get; set; }
    public DateTime? SeenInPortalDate { get; set; }
    public string? SeenByName { get; set; }
    public DateTime? LastReminderDate { get; set; }
    public string? BuyerName { get; set; }
    public string? Note { get; set; }
    public string? Perceel { get; set; }
    public int? FulfilledDocumentId { get; set; }
}

public class DocUploadDto
{
    public int ProjectId { get; set; }
    /// <summary>new | revision</summary>
    public string Mode { get; set; } = "new";
    public int? DocumentId { get; set; }
    public int? RequestId { get; set; }
    public string? Name { get; set; }
    public int? FolderId { get; set; }
    public DateOnly? DocDate { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? Number { get; set; }
    public string? AuthoredBy { get; set; }
    public string? Perceel { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
    public List<DocLinkRef> Links { get; set; } = new();
    public bool ShareClients { get; set; }
    public bool ShareSuppliers { get; set; }
    public byte? Status { get; set; }
    public byte UploaderKind { get; set; }
    public string StoredFilename { get; set; } = "";
    public string? OriginalFilename { get; set; }
    public long? SizeBytes { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
}

public class DocLinkRef
{
    /// <summary>unit | client | company</summary>
    public string Type { get; set; } = "";
    public int Id { get; set; }
}

public class DocUpdateDto
{
    public string Name { get; set; } = "";
    public int? FolderId { get; set; }
    public string? Number { get; set; }
    public string? AuthoredBy { get; set; }
    public DateOnly? DocDate { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? Perceel { get; set; }
}

public class DocRequestDto
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public int FolderId { get; set; }
    public int? UnitId { get; set; }
    public string? Perceel { get; set; }
    /// <summary>intern | leverancier | klant | extern</summary>
    public string ResponsibleKind { get; set; } = "leverancier";
    public int? CompanyId { get; set; }
    public int? ClientAccountId { get; set; }
    public string? ResponsibleName { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? DueLabel { get; set; }
    public int? ReminderDaysBefore { get; set; }
    public int? ExpiryYears { get; set; }
    public bool ShareWithBuyerAfterApproval { get; set; } = true;
    public string? Note { get; set; }
    public bool SendNow { get; set; }
}

public class DocSignatoryDto
{
    public string Name { get; set; } = "";
    public string? Role { get; set; }
    public int? ClientAccountId { get; set; }
    public string? UserId { get; set; }
}

public class DocRevisionFile
{
    public int RevisionId { get; set; }
    public int DocumentId { get; set; }
    public string Filename { get; set; } = "";
    public string? OriginalFilename { get; set; }
}

public class DocCardScope
{
    public int ProjectId { get; set; }
    public int? UnitId { get; set; }
    public int? ClientAccountId { get; set; }
    public int? CompanyId { get; set; }
}

public class DocCardItem
{
    public string Kind { get; set; } = "doc";
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Ext { get; set; } = "";
    public string? SubLine { get; set; }
    public string StatusKey { get; set; } = "ok";
    public string StatusLabel { get; set; } = "";
    /// <summary>direct = rechtstreeks gekoppeld; via = via project/eenheid/klant.</summary>
    public bool Direct { get; set; }
    public string? ViaLabel { get; set; }
}

public class DocCard
{
    public int DirectCount { get; set; }
    public int ViaCount { get; set; }
    public bool ShowProjectColumn { get; set; }
    public List<DocCardItem> Items { get; set; } = new();
}

public class DocPortalAudience
{
    /// <summary>klant | leverancier</summary>
    public string Kind { get; set; } = "klant";
    public int? ClientAccountId { get; set; }
    public int? CompanyId { get; set; }
}

public class DocPortalItem
{
    public int DocumentId { get; set; }
    public int RevisionId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public string? RevisionLabel { get; set; }
    public DateTime Date { get; set; }
    public bool IsSigned { get; set; }
    public bool IsNew { get; set; }
    public string Filename { get; set; } = "";
}

public class DocPortalRequestItem
{
    public int RequestId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public string? UnitLabel { get; set; }
}

public class DocPortalView
{
    public List<DocPortalItem> Items { get; set; } = new();
    /// <summary>Leverancier: aangevraagde documenten met een uploadknop.</summary>
    public List<DocPortalRequestItem> Requests { get; set; } = new();
}
