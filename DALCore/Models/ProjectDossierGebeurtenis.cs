#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Eén regel in de opvolgingstijdlijn van een <see cref="ProjectDossier"/>.</summary>
public partial class ProjectDossierGebeurtenis
{
    public int Id { get; set; }

    public int ProjectDossierId { get; set; }

    public DateTime Datum { get; set; }

    /// <summary>BOCore.DossierGebeurtenisType.</summary>
    public int Type { get; set; }

    public string Titel { get; set; }

    public string Tekst { get; set; }

    public string UserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ProjectDossier ProjectDossier { get; set; }
}
