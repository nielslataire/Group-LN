using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;


namespace FacadeCore
{
    public interface IInvoicingService
    {
        GetResponse<InvoiceBO> GetInvoices();
        GetResponse<InvoiceBO> GetClientInvoices(int id, int itype = 1);

        //GetResponse<InvoiceBO> GetInvoiceByID(int id);
        GetResponse<InvoiceFileBO> GetInvoiceFileByFilename(string name);
    }
}
