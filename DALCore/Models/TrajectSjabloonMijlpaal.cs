#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Mijlpaal-definitie binnen een <see cref="TrajectSjabloonFase"/>.</summary>
public partial class TrajectSjabloonMijlpaal
{
    public int Id { get; set; }

    public int TrajectSjabloonFaseId { get; set; }

    public string Naam { get; set; }

    /// <summary>Stabiele sleutel; ook bruikbaar als anker-code voor andere mijlpalen (<see cref="DoeldatumAnkerCode"/>).</summary>
    public string Code { get; set; }

    public int Volgorde { get; set; }

    /// <summary>BOCore.Traject.MijlpaalType.</summary>
    public int MijlpaalType { get; set; }

    /// <summary>BOCore.Traject.MijlpaalScope — 0 = één mijlpaal op projectniveau, 1 = één mijlpaal per eenheid.</summary>
    public int Scope { get; set; }

    /// <summary>BOCore.Traject.InterneRol.</summary>
    public int? VerantwoordelijkeRol { get; set; }

    /// <summary>Code van de anker-mijlpaal/-gebeurtenis waarop de doeldatum wordt berekend (bv. VERGUNNING_VERLEEND, PROJECT_CREATED).</summary>
    public string DoeldatumAnkerCode { get; set; }

    /// <summary>Aantal dagen na de ankerdatum.</summary>
    public int? DoeldatumOffsetDagen { get; set; }

    public bool IsVerplicht { get; set; }

    /// <summary>BOCore.Traject.ComputedBinding (bindings worden in increment 2 geactiveerd).</summary>
    public int? BronBinding { get; set; }

    public string BronParam { get; set; }

    /// <summary>BOCore.Traject.DossierKind — het dossier-type dat bij deze mijlpaal hoort (increment 4+).</summary>
    public int? DossierKind { get; set; }

    public string Omschrijving { get; set; }

    public virtual TrajectSjabloonFase TrajectSjabloonFase { get; set; }

    public virtual ICollection<TrajectSjabloonMijlpaalTrigger> Triggers { get; set; } = new List<TrajectSjabloonMijlpaalTrigger>();
}
