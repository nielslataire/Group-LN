using BOCore;
using System.Collections.Generic;

namespace CPMCore.Models.Instellingen;

/// <summary>gl-v2 Blogartikelen (punt 55b): het bestaande bewerk-model plus de voorvertoning.</summary>
public class BlogEditV2Vm
{
    public BlogArtikelEditVM Edit { get; set; } = new();
    public string? PreviewUrl { get; set; }
}
