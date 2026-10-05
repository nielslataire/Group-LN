#nullable disable
using System;

namespace DALCore.Models;

// Migratie 070_ChangeOrderNumbering.sql: publieke nummering los van het interne Id.
public partial class ChangeOrder
{
    public int? NumberProjectId { get; set; }
    public int? NumberYear { get; set; }
    public int? NumberSeq { get; set; }
    /// <summary>1 = eerste versie; elke "nieuwe versie" (SourceKind 2) telt er één bij.</summary>
    public int VersionNo { get; set; } = 1;
    /// <summary>De eerste versie van dit document; NULL bij de eerste versie zelf.</summary>
    public int? RootChangeOrderId { get; set; }

    /// <summary>Laatste keer dat het scherm is opgeslagen (migratie 071).</summary>
    public DateTime? SavedAt { get; set; }

    /// <summary>"OF-2026-014" / "WO-2026-006-v2" (versiesuffix vanaf versie 2).</summary>
    public string PublicNumber => ChangeOrderNumbering.Format(IsQuote, NumberYear, NumberSeq, VersionNo, Id);
}
