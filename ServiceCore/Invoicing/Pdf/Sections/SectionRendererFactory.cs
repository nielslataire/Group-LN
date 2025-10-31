using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore.Invoicing.Pdf.Sections;

public class SectionRendererFactory
{
    private readonly IDictionary<string, ISectionRenderer> _renderers;

    public SectionRendererFactory(IEnumerable<ISectionRenderer> renderers)
    {
        _renderers = renderers.ToDictionary(r => r.SectionType, StringComparer.OrdinalIgnoreCase);
    }

    public ISectionRenderer? Resolve(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        return _renderers.TryGetValue(type, out var renderer)
            ? renderer
            : null;
    }
}