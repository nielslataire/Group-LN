using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Gebruikersnaam")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Naam")]
        public string? Name { get; set; }

        [Display(Name = "Voornaam")]
        public string? Forename { get; set; }

        [Display(Name = "Functie")]
        public string? JobFunction { get; set; }

        [Display(Name = "GSM")]
        public string? Cellphone { get; set; }

        public List<string> AvailableRoles { get; set; } = new();

        [Display(Name = "Rollen")]
        public List<string> SelectedRoles { get; set; } = new();
    }
}