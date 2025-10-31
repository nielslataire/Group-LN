using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ServiceCore.Invoicing.Pdf;

public class TemplateInterpolator
{
    private static readonly Regex TokenRegex =
        new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("nl-BE");

    public string Interpolate(string? template, InvoiceVm vm)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        return TokenRegex.Replace(template, match =>
        {
            var expr = match.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(expr))
                return string.Empty;

            string? filter = null;
            var pipeIndex = expr.IndexOf('|');
            if (pipeIndex >= 0)
            {
                filter = expr[(pipeIndex + 1)..].Trim();
                expr = expr[..pipeIndex].Trim();
            }

            string? format = null;
            var colonIndex = expr.IndexOf(':');
            if (colonIndex >= 0)
            {
                format = expr[(colonIndex + 1)..].Trim();
                expr = expr[..colonIndex].Trim();
            }

            var value = ResolvePath(vm, expr);
            if (value == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(format))
            {
                value = ApplyFormat(value, format);
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                value = ApplyFilter(value, filter);
            }

            return value?.ToString() ?? string.Empty;
        });
    }

    private object? ApplyFormat(object value, string format)
    {
        if (value is DateOnly dateOnly)
            return dateOnly.ToString(format, _culture);
        if (value is DateTime dt)
            return dt.ToString(format, _culture);
        if (value is IFormattable fmt)
            return fmt.ToString(format, _culture);
        return value;
    }

    private object? ApplyFilter(object value, string filter)
    {
        switch (filter)
        {
            case "eur":
                if (value is decimal dec)
                    return dec.ToString("C", _culture);
                if (value is double dbl)
                    return dbl.ToString("C", _culture);
                if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    return parsed.ToString("C", _culture);
                break;
            case "pct":
                if (value is decimal pctDec)
                    return $"{pctDec:0.##}%";
                if (value is double pctDbl)
                    return $"{pctDbl:0.##}%";
                if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var pctParsed))
                    return $"{pctParsed:0.##}%";
                break;
        }

        return value;
    }

    private static object? ResolvePath(object root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
            return null;

        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current == null)
                return null;

            var type = current.GetType();
            var property = type.GetProperty(segment, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }
}