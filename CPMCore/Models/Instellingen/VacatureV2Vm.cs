using BOCore;
using System.Collections.Generic;

namespace CPMCore.Models.Instellingen;

/// <summary>gl-v2 Vacatures (punt 55c): de lijst met per vacature het aantal sollicitaties en hoeveel daarvan nog ongelezen zijn.</summary>
public class VacatureListV2Vm
{
    public List<VacatureBO> Vacatures { get; set; } = new();
    public Dictionary<int, (int Totaal, int Nieuw)> Sollicitaties { get; set; } = new();
    public int TotaalNieuw { get; set; }
}

/// <summary>gl-v2 Vacature bewerken (punt 55d): het bestaande bewerk-model plus de voorvertoning en het aantal sollicitaties.</summary>
public class VacatureEditV2Vm
{
    public VacatureEditVM Edit { get; set; } = new();
    public string? PreviewUrl { get; set; }
    public int AantalSollicitaties { get; set; }
}

/// <summary>Eén sleepbare puntenlijst in de vacature-editor (taken, must-haves, mooi meegenomen, voordelen of stappen).</summary>
public class VacatureLijstV2Vm
{
    public string Kind { get; set; } = "";            // taak | must | mooi | voordeel | stap
    public string Titel { get; set; } = "";
    public string? Omschrijving { get; set; }
    public string AddText { get; set; } = "Toevoegen";
    public string? Categorie { get; set; }            // MustHave | MooiMeegenomen (alleen bij vereisten)
    public string SaveAction { get; set; } = "";
    public string DeleteAction { get; set; } = "";
    public string DeleteField { get; set; } = "";     // taakId | vereisteId | voordeelId | stapId
    public string OrderAction { get; set; } = "";
    public bool Numbered { get; set; }
    public bool CanWrite { get; set; }
    public List<VacatureLijstItemV2> Items { get; set; } = new();
}

public class VacatureLijstItemV2
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string? Titel { get; set; }
    public string? Tekst { get; set; }
}
