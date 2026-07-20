using BOCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen;

public class EmailTemplateListVM
{
    public List<EmailTemplateBO> Templates { get; set; } = new();
}

public class EmailTemplateEditVM
{
    public int ID { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [StringLength(200)]
    public string Naam { get; set; } = string.Empty;

    [Required(ErrorMessage = "Onderwerp is verplicht.")]
    [StringLength(300)]
    public string Onderwerp { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public bool IsActief { get; set; } = true;
}
