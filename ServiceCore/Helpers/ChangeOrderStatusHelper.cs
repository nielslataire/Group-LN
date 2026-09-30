using System;
using BOCore;

namespace ServiceCore.Helpers
{
    /// <summary>Statuspijplijn voor "Offertes & wijzigingen" gl-v2 (design-handoff 20b/20c/20d,
    /// Flow Facturatie en wijzigingen.md §3). Een offerte en een WO zijn dezelfde ChangeOrder-rij
    /// (ChangeOrder.IsQuote) — geen aparte tabel, dus ook geen aparte status-enum voor de offertefase.
    /// De status wordt hier ALTIJD afgeleid uit bestaande bronvelden (ChangeOrder, SigningCase via
    /// ISigningService, InvoicesDetails/Invoices) — nooit zelf opgeslagen, zelfde aanpak als
    /// ProjectenController.PaymentStagesV2.cs se IsReached. Gebruikt door zowel 20b (funnelbalk +
    /// rijbadge) als 20d (bovenste stepper), zodat de twee schermen nooit uit sync kunnen raken.</summary>
    public enum ChangeOrderStatus
    {
        Offerte = 0,
        Verlopen = 1,
        Opgemaakt = 2,
        Verzonden = 3,
        Geweigerd = 4,
        Geannuleerd = 5,
        Ondertekend = 6,
        Factureerbaar = 7,
        Gefactureerd = 8,
        Betaald = 9,
    }

    /// <summary>Alle brongegevens die de status bepalen, per ChangeOrder al vooraf opgehaald door de
    /// aanroeper (één signing-lookup + één InvoicesDetails-query per WO, niet N+1 vanuit deze helper).</summary>
    public readonly record struct ChangeOrderStatusInput(
        bool IsQuote,
        DateOnly ExpirationDate,
        DateOnly? DateSendToClient,
        DateOnly? DateAgreement,
        /// <summary>Status van het recentste ondertekendossier (<see cref="SigningCaseStatus"/>), of
        /// null als er nooit een dossier gestart is.</summary>
        int? SigningCaseStatus,
        /// <summary>Minstens één facturatieplan-termijn is nu factureerbaar (trigger bereikt) en nog
        /// niet gefactureerd.</summary>
        bool HasInvoicableTerm,
        /// <summary>Er bestaat minstens één termijn EN elke termijn heeft een InvoicesDetails-rij.</summary>
        bool AllTermsInvoiced,
        /// <summary>Enkel relevant als AllTermsInvoiced: elke bijhorende factuur staat op InvoiceStatusId.Paid.</summary>
        bool AllInvoicesPaid);

    public static class ChangeOrderStatusHelper
    {
        public static ChangeOrderStatus Compute(ChangeOrderStatusInput input, DateOnly today)
        {
            if (input.IsQuote)
                return input.ExpirationDate < today ? ChangeOrderStatus.Verlopen : ChangeOrderStatus.Offerte;

            if (input.SigningCaseStatus == (int)SigningCaseStatus.Declined) return ChangeOrderStatus.Geweigerd;
            if (input.SigningCaseStatus == (int)SigningCaseStatus.Cancelled) return ChangeOrderStatus.Geannuleerd;

            if (!input.DateAgreement.HasValue)
            {
                var isSent = input.DateSendToClient.HasValue || input.SigningCaseStatus == (int)SigningCaseStatus.Open;
                return isSent ? ChangeOrderStatus.Verzonden : ChangeOrderStatus.Opgemaakt;
            }

            if (input.AllTermsInvoiced) return input.AllInvoicesPaid ? ChangeOrderStatus.Betaald : ChangeOrderStatus.Gefactureerd;
            if (input.HasInvoicableTerm) return ChangeOrderStatus.Factureerbaar;
            return ChangeOrderStatus.Ondertekend;
        }

        public static string DisplayName(ChangeOrderStatus status) => status switch
        {
            ChangeOrderStatus.Offerte => "Offerte",
            ChangeOrderStatus.Verlopen => "Verlopen",
            ChangeOrderStatus.Opgemaakt => "Opgemaakt",
            ChangeOrderStatus.Verzonden => "Verzonden",
            ChangeOrderStatus.Geweigerd => "Geweigerd",
            ChangeOrderStatus.Geannuleerd => "Geannuleerd",
            ChangeOrderStatus.Ondertekend => "Ondertekend",
            ChangeOrderStatus.Factureerbaar => "Factureerbaar",
            ChangeOrderStatus.Gefactureerd => "Gefactureerd",
            ChangeOrderStatus.Betaald => "Betaald",
            _ => status.ToString(),
        };
    }
}
