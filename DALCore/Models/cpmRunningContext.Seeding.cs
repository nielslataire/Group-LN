using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALCore.Models
{
    public partial class cpmRunningContext
    {
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceStatusLookup>().HasData(
                new InvoiceStatusLookup { Id = 1, Name = "Draft" },
                new InvoiceStatusLookup { Id = 2, Name = "Issued" },
                new InvoiceStatusLookup { Id = 3, Name = "Sent" },
                new InvoiceStatusLookup { Id = 4, Name = "PartiallyPaid" },
                new InvoiceStatusLookup { Id = 5, Name = "Paid" },
                new InvoiceStatusLookup { Id = 6, Name = "Overdue" },
                new InvoiceStatusLookup { Id = 7, Name = "Cancelled" }
            );

            modelBuilder.Entity<PaymentTerms>().HasData(
                new PaymentTerms { Id = 1, Name = "14 dagen", Days = 14, Description = "Standaard betaaltermijn 14 dagen" },
                new PaymentTerms { Id = 2, Name = "30 dagen", Days = 14, Description = "Betaaltermijn 30 dagen" },
                new PaymentTerms { Id = 3, Name = "30 dagen einde maand", Days = 30, Description = "30 dagen na einde maand factuurdatum" },
                new PaymentTerms { Id = 4, Name = "Contant", Days = 0, Description = "Onmiddellijk bij factuurdatum" }
);
        }
    }
}
