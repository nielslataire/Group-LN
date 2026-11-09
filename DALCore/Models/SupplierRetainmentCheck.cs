#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Een controle van de inhoudingsplicht (30bis/30ter) van een leverancier bij de RSZ-webservice BillRetainment (migratie 137).</summary>
public partial class SupplierRetainmentCheck
{
    public int Id { get; set; }
    public int? IncomingInvoiceId { get; set; }
    /// <summary>Controlelijst (handmatige controle van meerdere ondernemingen) waartoe deze controle behoort.</summary>
    public Guid? BatchId { get; set; }
    public string SupplierName { get; set; }
    public string EnterpriseIdentifier { get; set; }
    public string EnterpriseNumber { get; set; }
    public string Environment { get; set; }
    public DateTime CheckedAt { get; set; }
    public string CheckedBy { get; set; }
    public string RetainmentCheckReference { get; set; }
    /// <summary>BelGov-Trace-Id van het RSZ-antwoord: audit en supportvragen.</summary>
    public string TraceId { get; set; }
    public string FiscalSituation { get; set; }
    public byte? FiscalWithholdingPercentage { get; set; }
    public DateTime? FiscalValidUntil { get; set; }
    public long? FiscalAttestId { get; set; }
    public string SocialSituation { get; set; }
    public byte? SocialWithholdingPercentage { get; set; }
    public DateTime? SocialValidUntil { get; set; }
    /// <summary>Het ongewijzigde resultaat van de RSZ voor deze onderneming (JSON): het bewijsstuk.</summary>
    public string RawResponse { get; set; }
    public string ErrorMessage { get; set; }
    /// <summary>Naam van de controlelijst (migratie 138); staat op elke rij van de lijst.</summary>
    public string BatchName { get; set; }
}
