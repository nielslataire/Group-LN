#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Concrete mijlpaal (keypoint) binnen een <see cref="Projecttraject"/>. Kan handmatig worden bijgewerkt
/// of (vanaf increment 2) via een <c>BronBinding</c> uit bestaande records worden afgeleid.
/// </summary>
public partial class Mijlpaal
{
    public int Id { get; set; }

    public int ProjecttrajectId { get; set; }

    public int? ProjecttrajectFaseId { get; set; }

    /// <summary>Null = mijlpaal op projectniveau; gezet = mijlpaal voor die ene eenheid.</summary>
    public int? UnitId { get; set; }

    public int? SjabloonMijlpaalId { get; set; }

    public string Code { get; set; }

    public string Naam { get; set; }

    public int Volgorde { get; set; }

    /// <summary>BOCore.Traject.MijlpaalType.</summary>
    public int MijlpaalType { get; set; }

    /// <summary>BOCore.Traject.MijlpaalStatus.</summary>
    public int Status { get; set; }

    /// <summary>Handmatig gezette streefdatum; heeft voorrang op <see cref="DoeldatumBerekend"/>.</summary>
    public DateOnly? Doeldatum { get; set; }

    /// <summary>Uit ankerdatum + offset berekende streefdatum.</summary>
    public DateOnly? DoeldatumBerekend { get; set; }

    /// <summary>Datum waarop de mijlpaal effectief bereikt werd.</summary>
    public DateOnly? WerkelijkeDatum { get; set; }

    /// <summary>BOCore.Traject.InterneRol.</summary>
    public int? VerantwoordelijkeRol { get; set; }

    /// <summary>BOCore.Traject.PartijType.</summary>
    public int? VerantwoordelijkePartijType { get; set; }

    public int? VerantwoordelijkePartijId { get; set; }

    public string VerantwoordelijkeUserId { get; set; }

    /// <summary>BOCore.Traject.ComputedBinding.</summary>
    public int? BronBinding { get; set; }

    public string BronParam { get; set; }

    public int? BronRefId { get; set; }

    /// <summary>FK naar ProjectDossier (increment 4+).</summary>
    public int? DossierId { get; set; }

    public bool IsVerplicht { get; set; }

    public string Opmerking { get; set; }

    public DateTime? LastComputedDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedByUserId { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    public virtual Projecttraject Projecttraject { get; set; }

    public virtual ProjecttrajectFase ProjecttrajectFase { get; set; }

    public virtual Units Unit { get; set; }

    public virtual ICollection<MijlpaalHistoriek> Historiek { get; set; } = new List<MijlpaalHistoriek>();

    public virtual ICollection<MijlpaalAfhankelijkheid> Afhankelijkheden { get; set; } = new List<MijlpaalAfhankelijkheid>();
}
