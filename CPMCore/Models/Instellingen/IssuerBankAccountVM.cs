using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen
{
    public class IssuerBankAccountVM
    {
        public int Id { get; set; }
        public int IssuerCompanyId { get; set; }

        [Required, Display(Name = "IBAN")]
        public string Iban { get; set; } = default!;

        [Display(Name = "BIC")]
        public string? Bic { get; set; }

        [Display(Name = "Omschrijving")]
        public string? DisplayName { get; set; }

        [Display(Name = "Standaard")]
        public bool IsDefault { get; set; }

        [Display(Name = "Geldig vanaf")]
        public DateOnly? ValidFrom { get; set; }

        [Display(Name = "Geldig tot")]
        public DateOnly? ValidTo { get; set; }
    }
}
