using System.Globalization;
using System.Reflection;

public static class DecimalNormalizationHelper
{
    private static readonly CultureInfo BeCulture = CultureInfo.GetCultureInfo("nl-BE");

    public static void NormalizeDecimals(object model, IFormCollection form)
    {
        if (model == null) return;

        foreach (var prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            {
                var key = prop.Name;

                if (!form.TryGetValue(key, out var rawValue))
                    continue;

                var raw = rawValue.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var normalized = raw.Replace('.', ',');

                if (decimal.TryParse(normalized, NumberStyles.Any, BeCulture, out var value))
                {
                    prop.SetValue(model, value);
                }
            }
            else if (!prop.PropertyType.IsPrimitive && !prop.PropertyType.IsEnum)
            {
                // 🔁 Recursief voor nested objects & collections
                var child = prop.GetValue(model);
                if (child is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                        NormalizeDecimals(item, form);
                }
                else
                {
                    NormalizeDecimals(child, form);
                }
            }
        }
    }
}
