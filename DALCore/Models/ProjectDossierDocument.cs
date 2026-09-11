#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Koppelt een document aan een dossier — bij voorkeur naar een bestaande <see cref="ProjectDocs"/>-rij.</summary>
public partial class ProjectDossierDocument
{
    public int Id { get; set; }

    public int ProjectDossierId { get; set; }

    public int? ProjectDocId { get; set; }

    /// <summary>Ruwe Storage-bestandsverwijzing — enkel gebruikt als er geen <see cref="ProjectDocId"/> is.</summary>
    public string FileId { get; set; }

    public string Naam { get; set; }

    public int? DocType { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedByUserId { get; set; }

    public virtual ProjectDossier ProjectDossier { get; set; }

    public virtual ProjectDocs ProjectDoc { get; set; }
}
