using BOCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen;

public class VacatureListVM
{
    public List<VacatureBO> Vacatures { get; set; } = new();
}

public class VacatureEditVM
{
    public int ID { get; set; }

    [Required(ErrorMessage = "Titel is verplicht.")]
    [StringLength(250)]
    public string Titel { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [StringLength(100)]
    public string? Categorie { get; set; }

    [StringLength(100)]
    public string? Locatie { get; set; }

    [StringLength(100)]
    public string? Dienstverband { get; set; }

    [StringLength(500)]
    public string? KorteBeschrijving { get; set; }

    public string? Beschrijving { get; set; }

    public bool IsGepubliceerd { get; set; }

    public int SortOrder { get; set; }
}
