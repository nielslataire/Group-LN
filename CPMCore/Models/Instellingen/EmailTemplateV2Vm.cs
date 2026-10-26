using BOCore;
using System.Collections.Generic;

namespace CPMCore.Models.Instellingen;

/// <summary>gl-v2 E-mailtemplates (punt 55f): lijst en editor op één scherm. De editor gebruikt hetzelfde model als de klassieke pagina.</summary>
public class EmailTemplateV2Vm
{
    public List<EmailTemplateBO> Templates { get; set; } = new();
    public EmailTemplateEditVM? Edit { get; set; }
    /// <summary>Naam van de ingelogde gebruiker, voor de regel "Van" in het voorbeeld.</summary>
    public string Afzender { get; set; } = "";
}
