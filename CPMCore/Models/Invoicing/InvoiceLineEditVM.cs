using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Invoicing
{
    public class InvoiceLineEditVM
    {
        public int LineId { get; set; }

        [Display(Name = "Omschrijving")]
        [DataType(DataType.MultilineText)]
        public string? Text { get; set; }

        public string? GroupName { get; set; }
        public decimal VatRate { get; set; }
        public decimal NetAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
    }
}
