#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

// Documentenmodule (migratie 049_DocumentenModel.sql, design-handoff 17a-17e).
// ProjectDocs blijft de bron voor "het document"; deze partial voegt de kolommen van migratie 049 toe.
public partial class ProjectDocs
{
    public int? FolderId { get; set; }
    public string DocumentNumber { get; set; }
    public string AuthoredBy { get; set; }
    /// <summary>Zie <see cref="DocumentStatus"/>.</summary>
    public byte Status { get; set; } = DocumentStatus.Goedgekeurd;
    public DateOnly? ExpiresOn { get; set; }
    /// <summary>Zonder gekoppelde klant: zichtbaar voor alle kopers van het project (brochure).</summary>
    public bool ShareAllBuyers { get; set; }
    public string Perceel { get; set; }
    /// <summary>Bv. de bestelbon wijst naar de gegunde offerte.</summary>
    public int? RelatedDocumentId { get; set; }
    public int? CurrentRevisionId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string CreatedByUserId { get; set; }
    public DateTime? ModifiedDate { get; set; }
    /// <summary>Migratie 054: dit document is de PDF van deze wijzigingsopdracht (ondertekening via link).</summary>
    public int? ChangeOrderId { get; set; }

    public virtual DocumentFolder Folder { get; set; }
    public virtual DocumentRevision CurrentRevision { get; set; }
    public virtual ICollection<DocumentRevision> Revisions { get; set; } = new List<DocumentRevision>();
    public virtual ICollection<DocumentLink> Links { get; set; } = new List<DocumentLink>();
    public virtual ICollection<DocumentSignature> Signatures { get; set; } = new List<DocumentSignature>();
}

/// <summary>Status van een document (ProjectDocs.Status).</summary>
public static class DocumentStatus
{
    public const byte Concept = 0;
    public const byte TerGoedkeuring = 1;
    public const byte Goedgekeurd = 2;
    public const byte Getekend = 3;
    public const byte TerOndertekening = 4;
    public const byte Ingediend = 5;
    public const byte Gegund = 6;
    public const byte NietGegund = 7;
}

public static class DocumentRequestStatus
{
    public const byte Ontbreekt = 0;
    public const byte Aangevraagd = 1;
    public const byte Ontvangen = 2;
    public const byte NietVereist = 3;
}

/// <summary>Wie heeft een revisie geüpload — voor de portalen.</summary>
public static class DocumentUploaderKind
{
    public const byte Intern = 0;
    public const byte Klant = 1;
    public const byte Leverancier = 2;
    public const byte Extern = 3;
}

public partial class DocumentFolder
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public int SortOrder { get; set; }
    /// <summary>default | keuringen | contracten | offertes — bepaalt kolomset en groepering (17c-17e).</summary>
    public string ViewKind { get; set; } = "default";
    public bool IsActive { get; set; } = true;
}

public partial class DocumentRevision
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int RevisionNo { get; set; }
    public string Filename { get; set; }
    public string OriginalFilename { get; set; }
    public long? SizeBytes { get; set; }
    /// <summary>0 concept, 1 ter goedkeuring, 2 goedgekeurd, 3 vervangen/afgewezen.</summary>
    public byte Status { get; set; }
    public string Note { get; set; }
    public decimal? Amount { get; set; }
    public byte UploadedByKind { get; set; }
    public string UploadedByUserId { get; set; }
    public string UploadedByName { get; set; }
    public DateTime UploadedDate { get; set; }
    public string ApprovedByUserId { get; set; }
    public string ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public virtual ProjectDocs Document { get; set; }
}

public partial class DocumentLink
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int? UnitId { get; set; }
    public int? ClientAccountId { get; set; }
    public int? CompanyId { get; set; }
    public bool SharedInPortal { get; set; }
    public DateTime? SeenDate { get; set; }
    public string SeenByName { get; set; }
    public DateTime CreatedDate { get; set; }

    public virtual ProjectDocs Document { get; set; }
    public virtual Units Unit { get; set; }
    public virtual ClientAccount ClientAccount { get; set; }
    public virtual CompanyInfo Company { get; set; }
}

public partial class DocumentSignature
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int? RevisionId { get; set; }
    public int SignOrder { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public int? ClientAccountId { get; set; }
    public string UserId { get; set; }
    /// <summary>0 wacht, 1 geopend, 2 getekend.</summary>
    public byte Status { get; set; }
    public string Method { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? ReminderDate { get; set; }

    // ── Ondertekening via persoonlijke link + e-mailcode (migratie 054, SigningService) ──
    public string SignerEmail { get; set; }
    /// <summary>Wie een melding krijgt zodra deze ondertekenaar tekende (de CPM-gebruiker die het verstuurde).</summary>
    public string NotifyEmail { get; set; }
    /// <summary>SHA-256 (hex) van de token in de link — de token zelf wordt nooit bewaard.</summary>
    public string TokenHash { get; set; }
    public DateTime? TokenExpiresOn { get; set; }
    public string CodeHash { get; set; }
    public DateTime? CodeExpiresOn { get; set; }
    public int CodeAttempts { get; set; }
    public int CodeSentCount { get; set; }
    public DateTime? CodeWindowStart { get; set; }
    public string SignedName { get; set; }
    public string SignedIp { get; set; }
    public string SignedUserAgent { get; set; }
    public string ConsentText { get; set; }
    public string DocumentHash { get; set; }
    public string EvidenceRef { get; set; }

    public virtual ProjectDocs Document { get; set; }
}

public partial class DocumentTemplate
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int FolderId { get; set; }
    public int? ProjectType { get; set; }
    public bool PerUnit { get; set; } = true;
    public string ResponsibleRole { get; set; }
    public string DueLabel { get; set; }
    public int? ExpiryYears { get; set; }
    public int? ReminderDays { get; set; }
    public int? LegacyDocType { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual DocumentFolder Folder { get; set; }
}

public partial class DocumentRequest
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int FolderId { get; set; }
    public int? TemplateId { get; set; }
    public string Name { get; set; }
    public int? UnitId { get; set; }
    public string Perceel { get; set; }
    /// <summary>0 intern, 1 leverancier, 2 klant, 3 extern.</summary>
    public byte ResponsibleKind { get; set; }
    public int? ResponsibleCompanyId { get; set; }
    public int? ResponsibleClientAccountId { get; set; }
    public string ResponsibleName { get; set; }
    public string ResponsibleRole { get; set; }
    public DateOnly? DueDate { get; set; }
    public string DueLabel { get; set; }
    public int? ReminderDaysBefore { get; set; }
    public int? ExpiryYears { get; set; }
    public int? LegacyDocType { get; set; }
    public byte Status { get; set; }
    public DateTime? RequestedDate { get; set; }
    public string RequestedByUserId { get; set; }
    public string RequestedByName { get; set; }
    public DateTime? SeenInPortalDate { get; set; }
    public string SeenByName { get; set; }
    public DateTime? LastReminderDate { get; set; }
    public bool ShareWithBuyerAfterApproval { get; set; } = true;
    public int? FulfilledDocumentId { get; set; }
    public string Note { get; set; }
    public DateTime CreatedDate { get; set; }

    public virtual Project Project { get; set; }
    public virtual DocumentFolder Folder { get; set; }
    public virtual DocumentTemplate Template { get; set; }
    public virtual Units Unit { get; set; }
    public virtual CompanyInfo ResponsibleCompany { get; set; }
    public virtual ClientAccount ResponsibleClientAccount { get; set; }
    public virtual ProjectDocs FulfilledDocument { get; set; }
}
