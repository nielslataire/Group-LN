using System;
using System.Collections.Generic;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Elements.Table;
using QuestPDF.Elements;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;   // <-- nodig voor IContainer
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class LinesTableRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("nl-BE");
    private static readonly string[] ChangeOrderSeparators = new[] { " – ", " - " };

    public LinesTableRenderer(TemplateInterpolator interpolator) => _interpolator = interpolator;

    public string SectionType => "linesTable";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not LinesTableSectionConfig tableConfig || !section.Visible)
            return;

        var visibleColumns = GetVisibleColumns(tableConfig);
        if (visibleColumns.Count == 0)
            return;

        var hasHeader = !string.IsNullOrWhiteSpace(vm.HeaderDescription);
        var hasDetail = !string.IsNullOrWhiteSpace(vm.DetailDescription);
        var needsTableSpacing = hasHeader || hasDetail;

        column.Item().Column(col =>
        {
            if (hasHeader)
            {
                col.Item().Text(vm.HeaderDescription)
                    .FontSize(9);
            }

            if (hasDetail)
            {
                col.Item().Text(vm.DetailDescription)
                    .FontSize(9)
                    .Medium();
            }

            col.Item().Element(container =>
            {
                var tableContainer = needsTableSpacing
                    ? container.PaddingTop(15)
                    : container;

                RenderTable(tableContainer, visibleColumns, tableConfig, vm);
            });
        });
    }

    private void RenderTable(
        IContainer container,
        IReadOnlyList<LinesTableColumnConfig> visibleColumns,
        LinesTableSectionConfig tableConfig,
        InvoiceVm vm)
    {
        container.Table(table =>
        {
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

            table.Header(header =>
            {
                for (var columnIndex = 0; columnIndex < visibleColumns.Count; columnIndex++)
                {
                    var col = visibleColumns[columnIndex];
                    var label = col.Label ?? col.Key;
                    label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.ToUpper(_culture);

                    header.Cell().Element(c =>
                    {
                        var cell = ApplyAlignment(
                            ApplyColumnPadding(c.PaddingBottom(5), columnIndex, visibleColumns.Count),
                            col.Align);

                        cell.Text(label)
                            .FontSize(9)
                            .Bold();
                    });
                }
            });

            if (vm.Lines.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(tableConfig.EmptyText))
                {
                    var text = _interpolator.Interpolate(tableConfig.EmptyText, vm);
                    table.Cell()
                        .ColumnSpan((uint)visibleColumns.Count)
                        .PaddingTop(4)
                        .Text(text)
                        .FontSize(9);
                }
                return;
            }

            string? currentGroupKey = null;

            foreach (var line in vm.Lines)
            {
                var group = GetGroupInfo(line);

                if (group.Key != null && !string.IsNullOrWhiteSpace(group.Subtitle) && group.Key != currentGroupKey)
                {
                    AddGroupSubtitle(table, visibleColumns.Count, group.Subtitle!);
                    currentGroupKey = group.Key;
                }
                else if (group.Key == null)
                {
                    currentGroupKey = null;
                }

                var descriptionValue = group.DescriptionOverride ?? line.Description;

                for (var columnIndex = 0; columnIndex < visibleColumns.Count; columnIndex++)
                {
                    var col = visibleColumns[columnIndex];
                    object? value = ResolveLineValue(line, col.Key);

                    if (string.Equals(col.Key, nameof(InvoiceLineVm.Description), StringComparison.OrdinalIgnoreCase))
                        value = descriptionValue;

                    var formatted = FormatValue(col, value);

                    table.Cell().Element(c =>
                    {
                        var cell = ApplyAlignment(
                            ApplyColumnPadding(c, columnIndex, visibleColumns.Count),
                            col.Align);

                        cell.Text(formatted ?? string.Empty)
                            .FontSize(9);
                    });
                }
            }
        });
    }

    private static void AddGroupSubtitle(TableDescriptor table, int columnCount, string subtitle)
    {
        table.Cell()
            .ColumnSpan((uint)columnCount)
            .PaddingTop(8)
            .PaddingBottom(3)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Black)
            .Text(subtitle)
            .FontSize(9)
            .Medium();
    }

    private (string? Key, string? Subtitle, string? DescriptionOverride) GetGroupInfo(InvoiceLineVm line)
    {
        var type = line.LineType;
        if (string.IsNullOrWhiteSpace(type))
            return (null, null, null);

        if (string.Equals(type, "Stages", StringComparison.OrdinalIgnoreCase))
        {
            var subtitle = string.IsNullOrWhiteSpace(line.GroupName) ? null : line.GroupName.Trim();
            return subtitle != null
                ? ($"Stages:{subtitle}", subtitle, null)
                : (null, null, null);
        }

        if (string.Equals(type, "ChangeOrders", StringComparison.OrdinalIgnoreCase))
        {
            var (subtitle, detail) = SplitChangeOrderTitle(line.Description);
            var label = !string.IsNullOrWhiteSpace(subtitle) ? subtitle : line.GroupName;
            var key = !string.IsNullOrWhiteSpace(label) ? $"ChangeOrders:{label}" : null;
            var description = !string.IsNullOrWhiteSpace(detail) ? detail : null;
            return (key, label, description);
        }

        return (null, null, null);
    }

    private (string? Subtitle, string? Detail) SplitChangeOrderTitle(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return (null, null);

        var parts = description.Split(ChangeOrderSeparators, 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            var subtitle = string.IsNullOrWhiteSpace(parts[0]) ? null : parts[0];
            var detail = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];
            return (subtitle, detail);
        }

        return (description.Trim(), null);
    }

    // Align toepassen op de cell-container
    private static IContainer ApplyAlignment(IContainer container, string? align) =>
        align?.ToLowerInvariant() switch
        {
            "right" => container.AlignRight(),
            "center" => container.AlignCenter(),
            _ => container
        };

    private static IContainer ApplyColumnPadding(IContainer container, int columnIndex, int columnCount)
    {
        if (columnIndex > 0)
            container = container.PaddingLeft(5);

        if (columnIndex < columnCount - 1)
            container = container.PaddingRight(5);

        return container;
    }

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
