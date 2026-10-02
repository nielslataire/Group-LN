using CPMCore.Models.GlV2;
using ServiceCore.Helpers;

namespace CPMCore.Models.Projecten;

/// <summary>
/// Bouwt het Stappenplan (design-handoff punt 27, zie DESIGN.md "Stappenplan") voor de twee plekken
/// die het verloop van "Offertes &amp; wijzigingen" (20b/20d) tonen: de volledige stepper bovenaan
/// ChangeOrderDetailV2 (<see cref="BuildDetail"/>) en de compacte "Verloop"-strip per rij op
/// ChangeOrdersV2 (<see cref="BuildRow"/>). Eén plek voor deze afleiding i.p.v. twee keer dezelfde
/// <see cref="ChangeOrderStatus"/>-naar-stap-vertaling — net als <see cref="ChangeOrderStatusHelper"/>
/// zelf al de ene bron is voor de status, zodat beide schermen nooit uit sync kunnen raken.
/// </summary>
public static class ChangeOrderStepsBuilder
{
    /// <summary>Volledig traject (7 stappen + een eventuele achtste "Geweigerd"/"Geannuleerd"), voor
    /// de <see cref="GlV2StepsVariant.Lijn"/>-stepper bovenaan ChangeOrderDetailV2.</summary>
    public static GlV2StepsVm BuildDetail(ChangeOrderStatus status, bool isQuote, string? quoteSupplierReference,
        DateOnly? dateSendToClient, DateOnly? dateAgreement, string id = "co-steps")
    {
        GlV2StepState StateFor(ChangeOrderStatus at) => status == at ? GlV2StepState.Current : status > at ? GlV2StepState.Done : GlV2StepState.Todo;

        var steps = new List<GlV2StepItemVm>
        {
            isQuote
                ? new GlV2StepItemVm { Label = "Offerte", State = GlV2StepState.Current, Sub = status == ChangeOrderStatus.Verlopen ? "verlopen" : null }
                : new GlV2StepItemVm { Label = "Offerte", State = GlV2StepState.Done, Sub = quoteSupplierReference },
            new() { Label = "Opgemaakt", State = isQuote ? GlV2StepState.Todo : StateFor(ChangeOrderStatus.Opgemaakt) },
            new() { Label = "Verzonden", State = isQuote ? GlV2StepState.Todo : StateFor(ChangeOrderStatus.Verzonden), Sub = dateSendToClient?.ToString("dd/MM/yyyy") },
            new() { Label = "Ondertekend", State = isQuote ? GlV2StepState.Todo : StateFor(ChangeOrderStatus.Ondertekend), Sub = dateAgreement?.ToString("dd/MM/yyyy") },
            new() { Label = "Factureerbaar", State = isQuote ? GlV2StepState.Todo : StateFor(ChangeOrderStatus.Factureerbaar) },
            new() { Label = "Gefactureerd", State = isQuote ? GlV2StepState.Todo : StateFor(ChangeOrderStatus.Gefactureerd) },
            new() { Label = "Betaald", State = isQuote ? GlV2StepState.Todo : StateFor(ChangeOrderStatus.Betaald) },
        };

        var isTerminal = status is ChangeOrderStatus.Geweigerd or ChangeOrderStatus.Geannuleerd;
        if (isTerminal)
        {
            // 27c se eigen voorbeeld (een geweigerde wijzigingsopdracht, Stappenplan.dc.html se
            // "woRefused"): de stappen die hierna nooit meer aan de beurt komen in DIT dossier worden
            // "Uit", niet stilzwijgend "Todo" blijven zoals de vorige, eigen stepper deed.
            foreach (var s in steps) if (s.State == GlV2StepState.Todo) s.State = GlV2StepState.Uit;
            steps.Add(new GlV2StepItemVm
            {
                Label = status == ChangeOrderStatus.Geweigerd ? "Geweigerd" : "Geannuleerd",
                State = status == ChangeOrderStatus.Geweigerd ? GlV2StepState.Error : GlV2StepState.Uit,
            });
        }

        var current = isTerminal ? steps.Count - 1 : steps.FindIndex(s => s.State == GlV2StepState.Current);
        return new GlV2StepsVm
        {
            Id = id,
            Variant = GlV2StepsVariant.Lijn,
            Current = current < 0 ? 0 : current,
            Clickable = false,
            Steps = steps,
        };
    }

    /// <summary>Verkort traject (6 posities: Offerte/Opgemaakt/Verzonden/Ondertekend/Gefactureerd/
    /// Betaald — "Ondertekend" dekt ook "Factureerbaar", "Verzonden" ook "Geweigerd"/"Geannuleerd"),
    /// voor de compacte <see cref="GlV2StepsVariant.Balk"/>-strip in de "Verloop"-kolom van ChangeOrdersV2.
    /// Zelfde positiegroepering als de vroegere <c>DotPositionFor</c> — zie <see cref="RowPosition"/>
    /// voor diezelfde positie als sorteersleutel.</summary>
    public static GlV2StepsVm BuildRow(ChangeOrderStatus status, int changeOrderId)
    {
        var position = RowPosition(status);
        var isRefused = status == ChangeOrderStatus.Geweigerd;
        var isCancelled = status == ChangeOrderStatus.Geannuleerd;

        string[] labels = { "Offerte", "Opgemaakt", "Verzonden", "Ondertekend", "Gefactureerd", "Betaald" };
        var steps = labels.Select((label, i) =>
        {
            GlV2StepState state;
            if (i < position) state = GlV2StepState.Done;
            else if (i == position) state = isRefused ? GlV2StepState.Error : isCancelled ? GlV2StepState.Uit : GlV2StepState.Current;
            else state = isRefused || isCancelled ? GlV2StepState.Uit : GlV2StepState.Todo;
            return new GlV2StepItemVm { Label = label, State = state };
        }).ToList();

        return new GlV2StepsVm
        {
            Id = $"co2-verloop-{changeOrderId}",
            Variant = GlV2StepsVariant.Balk,
            Size = GlV2StepsSize.Sm,
            Current = position,
            Clickable = false,
            ShowBalkHead = false,
            Steps = steps,
        };
    }

    /// <summary>Positie (0-5) van een status in het verkorte 6-stappentraject — ook de sorteersleutel
    /// van de Wijzigingsopdrachten-tabel (oplopend: wie het eerst aandacht nodig heeft, eerst).</summary>
    public static int RowPosition(ChangeOrderStatus status) => status switch
    {
        ChangeOrderStatus.Offerte or ChangeOrderStatus.Verlopen => 0,
        ChangeOrderStatus.Opgemaakt => 1,
        ChangeOrderStatus.Verzonden or ChangeOrderStatus.Geweigerd or ChangeOrderStatus.Geannuleerd => 2,
        ChangeOrderStatus.Ondertekend or ChangeOrderStatus.Factureerbaar => 3,
        ChangeOrderStatus.Gefactureerd => 4,
        ChangeOrderStatus.Betaald => 5,
        _ => 0,
    };
}
