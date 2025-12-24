using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var raw = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
            return Task.CompletedTask;

        // spaties & NBSP normaliseren
        raw = raw.Trim()
                 .Replace("\u00A0", "")
                 .Replace(" ", "");

        var styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        decimal parsed;

        // 1️⃣ huidige cultuur
        if (decimal.TryParse(raw, styles, CultureInfo.CurrentCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(Normalize(parsed));
            return Task.CompletedTask;
        }

        // 2️⃣ invariant cultuur
        if (decimal.TryParse(raw, styles, CultureInfo.InvariantCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(Normalize(parsed));
            return Task.CompletedTask;
        }

        // 3️⃣ normaliseer separators
        var normalized = raw
            .Replace(".", "")
            .Replace(",", ".");

        if (decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(Normalize(parsed));
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"The value '{raw}' is not valid.");

        return Task.CompletedTask;
    }

    // 🔑 DIT IS DE OPLOSSING
    private static decimal Normalize(decimal value)
    {
        // forceer geldschaal
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}


public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var t = context.Metadata.UnderlyingOrModelType;

        if (t == typeof(decimal))
            return new FlexibleDecimalModelBinder();

        return null;
    }
}

