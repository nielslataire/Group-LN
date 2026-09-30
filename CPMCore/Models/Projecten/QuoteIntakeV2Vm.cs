using BOCore;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/QuoteIntakeV2 (design-handoff 20c, "Offerte inlezen — kader
/// rond een tabel leest regels in, kader rond een foto hangt die aan een regel"). Werkt op dezelfde
/// ChangeOrder-rij als ChangeOrderDetailV2 (IsQuote=true zolang het nog een offerte is) — dit scherm is
/// enkel de intake-stap; opslaan/omzetten hergebruikt ChangeOrderDetailV2Save/-Convert.</summary>
public class QuoteIntakeV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int ChangeOrderId { get; set; }
    public bool IsNew { get; set; }
    public bool CanWrite { get; set; }

    public int ClientAccountId { get; set; }
    public string ClientName { get; set; } = "";
    public string? UnitName { get; set; }
    public int ContractActivityId { get; set; }
    public List<IdNameBO> ContractActivities { get; set; } = new();
    public string? QuoteSupplierReference { get; set; }
    public decimal? QuoteVatPercentage { get; set; }
    public DateOnly QuoteDate { get; set; }
    public DateOnly ExpirationDate { get; set; }

    public List<ChangeOrderDetailRowV2> Rows { get; set; } = new();
    public string? SourceFileName { get; set; }
    public bool AzureConfigured { get; set; }
}
