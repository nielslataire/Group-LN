using CPMCore.Models.GlV2;
using ServiceCore.Helpers;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/ChangeOrdersV2 (design-handoff 20b, "Offertes &
/// wijzigingen — één lijst, één verloop: offerte → opgemaakt → verzonden → ondertekend → gefactureerd
/// → betaald"). De hub-pagina: offertes en WO's zijn dezelfde ChangeOrder-rijen (zie
/// ChangeOrderDetailV2Vm/ChangeOrderStatusHelper), hier enkel gegroepeerd per fase.</summary>
public class ChangeOrdersV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }

    public List<ChangeOrderFunnelStepV2> Funnel { get; set; } = new();
    public List<ChangeOrderRowV2> Quotes { get; set; } = new();
    public List<ChangeOrderRowV2> Orders { get; set; } = new();

    public int QuoteCount => Quotes.Count;
    public int OrderCount => Orders.Count;
    public int AllCount => QuoteCount + OrderCount;

    public decimal SignedExtraTotal => Orders.Where(o => o.Status >= ChangeOrderStatus.Ondertekend && o.Amount > 0).Sum(o => o.Amount);
    public decimal SignedReductionTotal => Orders.Where(o => o.Status >= ChangeOrderStatus.Ondertekend && o.Amount < 0).Sum(o => o.Amount);
    public decimal SignedNetTotal => SignedExtraTotal + SignedReductionTotal;
}

public class ChangeOrderFunnelStepV2
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsHighlighted { get; set; }
}

public class ChangeOrderRowV2
{
    public int Id { get; set; }
    public bool IsQuote { get; set; }
    public string Number => IsQuote ? $"OF-{Id:000}" : $"WO-{Id:000}";
    public string ClientName { get; set; } = "";
    public string? UnitName { get; set; }
    public string Description { get; set; } = "";
    public string? SubText { get; set; }
    public bool SubTextIsWarning { get; set; }
    public string? SupplierName { get; set; }
    public string? SourceReference { get; set; } // "vanuit OF-008"
    public decimal Amount { get; set; }
    public ChangeOrderStatus Status { get; set; }
    public string StatusLabel => ChangeOrderStatusHelper.DisplayName(Status);
    /// <summary>De compacte "Verloop"-strip (design-handoff punt 27, DESIGN.md "Stappenplan") — gebouwd
    /// door <see cref="ChangeOrderStepsBuilder.BuildRow"/>, zelfde 6-stappen-positionering als voorheen
    /// (Factureerbaar telt visueel als "Ondertekend" — geen aparte stap daarvoor).</summary>
    public GlV2StepsVm Steps { get; set; } = new();
    public bool IsRejectedOrCancelled => Status == ChangeOrderStatus.Geweigerd || Status == ChangeOrderStatus.Geannuleerd;
    public bool CanRemind { get; set; }
    public int? SigningCaseId { get; set; }
    public DateOnly ExpirationDate { get; set; }
}
