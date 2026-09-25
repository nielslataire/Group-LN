namespace CPMCore.Models.Projecten;

/// <summary>Invoer voor <c>Views/Projecten/Partials/_ProjectFormV2SearchSelect.cshtml</c> — het zoekende
/// gl-v2 keuzeveld op Projecten/EditV2 + ToevoegenV2 (Gemeente, weerstation, bedrijven/partners).
/// Bindt een id-veld (en optioneel een verborgen weergavenaam-veld, zoals <c>Project.Developer.Display</c>)
/// en zoekt via een bestaande POST-lookup die <c>[{ id, text }]</c> teruggeeft (GetCompanys,
/// GetPostcodesByCountry, GetWheaterstations). De open/zoek-logica zit in gl-v2-projecten-form.js.</summary>
public class ProjectSearchSelectVm
{
    public string Label { get; set; } = "";
    public bool Required { get; set; }

    /// <summary>Veldnaam van het gebonden id, bv. "Project.Developer.ID" of "SelectedPostalcode".</summary>
    public string IdName { get; set; } = "";
    public string? IdValue { get; set; }

    /// <summary>Optioneel: veldnaam van het gebonden weergavenaam-veld, bv. "Project.Developer.Display".</summary>
    public string? TextName { get; set; }
    public string? TextValue { get; set; }

    /// <summary>Wat de gesloten trigger toont zolang er een waarde is (leeg = placeholder).</summary>
    public string? DisplayText { get; set; }
    public string Placeholder { get; set; } = "Zoeken …";

    public string LookupUrl { get; set; } = "";
    /// <summary>CSS-selector van het (verborgen) landveld waarvan de waarde als <c>countryId</c> meegaat.</summary>
    public string? CountrySelector { get; set; }
    public int MinChars { get; set; } = 2;

    public string? Help { get; set; }
    public string? Error { get; set; }
    /// <summary>Foutmelding voor de client-side verplicht-controle (zet <c>data-gl-v2-required</c> op het veld).</summary>
    public string? RequiredMessage { get; set; }
}

/// <summary>Eén rij van de "Verplichte documenten"-tabel (Projecten/EditV2, tab Documenten).</summary>
public class ProjectRequiredDocRowVm
{
    public string FieldName { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Required { get; set; }
    public DateOnly? DeliveredOn { get; set; }
}
