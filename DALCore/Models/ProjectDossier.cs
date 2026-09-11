#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Generiek dossier/workstream binnen een project (nutsaansluiting, vergunning, akte, verzekering, …).
/// Getypeerde gemene kolommen + <see cref="DataJson"/> voor de lange staart per <c>DossierKind</c>;
/// het lanceringstype (nutsaansluiting) heeft daarnaast een getypeerde companion, <see cref="ProjectNutsAansluiting"/>.
/// </summary>
public partial class ProjectDossier
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    /// <summary>Optioneel — dossier voor één specifieke eenheid (bv. Wet Breyne-dossier van een unit).</summary>
    public int? UnitId { get; set; }

    /// <summary>BOCore.DossierKind.</summary>
    public int DossierKind { get; set; }

    public string Titel { get; set; }

    /// <summary>Extern dossier-/referentienummer (bv. bij de netbeheerder of gemeente).</summary>
    public string Referentie { get; set; }

    /// <summary>BOCore.DossierStatus.</summary>
    public int Status { get; set; }

    /// <summary>BOCore.MijlpaalPartijType.</summary>
    public int? VerantwoordelijkePartijType { get; set; }

    public int? VerantwoordelijkePartijId { get; set; }

    public string VerantwoordelijkeUserId { get; set; }

    public string ExterneContactNaam { get; set; }

    public string ExterneContactEmail { get; set; }

    public DateOnly? AanvraagDatum { get; set; }

    public DateOnly? VerwachteAfhandelingDatum { get; set; }

    public DateOnly? AfgehandeldDatum { get; set; }

    public decimal? Bedrag { get; set; }

    public string Omschrijving { get; set; }

    /// <summary>Kind-specifieke velden die (nog) geen eigen getypeerde companion hebben.</summary>
    public string DataJson { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedByUserId { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    public virtual Project Project { get; set; }

    public virtual Units Unit { get; set; }

    public virtual ProjectNutsAansluiting NutsAansluiting { get; set; }

    public virtual ICollection<ProjectDossierGebeurtenis> Gebeurtenissen { get; set; } = new List<ProjectDossierGebeurtenis>();

    public virtual ICollection<ProjectDossierDocument> Documenten { get; set; } = new List<ProjectDossierDocument>();

    public virtual ICollection<ProjectDossierMijlpaal> DossierMijlpalen { get; set; } = new List<ProjectDossierMijlpaal>();

    public virtual ICollection<ProjectDossierSubstap> Substappen { get; set; } = new List<ProjectDossierSubstap>();
}
