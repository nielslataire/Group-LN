#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Ondertekeningsdossier — de aggregate root van de signingmodule (ONDERTEKENEN_VOORSTEL.md §3.2).
/// Kent het brondocument enkel als (<see cref="DocumentType"/>, <see cref="SourceEntityId"/>); de
/// bron zelf (bv. <c>ChangeOrder</c>) heeft géén verwijzing terug. Beleid, akkoordtekst en de
/// documentbytes zijn snapshots op het moment van aanbieden.
/// </summary>
public partial class SigningCase
{
    public int Id { get; set; }

    /// <summary>Publiek verificatie-ID (op het ondertekeningsblad, in de verificatiepagina). Nooit het interne Id naar buiten.</summary>
    public Guid PublicVerificationId { get; set; }

    /// <summary>Sleutel van de <c>ISigningDocumentSource</c>, bv. "ChangeOrder".</summary>
    public string DocumentType { get; set; }

    /// <summary>Vingerafdruk (SHA-256 hex) van de broninhoud op het moment van aanmaken; zie <c>ISigningDocumentSource.ComputeFingerprintAsync</c>.</summary>
    public string SourceFingerprint { get; set; }

    /// <summary>Id van het brondocument binnen zijn eigen tabel (bv. ChangeOrder.Id).</summary>
    public int SourceEntityId { get; set; }

    public int? ProjectId { get; set; }

    public int? ClientAccountId { get; set; }

    public string Title { get; set; }

    public string DocumentNumber { get; set; }

    public string Summary { get; set; }

    public decimal? AmountExclVat { get; set; }

    public decimal? VatAmount { get; set; }

    public decimal? AmountInclVat { get; set; }

    /// <summary>BOCore.SigningCaseStatus.</summary>
    public int Status { get; set; }

    /// <summary>BOCore.SigningRule (snapshot uit het beleid, per dossier overschrijfbaar).</summary>
    public int SigningRule { get; set; }

    /// <summary>Snapshot: sleutel van de ondertekenmethode, bv. "internal-ses".</summary>
    public string SignatureMethod { get; set; }

    /// <summary>Referentie van het dossier bij een externe provider (leeg bij SES).</summary>
    public string ProviderCaseRef { get; set; }

    /// <summary>Snapshot uit het beleid.</summary>
    public bool OtpRequired { get; set; }

    /// <summary>Snapshot uit het beleid: sleutel van de verificatiemethode.</summary>
    public string VerificationMethod { get; set; }

    public int OtpValiditySeconds { get; set; }

    public int OtpMaxAttempts { get; set; }

    /// <summary>Snapshot van de akkoordtekst op het moment van aanbieden.</summary>
    public string ConsentTextSnapshot { get; set; }

    public int? OriginalDocumentId { get; set; }

    public int? FinalDocumentId { get; set; }

    public int? AuditReportDocumentId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Moment van aanbieden (Draft → Open).</summary>
    public DateTime? OpenedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public int? ClosedByUserId { get; set; }

    /// <summary>Reden bij annuleren/weigeren.</summary>
    public string CloseReason { get; set; }

    /// <summary>Het nieuwe dossier dat dit dossier verving na een inhoudelijke wijziging.</summary>
    public int? SupersededByCaseId { get; set; }

    public DateTime? RetentionUntil { get; set; }

    public DateTime? RetentionScrubbedAt { get; set; }

    public byte[] RowVersion { get; set; }

    public virtual SigningDocument OriginalDocument { get; set; }

    public virtual SigningDocument FinalDocument { get; set; }

    public virtual SigningDocument AuditReportDocument { get; set; }

    public virtual Project Project { get; set; }

    public virtual ClientAccount ClientAccount { get; set; }

    public virtual ICollection<SigningParty> Parties { get; set; } = new List<SigningParty>();

    public virtual ICollection<SigningDocument> Documents { get; set; } = new List<SigningDocument>();

    public virtual ICollection<SigningEvent> Events { get; set; } = new List<SigningEvent>();
}
