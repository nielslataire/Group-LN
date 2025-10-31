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
    }
}
