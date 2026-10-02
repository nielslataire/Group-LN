using BOCore;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/QuoteIntakeV2 (design-handoff 20c, "Offerte inlezen — kader
/// rond een tabel leest regels in, kader rond een foto hangt die aan een regel"). Werkt op dezelfde
/// ChangeOrder-rij als ChangeOrderDetailV2 (IsQuote=true zolang het nog een offerte is) — dit scherm is
/// enkel de intake-stap (stap 1 in 29b); opslaan én omzetten (21c-modal) posten naar ChangeOrderDetailV2Save.</summary>
public class QuoteIntakeV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int ChangeOrderId { get; set; }
    public bool IsNew { get; set; }
    public bool CanWrite { get; set; }
    /// <summary>"OF-012" (offerte) of "WO-012" (een offerte koppelen aan een bestaande WO).</summary>
    public string Number { get; set; } = "";
    /// <summary>False als dit scherm op een al omgezette WO werkt (vanuit Bron op het opmaakscherm).</summary>
    public bool IsQuote { get; set; } = true;
    /// <summary>29a "Offerte zonder omzetten": bewaren is de hoofdactie, omzetten kan later.</summary>
    public bool KeepOnly { get; set; }

    public int ClientAccountId { get; set; }
    public string ClientName { get; set; } = "";
    public string? UnitName { get; set; }
    public int ContractActivityId { get; set; }
    public List<IdNameBO> ContractActivities { get; set; } = new();
    public List<IdNameBO> ClientAccounts { get; set; } = new();
    public string? QuoteSupplierReference { get; set; }
    public decimal? QuoteVatPercentage { get; set; }
    public DateOnly QuoteDate { get; set; }
    public DateOnly ExpirationDate { get; set; }

    public List<ChangeOrderDetailRowV2> Rows { get; set; } = new();
    public string? SourceFileName { get; set; }
    public bool AzureConfigured { get; set; }
}
