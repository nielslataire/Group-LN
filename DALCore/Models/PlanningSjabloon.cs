#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Een planningssjabloon (migratie 146): blokken met taken die op een bouwdeel toegepast worden.</summary>
public partial class PlanningSjabloon
{
    public int Id { get; set; }
    public string Naam { get; set; }
    public string Beschrijving { get; set; }
    public bool IsActief { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string CreatedByUserId { get; set; }

    /// <summary>Wie het sjabloon het laatst wijzigde (migratie 147).</summary>
    public string ModifiedByUserId { get; set; }

    /// <summary>Waarop het sjabloon later automatisch voorgesteld wordt: null = niets (handmatig toepassen), anders een sleutel zoals "traject-start" of "type-woning" (migratie 147).</summary>
    public string ReageertOp { get; set; }

    public virtual ICollection<PlanningSjabloonBlok> Blokken { get; set; } = new List<PlanningSjabloonBlok>();
}

/// <summary>Een blok (wordt een fase): altijd, voorwaardelijk (kelder, dak) of herhaald per verdieping.</summary>
public partial class PlanningSjabloonBlok
{
    public int Id { get; set; }
    public int SjabloonId { get; set; }
    public string Code { get; set; }
    public string Naam { get; set; }
    public int Volgorde { get; set; }

    /// <summary>0 = altijd, 1 = herhaald per verdieping.</summary>
    public byte Herhaling { get; set; }

    public bool VolgtVorigeVerdieping { get; set; }

    /// <summary>"kelder" of "dak"; samen met <see cref="VoorwaardeWaarde"/> ("ja", "hellend", "plat"). Beide null = altijd.</summary>
    public string VoorwaardeVariabele { get; set; }
    public string VoorwaardeWaarde { get; set; }

    public virtual PlanningSjabloon Sjabloon { get; set; }
    public virtual ICollection<PlanningSjabloonTaak> Taken { get; set; } = new List<PlanningSjabloonTaak>();
}

public partial class PlanningSjabloonTaak
{
    public int Id { get; set; }
    public int BlokId { get; set; }
    public string Code { get; set; }
    public string Naam { get; set; }
    public string Beschrijving { get; set; }
    public int Duur { get; set; } = 1;
    public int Volgorde { get; set; }

    /// <summary>Wacht bij meerdere bouwdelen op dezelfde taak in het vorige bouwdeel.</summary>
    public bool Estafette { get; set; }
    public int EstafetteWacht { get; set; }

    /// <summary>Voorwaarde van de taak, bv. "laag:eerste,kelder:nee" (zie PlanningSjabloonUitrol.TaakVoorwaarden); leeg = altijd.</summary>
    public string Voorwaarde { get; set; }

    public virtual PlanningSjabloonBlok Blok { get; set; }
    public virtual ICollection<PlanningSjabloonVoorganger> Voorgangers { get; set; } = new List<PlanningSjabloonVoorganger>();
}

public partial class PlanningSjabloonVoorganger
{
    public int Id { get; set; }
    public int TaakId { get; set; }

    /// <summary>Code van een taak of blok in hetzelfde sjabloon.</summary>
    public string VoorgangerCode { get; set; }
    public byte Type { get; set; }
    public int WachtDagen { get; set; }

    public virtual PlanningSjabloonTaak Taak { get; set; }
}
