namespace CPMCore.Models.Invoicing
{
    public class InvoiceLineVM
    {
        public bool IsSelected { get; set; }
        public string Text { get; set; } = "";
        public decimal Number { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Price { get; set; }
        public decimal VatPercentage { get; set; }
        public int? VatTypeId { get; set; }
        public string? VatCode { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public int? UnitId { get; set; }
        public int? PaymentStageId { get; set; }
        public string? LineType { get; set; }
        public string? GroupName { get; set; }
        public bool UtilityCost { get; set; }
        public bool UtilityIsAdvance { get; set; }
        public int? ChangeOrderId { get; set; }          
        public int? ChangeOrderDetailId { get; set; }


        public string? UnitName { get; set; }
        public decimal StagePercentage { get; set; }

        public string? UnitType { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectStreet { get; set; }
        public string? ProjectHouseNumber { get; set; }
        public string? ProjectCity { get; set; }
        public string? UnitStreet { get; set; }
        public string? UnitHouseNumber { get; set; }
        public decimal UnitConstructionTotal { get; set; }
        public decimal OwnerPercentage { get; set; } = 100m; // voorlopig 100%; co-owner kan later

    }
}
