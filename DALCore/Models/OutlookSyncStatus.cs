#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Opt-in en syncstatus van de Outlook-koppeling van een gebruiker (migratie 121). Eén rij per gebruiker; bevat geen mail- of agenda-inhoud.</summary>
public partial class OutlookSyncStatus
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>De gebruiker heeft de koppeling aangezet. Standaard uit.</summary>
    public bool Aan { get; set; }

    /// <summary>Moment van aanzetten (UTC): mails worden enkel vanaf dan opgehaald.</summary>
    public DateTime? AangezetOp { get; set; }

    public DateTime? LaatsteSyncOp { get; set; }

    /// <summary>wacht, ok, geen-toegang, geen-mailbox of fout (zie OutlookSyncCodes).</summary>
    public string Status { get; set; } = "wacht";

    public string LaatsteFout { get; set; }

    public int FoutenOpRij { get; set; }

    /// <summary>JSON: Graph-deltatoken per map.</summary>
    public string DeltaTokensJson { get; set; }

    public DateTime GewijzigdOp { get; set; }

    public virtual Users User { get; set; }
}
