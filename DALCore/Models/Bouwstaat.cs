#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Een bouwstaat (migratie 158): een toestand van een eenheid die bereikt kan worden, bv. "Opstort gegoten".
/// Planning (taak of fase levert ze), schijven en traject verwijzen ernaar. Bedrijfsbrede standaardlijst met vaste codes;
/// <see cref="ProjectId"/> gevuld = eigen bouwstaat van één project.
/// </summary>
public partial class Bouwstaat
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Naam { get; set; }

    /// <summary>Groepering in de lijst, bv. "Ruwbouw bovenbouw".</summary>
    public string Groep { get; set; }

    public int Volgorde { get; set; }

    /// <summary>0 = geldt voor het hele gebouw (alle eenheden), 1 = per eenheid.</summary>
    public byte Bereik { get; set; }

    /// <summary>Bestaat per bouwlaag (gelijkvloers, 1ste verdieping, ...).</summary>
    public bool PerBouwlaag { get; set; }

    public bool IsActief { get; set; } = true;

    /// <summary>Leeg = bedrijfsbreed.</summary>
    public int? ProjectId { get; set; }
}
