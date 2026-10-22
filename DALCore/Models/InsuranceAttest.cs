#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Wat een aannemer per contract aan verzekeringsattesten heeft afgegeven (migratie 110, design-handoff 43b).
/// Kind: 0 tienjarige, 1 BA. Status: 0 ontbreekt, 1 ontvangen, 2 niet vereist.</summary>
public partial class InsuranceAttest
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int ContractId { get; set; }

    public int Kind { get; set; }

    public int Status { get; set; }

    public int? UnitsRequired { get; set; }

    public int? UnitsReceived { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public string Insurer { get; set; }

    public string PolicyNumber { get; set; }

    public string Note { get; set; }

    public int? DocumentId { get; set; }

    public DateOnly? ReceivedAt { get; set; }

    public DateTime? LastRequestedAt { get; set; }

    public string LastRequestedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Contract Contract { get; set; }
}
