namespace CPMCore.Models.Invoicing
{
    public class InvoiceLineVM
    {

            public string Text { get; set; } = "";
            public decimal Price { get; set; }
            public decimal VatPercentage { get; set; }
            public decimal? DiscountPercent { get; set; }
            public decimal? DiscountAmount { get; set; }
            public int? UnitId { get; set; }
            public int? PaymentStageId { get; set; }
            public string? LineType { get; set; }
            public string? GroupName { get; set; }
            public bool UtilityCost { get; set; }
    
    }
}
