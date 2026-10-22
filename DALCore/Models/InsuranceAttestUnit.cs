#nullable disable

namespace DALCore.Models;

/// <summary>Voor welke woningen een tienjarige-attest van een aannemer geldt (migratie 111).</summary>
public partial class InsuranceAttestUnit
{
    public int AttestId { get; set; }

    public int UnitId { get; set; }
}
