#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

// Migratie 070_ChangeOrderNumbering.sql: publieke nummering los van het interne Id.
public partial class ChangeOrder
{
    /// <summary>Migratie 131 — het project van het document. Staat los van het contract (dat optioneel is);
    /// een rij die zonder ProjectId aangemaakt wordt krijgt het uit haar contract in cpmRunningContext.SaveChanges.</summary>
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public virtual ICollection<ChangeOrderAttachment> Attachments { get; set; } = new List<ChangeOrderAttachment>();
    /// <summary>Migratie 140 — de leveranciersgroepen (28t); de regels hangen via ChangeOrderDetail.GroupId aan een groep.</summary>
    public virtual ICollection<ChangeOrderGroup> Groups { get; set; } = new List<ChangeOrderGroup>();

    public int? NumberProjectId { get; set; }
    /// <summary>Afkorting van de projectnaam in het nummer (migratie 139), vastgelegd bij het nummeren.</summary>
    public string NumberProjectTag { get; set; }
    public int? NumberYear { get; set; }
    public int? NumberSeq { get; set; }
    /// <summary>1 = eerste versie; elke "nieuwe versie" (SourceKind 2) telt er één bij.</summary>
    public int VersionNo { get; set; } = 1;
    /// <summary>De eerste versie van dit document; NULL bij de eerste versie zelf.</summary>
    public int? RootChangeOrderId { get; set; }

    /// <summary>Laatste keer dat het scherm is opgeslagen (migratie 071).</summary>
    public DateTime? SavedAt { get; set; }

    /// <summary>Onderwerpregel boven de inleiding op de PDF (migratie 072); max. 150 tekens.</summary>
    public string Subject { get; set; }

    /// <summary>"OF-2026-014" / "WO-2026-006-v2" (versiesuffix vanaf versie 2).</summary>
    public string PublicNumber => ChangeOrderNumbering.Format(IsQuote, NumberYear, NumberSeq, VersionNo, Id, NumberProjectTag);
}
