#nullable disable
using System.Collections.Generic;

namespace DALCore.Models;

public partial class IncommingInvoices
{
    public virtual IncomingInvoiceEnrichment Enrichment { get; set; }

    public virtual ICollection<IncomingInvoiceWarning> Warnings { get; set; } = new List<IncomingInvoiceWarning>();
}
