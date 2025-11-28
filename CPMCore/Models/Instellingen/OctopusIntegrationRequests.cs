using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Models.Instellingen
{
    public class OctopusAuthRequest
    {
        [FromForm(Name = "OctopusUsername")]
        public string? Username { get; set; }

        [FromForm(Name = "OctopusPassword")]
        public string? Password { get; set; }
    }

    public class OctopusDossierRequest
    {
        public string? DossierNumber { get; set; }
    }
}