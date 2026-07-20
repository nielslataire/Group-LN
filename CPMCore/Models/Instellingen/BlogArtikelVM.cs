using BOCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen;

public class BlogArtikelListVM
{
    public List<BlogArtikelBO> Artikelen { get; set; } = new();
}

public class BlogArtikelEditVM
{
    public int ID { get; set; }

    [Required(ErrorMessage = "Titel is verplicht.")]
    [StringLength(250)]
    public string Titel { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [StringLength(500)]
    public string? PreviewTekst { get; set; }

    [StringLength(250)]
    public string? DetailTitel { get; set; }

    [StringLength(1000)]
    public string? DetailTitelTekst { get; set; }

    public string? FotoBestand { get; set; }
    public IFormFile? FotoUpload { get; set; }

    [Required(ErrorMessage = "Datum is verplicht.")]
    public DateTime Datum { get; set; } = DateTime.Today;

    public bool IsGepubliceerd { get; set; }

    public int SortOrder { get; set; }

    [StringLength(250)]
    public string? MetaTitel { get; set; }

    [StringLength(500)]
    public string? MetaOmschrijving { get; set; }

    [StringLength(500)]
    public string? MetaKeywords { get; set; }

    [StringLength(50)]
    public string? GeoRegio { get; set; }

    [StringLength(100)]
    public string? GeoPlaatsnaam { get; set; }

    [StringLength(50)]
    public string? GeoPositie { get; set; }

    public string? Link1Type { get; set; }
    public int? Link1Id { get; set; }
    public string? Link2Type { get; set; }
    public int? Link2Id { get; set; }
    public string? Link3Type { get; set; }
    public int? Link3Id { get; set; }

    public List<BlogArtikelBlokVM> Blokken { get; set; } = new();
    public List<BlogArtikelFaqVM> FaqItems { get; set; } = new();

    public List<SelectListItem> ArtikelOpties { get; set; } = new();
    public List<SelectListItem> ProjectOpties { get; set; } = new();
}

public class BlogArtikelBlokVM
{
    public int ID { get; set; }
    public int ArtikelId { get; set; }
    public int SortOrder { get; set; }
    public string BlokType { get; set; } = "tekst";
    public string? Titel { get; set; }
    public string? RijkeTekst { get; set; }
    public string? FotoBestand { get; set; }
    public IFormFile? FotoUpload { get; set; }
}

public class BlokVolgordeVM
{
    public int ArtikelId { get; set; }
    public List<int> SortedIds { get; set; } = new();
}

public class BlogArtikelFaqVM
{
    public int ID { get; set; }
    public int ArtikelId { get; set; }
    public int SortOrder { get; set; }
    [Required(ErrorMessage = "Vraag is verplicht.")]
    [StringLength(500)]
    public string Vraag { get; set; } = string.Empty;
    public string? Antwoord { get; set; }
}

public class FaqVolgordeVM
{
    public int ArtikelId { get; set; }
    public List<int> SortedIds { get; set; } = new();
}
