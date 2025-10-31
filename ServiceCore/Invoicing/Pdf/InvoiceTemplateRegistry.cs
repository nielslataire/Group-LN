using ServiceCore.Invoicing.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore.Invoicing.Pdf;

public sealed class InvoiceTemplateRegistry : IInvoiceTemplateRegistry
{
    private readonly IDictionary<string, IInvoiceTemplate> _templates;
    private readonly IInvoiceTemplate _defaultTemplate;

    public InvoiceTemplateRegistry(IEnumerable<IInvoiceTemplate> templates)
    {
        _templates = new Dictionary<string, IInvoiceTemplate>(StringComparer.OrdinalIgnoreCase);
        var templateList = templates.ToList();
        foreach (var template in templateList)
        {
            if (template is NamedInvoiceTemplate named)
                _templates[named.Name] = template;
        }

        _defaultTemplate = templateList.FirstOrDefault()
            ?? throw new InvalidOperationException("No invoice templates registered.");
    }

    public IInvoiceTemplate Resolve(string templateKey)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
            return _defaultTemplate;

        return _templates.TryGetValue(templateKey, out var template)
            ? template
            : _defaultTemplate;
    }
}

public abstract class NamedInvoiceTemplate : IInvoiceTemplate
{
    protected NamedInvoiceTemplate(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract void Compose(IDocumentContainer container, InvoiceVm vm, TemplateContext ctx);
}
