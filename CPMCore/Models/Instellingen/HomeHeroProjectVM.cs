using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen
{
    public class HomeHeroProjectVM
    {
        [Required, Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Display(Name = "Kicker")]
        public string? Kicker { get; set; }

        [Display(Name = "Titel")]
        public string? Titel { get; set; }

        [Display(Name = "Tekst")]
        public string? Tekst { get; set; }

        [Display(Name = "Projecttitel")]
        public string? ProjectTitelOverride { get; set; }

        public List<SelectListItem> ProjectOpties { get; set; } = new();
    }
}
