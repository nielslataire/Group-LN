using BOCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen;

public class VacatureListVM
{
    public List<VacatureBO> Vacatures { get; set; } = new();
}

public class VacatureSollicitatieListVM
{
    public List<VacatureSollicitatieBO> Sollicitaties { get; set; } = new();
    public int? VacatureId { get; set; }
    public string? VacatureTitel { get; set; }
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

    [StringLength(150)]
    public string? Opleiding { get; set; }

    [StringLength(100)]
    public string? Start { get; set; }

    [StringLength(500)]
    public string? KorteBeschrijving { get; set; }

    public string? Beschrijving { get; set; }

    /// <summary>URL van de reeds opgeslagen video (Storage API). Leeg = geen video.</summary>
    public string? VideoBestand { get; set; }

    /// <summary>URL van de posterafbeelding die als eerste beeld voor de video dient.</summary>
    public string? VideoPosterBestand { get; set; }

    /// <summary>Nieuw geüpload videobestand (optioneel).</summary>
    public IFormFile? VideoUpload { get; set; }

    /// <summary>Nieuw geüploade posterafbeelding (optioneel).</summary>
    public IFormFile? PosterUpload { get; set; }

    public bool IsGepubliceerd { get; set; }

    public int SortOrder { get; set; }

    public List<VacatureTaakBO> TaakItems { get; set; } = new();
    public List<VacatureVereisteBO> VereisteItems { get; set; } = new();
    public List<VacatureVoordeelBO> VoordeelItems { get; set; } = new();
    public List<VacatureSollicitatieStapBO> SollicitatieStapItems { get; set; } = new();
}

public class VacatureTaakVM
{
    public int ID { get; set; }
    public int VacatureId { get; set; }
    public int SortOrder { get; set; }
    [Required(ErrorMessage = "Tekst is verplicht.")]
    [StringLength(500)]
    public string Tekst { get; set; } = string.Empty;
}

public class VacatureVereisteVM
{
    public int ID { get; set; }
    public int VacatureId { get; set; }
    public int SortOrder { get; set; }
    public string Categorie { get; set; } = "MustHave";
    [Required(ErrorMessage = "Tekst is verplicht.")]
    [StringLength(500)]
    public string Tekst { get; set; } = string.Empty;
}

public class VacatureVoordeelVM
{
    public int ID { get; set; }
    public int VacatureId { get; set; }
    public int SortOrder { get; set; }
    [Required(ErrorMessage = "Tekst is verplicht.")]
    [StringLength(500)]
    public string Tekst { get; set; } = string.Empty;
}

public class VacatureSollicitatieStapVM
{
    public int ID { get; set; }
    public int VacatureId { get; set; }
    public int SortOrder { get; set; }
    [Required(ErrorMessage = "Titel is verplicht.")]
    [StringLength(200)]
    public string Titel { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Tekst { get; set; }
}

public class VacatureVolgordeVM
{
    public int VacatureId { get; set; }
    public List<int> SortedIds { get; set; } = new();
}
