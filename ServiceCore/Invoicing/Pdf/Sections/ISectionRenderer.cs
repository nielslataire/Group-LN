using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public interface ISectionRenderer
{
    string SectionType { get; }
    void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx);
}