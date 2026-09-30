namespace CPMCore.Models.Instellingen;

/// <summary>gl-v2 view-model voor Instellingen/IndexV2 (design-handoff punt 24a, "CRM Instellingen.dc.html"):
/// "groepen als index links, kaarten in een vast raster van drie, de hele kaart is klikbaar · typ in de
/// zoekbalk". Vervangt de losse @if-blokken van de legacy Index.cshtml (die haar canXxx-permissiechecks
/// rechtstreeks in de view deed) door een door de controller opgebouwde lijst — nodig omdat een kaart nu
/// ook een optionele live status (<see cref="SettingsItemVm.MetaLabel"/>, bv. Marktdata-status se laatste
/// crawl) kan tonen, wat een service-aanroep vergt die niet in de view thuishoort.</summary>
public class InstellingenIndexV2Vm
{
    public List<SettingsGroupVm> Groups { get; set; } = new();

    /// <summary>Totaal aantal kaarten over alle groepen — voor de "ONDERDELEN"-navigatie/lege-staat-tekst.</summary>
    public int TotalCount => Groups.Sum(g => g.Items.Count);
}

/// <summary>Eén onderdeel-groep (24a: "ONDERDELEN"-navigatie links, sectiekop rechts). <see cref="Key"/>
/// is de anchor/­id die de linkernavigatie en de sectiekop aan elkaar koppelt (client-side scroll, geen
/// server-side filtering nodig — 24a's "groepen als index links" is puur navigatie binnen één pagina).</summary>
public class SettingsGroupVm
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public List<SettingsItemVm> Items { get; set; } = new();
    public int Count => Items.Count;
}

/// <summary>Eén kaart. De hele kaart is de link (24a: "Knop 'Open beheer' + pijltje deden hetzelfde; nu
/// is de kaart de klik") — <see cref="Href"/> is dus niet enkel voor een los actieknopje, maar voor de
/// kaart als geheel (JS maakt er een klikbare rij van, zelfde discipline als initClickableRows()
/// elders). <see cref="Chips"/> zijn de "tweede ingangen" (24a: Rollen/Groepen) die naast de hoofdklik in
/// blijven bestaan als kleine, eigen-klikbare pilletjes bovenop de kaart-brede link.</summary>
public class SettingsItemVm
{
    /// <summary>Phosphor-icoonklasse zonder het "ph "-voorvoegsel, bv. "ph-buildings".</summary>
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Href { get; set; } = "";

    /// <summary>24a: "Status die je anders moet gaan zoeken (Octopus-token, laatste crawl) staat op de
    /// kaart zelf" — optionele statusstip + label, enkel gezet waar er al een goedkope, bestaande
    /// databron voor is (vandaag: Marktdata-status se laatste crawl). Geen label = geen statusrij.</summary>
    public string? MetaLabel { get; set; }

    /// <summary>Toon voor de statusstip/-tekst: "is-success"/"is-warning"/"is-danger"/"is-info" (zelfde
    /// naamgeving als de Meldingskaders-typeklassen, punt 25 — geen aparte kleurtaal hier verzinnen).</summary>
    public string MetaTone { get; set; } = "is-info";

    public List<SettingsChipVm> Chips { get; set; } = new();
}

/// <summary>Een "tweede ingang" op een kaart (24a: Rollen/Groepen) — eigen link, eigen klikzone, moet de
/// kaart-brede hoofdklik (<see cref="SettingsItemVm.Href"/>) niet meenemen.</summary>
public class SettingsChipVm
{
    public string Label { get; set; } = "";
    public string Href { get; set; } = "";
}
