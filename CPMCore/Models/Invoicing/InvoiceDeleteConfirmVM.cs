using BOCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using UblSharp.CommonAggregateComponents;

namespace CPMCore.Models.Invoicing
{
    // VM-items voor dropdown rendering
    public class InvoiceDeleteConfirmVM
    {
        public int Id { get; set; }
        public int IssuerCompanyId { get; set; }
        public string DisplayId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unknown;
        public string StatusLabel => Status.GetDisplayName();
        /// <summary>Het concept komt uit een terugkerend schema: bij verwijderen kies je of de factuurdatum terug in het schema komt.</summary>
        public int? RecurringScheduleId { get; set; }
        public bool CanRestoreToSchedule { get; set; }
        public string? RecurringPeriod { get; set; }
    }
}
