using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen;

public class InvoiceLayoutTemplateVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sleutel is verplicht.")]
    [Display(Name = "Sleutel")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "Beschrijving")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Template JSON is verplicht.")]
    [Display(Name = "Template JSON")]
    public string TemplateJson { get; set; } = string.Empty;

    public bool IsSystem { get; set; }
}

public class InvoiceLayoutTemplateOptionVM
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TemplateJson { get; set; } = string.Empty;

    public bool IsSystem { get; set; }
}