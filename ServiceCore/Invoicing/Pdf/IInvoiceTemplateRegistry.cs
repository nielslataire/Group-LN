using ServiceCore.Invoicing.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ServiceCore.Invoicing.Pdf;

public interface IInvoiceTemplateRegistry
{
    IInvoiceTemplate Resolve(string templateKey);
}