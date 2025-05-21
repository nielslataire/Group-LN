using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Models
{
    public class RegisterViewModel

    {
        [BindProperty]
        public InputModel Input { get; set; }
        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }
        public class InputModel
        {
            [Required]
            [Display(Name = "Gebruikersnaam")]
            public required string Username { get; set; }

            [Required]
            [Display(Name = "Naam")]
            public required string Name { get; set; }

            [Required]
            [Display(Name = "Voornaam")]
            public required string Forename { get; set; }

            [Required]
            [Display(Name = "Functie")]
            public required string JobFunction { get; set; }

            [Required]
            [Phone]
            [Display(Name = "GSM")]
            public required string Cellphone { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public required string Email { get; set; }


            [StringLength(100, ErrorMessage = "Uw {0} moet minstens {2} karakters lang zijn.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Paswoord")]
            public required string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Bevestig paswoord")]
            [Compare("Password", ErrorMessage = "De paswoorden komen niet overeen.")]
            public required string ConfirmPassword { get; set; }
        }

    }
    public class UpdateViewModel

    {
        [BindProperty]
        public ApplicationUser User { get; set; }
        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    }
    public class LoginViewModel
    {
        [BindProperty]
        public InputModel Input { get; set; }
        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }    

        public class InputModel
        {
            [Required]
            [Display(Name = "Gebruikersnaam")]
            public required string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Paswoord")]
            public required string Password { get; set; }

            [Display(Name = "Onthoud mij?")]
            public bool RememberMe { get; set; }
        }
    }
}
