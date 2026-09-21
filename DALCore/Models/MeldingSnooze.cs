#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Server-side snooze-record voor het gl-v2 dashboard-meldingenscherm (design-handoff 7b). De
/// onderliggende "meldingen" (verzekeringswaarschuwingen, projectinfo, aannemer-comments) hebben
/// zelf geen stabiele, uniforme Id over hun drie bronnen heen — <see cref="MeldingKey"/> is daarom
/// een SHA2_256-hash van "ProjectId|Category|Tekst", berekend door <c>MeldingKeyHelper</c>.
/// </summary>
public partial class MeldingSnooze
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>SHA2_256-hash (hex, 64 tekens) van "ProjectId|Category|Tekst" — zie MeldingKeyHelper.</summary>
    public string MeldingKey { get; set; }

    public DateTime SnoozedUntil { get; set; }

    public DateTime CreatedDate { get; set; }
}
