#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

// Partial uitbreiding van de auto-generated cpmRunningContext
// voor verrijkings- en leerlagentabellen.
public partial class cpmRunningContext
{
    public virtual DbSet<IncomingInvoiceEnrichment> IncomingInvoiceEnrichment { get; set; }

    public virtual DbSet<IncomingInvoiceWarning> IncomingInvoiceWarning { get; set; }

    public virtual DbSet<InvoiceSupplierRule> InvoiceSupplierRule { get; set; }

    public virtual DbSet<InvoiceProjectAlias> InvoiceProjectAlias { get; set; }
}
