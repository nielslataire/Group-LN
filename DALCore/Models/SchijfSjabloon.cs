#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Een schijvensjabloon (migratie 155): een standaardlijst betalingsschijven die op een betalingsgroep toegepast wordt.</summary>
public partial class SchijfSjabloon
{
    public int Id { get; set; }
    public string Naam { get; set; }
    public string Beschrijving { get; set; }

    /// <summary>Onder dit percentage geeft het uitrekenen een waarschuwing (geen blokkade).</summary>
    public decimal MinimumPercentage { get; set; } = 5m;

    public bool IsActief { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string CreatedByUserId { get; set; }
    public string ModifiedByUserId { get; set; }

    public virtual ICollection<SchijfSjabloonSchijf> Schijven { get; set; } = new List<SchijfSjabloonSchijf>();
}

public partial class SchijfSjabloonSchijf
{
    public int Id { get; set; }
    public int SjabloonId { get; set; }
    public int Volgorde { get; set; }

    /// <summary>Mag {verdieping} bevatten; wordt bij een herhaling per verdieping "1ste verdieping", "2de verdieping", ...</summary>
    public string Naam { get; set; }

    /// <summary>Code van de taak of fase in het planningssjabloon waaraan de schijf hangt.</summary>
    public string PlanningCode { get; set; }

    /// <summary>Vast percentage; leeg = volgt uit het gewicht.</summary>
    public decimal? VastPercentage { get; set; }

    public decimal Gewicht { get; set; } = 1m;
    public bool PerVerdieping { get; set; }

    /// <summary>"kelder" of "dak" (migratie 156); samen met <see cref="VoorwaardeWaarde"/> ("ja", "nee", "hellend", "plat"). Beide leeg = altijd.</summary>
    public string VoorwaardeVariabele { get; set; }
    public string VoorwaardeWaarde { get; set; }

    public virtual SchijfSjabloon Sjabloon { get; set; }
}
