using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen
{
    public class InvoiceSeriesVM
    {
        public int Id { get; set; }
        public int? IssuerCompanyId { get; set; }

        [Display(Name = "Code")]
        [Required] public string Code { get; set; } = default!;
        [Display(Name = "Omschrijving")]
        public string? Description { get; set; }
        [Display(Name = "Creditnotareeks")]
        public bool IsCreditNote { get; set; }
        [Display(Name = "Actief")]
        public bool IsActive { get; set; } = true;
    }
}
