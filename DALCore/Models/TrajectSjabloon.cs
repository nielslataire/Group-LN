#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Sjabloon (template) voor een projecttraject, per projecttype. Wordt via de admin-UI beheerd
/// en via <c>TrajectInstantiationService</c> gekopieerd naar een <see cref="Projecttraject"/>.
/// </summary>
public partial class TrajectSjabloon
{
    public int Id { get; set; }

    public string Naam { get; set; }

    /// <summary>BOCore.ProjectType (1 = Woonproject, 2 = Commercieel). Null = alle types.</summary>
    public int? ProjectType { get; set; }

    public bool IsStandaard { get; set; }

    public bool IsActief { get; set; }

    public string Omschrijving { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedByUserId { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    public virtual ICollection<TrajectSjabloonFase> Fases { get; set; } = new List<TrajectSjabloonFase>();

    public virtual ICollection<Projecttraject> Projecttrajecten { get; set; } = new List<Projecttraject>();
}
