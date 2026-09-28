#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Onveranderlijk bestand in een ondertekeningsdossier (§3.3): origineel, ondertekend document,
/// auditrapport, bijlage of handtekeningafbeelding. De bytes staan in <see cref="Content"/> (SQL is
/// de bewijsbron: één transactie met het dossier, mee in de databaseback-up) én — als spiegel, per
/// beslissing §9.1 — in de Storage API (<see cref="StorageFileName"/>, map "signing"). Bij elke
/// uitlevering wordt <see cref="Sha256"/> opnieuw berekend en vergeleken.
///
/// Bewaarregel: enkel dossiers die effectief ondertekend werden houden hun bytes. Sluit een dossier
/// zonder voltooiing (verlopen, geweigerd, geannuleerd), dan worden <see cref="Content"/> en de
/// spiegelkopie verwijderd; naam, grootte en hash blijven staan zodat de audit trail leesbaar en
/// de hash aantoonbaar blijft. Daarbuiten worden rijen nooit gewijzigd of verwijderd (uitzondering:
/// de retentiescrub verwijdert handtekeningafbeeldingen).
/// </summary>
public partial class SigningDocument
{
    public int Id { get; set; }

    public int SigningCaseId { get; set; }

    /// <summary>De ondertekenaar bij wie dit bestand hoort (enkel bij <c>SignatureImage</c>).</summary>
    public int? SigningPartyId { get; set; }

    /// <summary>BOCore.SigningDocumentKind.</summary>
    public int Kind { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public long ByteLength { get; set; }

    /// <summary>SHA-256 van <see cref="Content"/>, hex, kleine letters (64 tekens).</summary>
    public string Sha256 { get; set; }

    /// <summary>Naam in de Storage API (map "signing"); null wanneer de spiegel-upload mislukte.</summary>
    public string StorageFileName { get; set; }

    public byte[] Content { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Null = aangemaakt door het systeem (bv. het ondertekende document).</summary>
    public int? CreatedByUserId { get; set; }

    public virtual SigningCase SigningCase { get; set; }
}
