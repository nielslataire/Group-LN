using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceCore;
using ServiceCore.Invoicing.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ServiceCore.Invoicing.Pdf
{
    public interface IInvoiceTemplate
    {
        void Compose(IDocumentContainer container, InvoiceVm vm, TemplateContext ctx);
    }
}
