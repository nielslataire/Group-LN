#nullable disable
namespace DALCore.Models;

// Migratie 092_BudgetParamsOpmetingSondering.sql
public partial class BudgetParams
{
    /// <summary>Opmeting als fractie van de bouwkost (NULL = standaard uit Instellingen &gt; Bouwkost %).</summary>
    public decimal? OpmetingPerc { get; set; }

    /// <summary>Sondering als fractie van de bouwkost (NULL = standaard uit Instellingen &gt; Bouwkost %).</summary>
    public decimal? SonderingPerc { get; set; }

    /// <summary>Registratierechten als fractie van de aankoopprijs grond (migratie 093; NULL = standaard).</summary>
    public decimal? RegistratierechtenPerc { get; set; }

    /// <summary>Publiciteit als fractie van de totale verkoopprijs (migratie 093; NULL = standaard, vervangt het forfait).</summary>
    public decimal? PubliciteitPerc { get; set; }

    /// <summary>Aankopen van de grond als JSON [{o,p}] (migratie 094); leeg = één aankoop ter waarde van AankoopprijsGrond. AankoopprijsGrond blijft het totaal.</summary>
    public string GrondAankopenJson { get; set; }

    /// <summary>Bijkomende notariskosten per akte (migratie 094; NULL = standaard 1.500 EUR).</summary>
    public decimal? AktekostenPerAkte { get; set; }

    /// <summary>Makelaarscommissie als fractie van de verkoopprijs (migratie 095; NULL = standaard uit Bouwkost %).</summary>
    public decimal? MakelaarsCommissiePerc { get; set; }

    /// <summary>Overige verkoopkosten, forfait per project (migratie 095).</summary>
    public decimal? OverigeVerkoopkosten { get; set; }

    /// <summary>Waarde van de tweede grondzone t.o.v. de eerste, als fractie (migratie 096; NULL = 20 %): enkel voor de verdeling van de grondkost over de loten.</summary>
    public decimal? GrondAgrarischPerc { get; set; }
}
