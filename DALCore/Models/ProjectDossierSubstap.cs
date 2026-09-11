#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Eén stap in de checklist van een dossier (bv. de stappen van een omgevingsvergunning:
/// ingediend, volledig verklaard, openbaar onderzoek, adviezen, collegebeslissing, beroepstermijn,
/// definitief). Generiek herbruikbaar voor elk <c>DossierKind</c> dat een stappenplan nodig heeft.
/// </summary>
public partial class ProjectDossierSubstap
{
    public int Id { get; set; }

    public int ProjectDossierId { get; set; }

    /// <summary>Stabiele sleutel (bv. "INGEDIEND", "DEFINITIEF") — ook gebruikt als <c>Mijlpaal.BronParam</c> bij de DossierSubstap-binding.</summary>
    public string Code { get; set; }

    public string Naam { get; set; }

    public int Volgorde { get; set; }

    /// <summary>BOCore.DossierSubstapStatus.</summary>
    public int Status { get; set; }

    public DateOnly? Datum { get; set; }

    public string Opmerking { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    public virtual ProjectDossier ProjectDossier { get; set; }
}
