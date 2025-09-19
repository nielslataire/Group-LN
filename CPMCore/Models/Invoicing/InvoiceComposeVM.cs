using BOCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using UblSharp.CommonAggregateComponents;

namespace CPMCore.Models.Invoicing
{
    public enum StartStatus { Draft = 0, Invoice = 1 } // Invoice = meteen uitgeven
    public class InvoiceComposeVM
    {
        public int IssuerCompanyId { get; set; }
        public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public int? PartyId { get; set; }
        public InvoicePartyType? PartyType { get; set; }

        public StartStatus StartAs { get; set; } = StartStatus.Invoice;

        public List<InvoiceLineVM> Lines { get; set; } = new()
        {
            new InvoiceLineVM() // 1 lege lijn standaard
        };

        // UI lijsten
        public IEnumerable<SelectListItem> Issuers { get; set; } = Array.Empty<SelectListItem>();
    }
}
