#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Lichte, persoonlijke/interne taak ("Mijn taken"). Bewust NIET in <see cref="ConstructionIssue"/>
/// geabsorbeerd — dat draagt eigen, defect-specifiek gewicht (media, plan-pins, Werfportaal, eigen
/// notificatie-scheduler). Optioneel gekoppeld via nullable FK's naar project/eenheid/mijlpaal/
/// dossier/punt; kan ook volledig los staan (algemene taak).
/// </summary>
public partial class ProjectTaak
{
    public int Id { get; set; }

    public int? ProjectId { get; set; }

    public int? UnitId { get; set; }

    public int? MijlpaalId { get; set; }

    public int? ProjectDossierId { get; set; }

    public int? ConstructionIssueId { get; set; }

    public string Titel { get; set; }

    public string Omschrijving { get; set; }

    /// <summary>BOCore.TaakStatus.</summary>
    public int Status { get; set; }

    /// <summary>BOCore.TaakPrioriteit.</summary>
    public int Prioriteit { get; set; }

    public string ToegewezenAanUserId { get; set; }

    /// <summary>BOCore.InterneRol — optioneel, als de taak aan een rol i.p.v. een persoon toegewezen is.</summary>
    public int? ToegewezenAanRol { get; set; }

    public DateOnly? Vervaldatum { get; set; }

    public DateTime? AfgewerktOp { get; set; }

    /// <summary>BOCore.TaakHerkomst.</summary>
    public int Herkomst { get; set; }

    public string CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    // Mijn taken als persoonlijke assistent (migratie 120)

    /// <summary>Plek of omstandigheid, bv. "@bureau", "@telefoon", "@werf".</summary>
    public string Context { get; set; }

    public int? SchattingMinuten { get; set; }

    /// <summary>Begin van het geplande blok in de dag/week (lokale tijd).</summary>
    public DateTime? GeplandStart { get; set; }

    public int? GeplandMinuten { get; set; }

    /// <summary>Vastgelegd maar nog niet verwerkt (Mijn taken › Inbox).</summary>
    public bool IsInbox { get; set; }

    public string InboxBron { get; set; }

    public string InboxMeta { get; set; }

    /// <summary>"Ooit / misschien": bewaard, maar niet op de lijst met volgende acties.</summary>
    public bool Ooit { get; set; }

    /// <summary>Gedelegeerd: wie of wat je afwacht (Status = Wachtend).</summary>
    public string WachtOp { get; set; }

    public DateTime? WachtOpDatum { get; set; }

    public virtual Project Project { get; set; }

    public virtual Units Unit { get; set; }

    public virtual Mijlpaal Mijlpaal { get; set; }

    public virtual ProjectDossier ProjectDossier { get; set; }

    public virtual ConstructionIssue ConstructionIssue { get; set; }
}
