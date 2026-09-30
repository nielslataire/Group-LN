namespace CPMCore.Models.Instellingen;

/// <summary>gl-v2 view-model voor Instellingen/IssuerCompaniesV2 (design-handoff punt 24b, "CRM
/// Instellingen.dc.html"): "de stap tussen overzicht en bewerken, koppeling en standaarden per
/// bedrijf in één oogopslag" — één tabelrij per bedrijf, de hele rij is de link naar bewerken.</summary>
public class IssuerCompanyListItemV2Vm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Ondertitel onder de naam (24b se "BEDRIJF"-kolom toont naam + een subtekst) — hier de
    /// wettelijke naam, als die afwijkt van de weergavenaam.</summary>
    public string? Sub { get; set; }
    public string? VatNumber { get; set; }
    /// <summary>IBAN van de standaardrekening (<see cref="BOCore.IssuerBankAccountBO.IsDefault"/>) —
    /// "—" als er nog geen rekening is.</summary>
    public string Iban { get; set; } = "—";
    /// <summary>Factuursjabloon-naam — vandaag de rauwe <c>TemplateKey</c> (geen aparte
    /// weergavenaam-lookup in deze eerste ronde), "—" als er nog geen sjabloon gekozen is.</summary>
    public string TemplateLabel { get; set; } = "—";
    public string OctopusLabel { get; set; } = "Niet gekoppeld";
    /// <summary>"is-success"/"is-warning"/"is-muted" — zelfde toon-naamgeving als Meldingskaders
    /// (punt 25) en Instellingen/IndexV2 se kaart-meta, geen aparte kleurtaal hier verzinnen.</summary>
    public string OctopusTone { get; set; } = "is-muted";
    public bool IsActive { get; set; }
    public bool IsExternalCoordinationDefault { get; set; }
    public string EditUrl { get; set; } = "#";
}
