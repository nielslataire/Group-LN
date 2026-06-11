using BOCore;
using Microsoft.AspNetCore.Http;
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

    public List<BlogArtikelBlokVM> Blokken { get; set; } = new();
}

public class BlogArtikelBlokVM
{
    public int ID { get; set; }
    public int ArtikelId { get; set; }
    public int SortOrder { get; set; }
    public string? Titel { get; set; }
    public string? RijkeTekst { get; set; }
    public string? FotoBestand { get; set; }
    public IFormFile? FotoUpload { get; set; }
}
