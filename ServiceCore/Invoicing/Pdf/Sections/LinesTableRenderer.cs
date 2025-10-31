using System;
using System.Collections.Generic;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Elements.Table;
using QuestPDF.Elements;
using QuestPDF.Infrastructure;   // <-- nodig voor IContainer
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class LinesTableRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("nl-BE");

    public LinesTableRenderer(TemplateInterpolator interpolator) => _interpolator = interpolator;

    public string SectionType => "linesTable";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not LinesTableSectionConfig tableConfig || !section.Visible)
            return;

        var visibleColumns = GetVisibleColumns(tableConfig);
        if (visibleColumns.Count == 0)
            return;

        column.Item().Table(table =>
        {
            // kolombreedtes
            table.ColumnsDefinition(cols =>
            {
                foreach (var col in visibleColumns)
                {
                    if (string.IsNullOrWhiteSpace(col.Width) || col.Width == "*")
                        cols.RelativeColumn();
                    else if (float.TryParse(col.Width, NumberStyles.Any, CultureInfo.InvariantCulture, out var w))
                        cols.ConstantColumn(w);
                    else
                        cols.RelativeColumn();
                }
            });

            // header
            table.Header(header =>
            {
                foreach (var col in visibleColumns)
                {
                    header.Cell()
                          .Element(c => ApplyAlignment(c, col.Align))
                          .Text(col.Label ?? col.Key)
                          .SemiBold();
                }
            });

            // geen lijnen
            if (vm.Lines.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(tableConfig.EmptyText))
                {
                    var text = _interpolator.Interpolate(tableConfig.EmptyText, vm);
                    table.Cell()
                         .ColumnSpan((uint)visibleColumns.Count)   // <-- uint
                         .Text(text);
                }
                return;
            }

            // datarijen
            foreach (var line in vm.Lines)
            {
                foreach (var col in visibleColumns)
                {
                    var value = ResolveLineValue(line, col.Key);
                    var formatted = FormatValue(col, value);

                    table.Cell()
                         .Element(c => ApplyAlignment(c, col.Align))
                         .Text(formatted);
                }
            }
        });
    }

    // Align toepassen op de cell-container
    private static IContainer ApplyAlignment(IContainer container, string? align) =>
        align?.ToLowerInvariant() switch
        {
            "right" => container.AlignRight(),
            "center" => container.AlignCenter(),
            _ => container
        };

    private static object? ResolveLineValue(InvoiceLineVm line, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var prop = typeof(InvoiceLineVm).GetProperty(
            key,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (prop != null)
            return prop.GetValue(line);

        return line.Extras.TryGetValue(key, out var value) ? value : null;
    }

    private string FormatValue(LinesTableColumnConfig column, object? value)
    {
        if (value == null) return string.Empty;

        return column.Format switch
        {
            "eur" => ToDecimal(value).ToString("C", _culture),
            "pct" => $"{ToDecimal(value):0.##}%",
            _ => Convert.ToString(value, _culture) ?? string.Empty
        };
    }

    private static decimal ToDecimal(object value) =>
        value switch
        {
            decimal d => d,
            double d => (decimal)d,
            float f => (decimal)f,
            int i => i,
            long l => l,
            _ => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                                  NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                 ? parsed : 0m
        };

    private static List<LinesTableColumnConfig> GetVisibleColumns(LinesTableSectionConfig config)
    {
        var result = new List<LinesTableColumnConfig>();
        foreach (var column in config.Columns)
        {
            if (column.Visible == false) continue;
            if (!config.ShowVat && string.Equals(column.Key, "Vat", StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(column);
        }
        return result;
    }
}
