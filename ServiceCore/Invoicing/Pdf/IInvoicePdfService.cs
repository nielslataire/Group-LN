using BOCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ServiceCore.Invoicing.Pdf
{
    public interface IInvoicePdfService
    {
        byte[] Render(InvoiceDto invoice, IssuerCompanyBO company);

        /// <summary>Dezelfde gegevens als <see cref="Render"/> gebruikt (InvoiceVm + logo, QR, kleur), maar zonder pdf: voor een HTML-voorbeeld dat de echte factuur zo dicht mogelijk benadert.</summary>
        InvoicePreviewData BuildPreview(InvoiceDto invoice, IssuerCompanyBO company);
    }

    /// <summary>Wat een voorbeeld nodig heeft om de factuur na te bouwen: het factuurmodel en de context (logo, EPC-QR, huiskleur).</summary>
    public sealed class InvoicePreviewData
    {
        public InvoiceVm Vm { get; init; } = new();
        public TemplateContext Context { get; init; } = new();
        public string TemplateKey { get; init; } = "layoutA";
        public bool IsProforma { get; init; }
    }
}
