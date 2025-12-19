using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var raw = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
            return Task.CompletedTask;

        // laat spaties/duizendtallen toe
        raw = raw.Trim().Replace("\u00A0", " ");

        var styles = NumberStyles.Number;

        // 1) Probeer met huidige cultuur (nl-BE => komma)
        if (decimal.TryParse(raw, styles, CultureInfo.CurrentCulture, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        // 2) Probeer met invariant (punt)
        if (decimal.TryParse(raw, styles, CultureInfo.InvariantCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        // 3) Laatste poging: normaliseer punt/komma
        var swapped = raw.Contains(',') ? raw.Replace(',', '.') : raw.Replace('.', ',');
        if (decimal.TryParse(swapped, styles, CultureInfo.CurrentCulture, out parsed) ||
            decimal.TryParse(swapped, styles, CultureInfo.InvariantCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"The value '{raw}' is not valid.");
        return Task.CompletedTask;
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var t = context.Metadata.UnderlyingOrModelType;
        if (t == typeof(decimal))
            return new FlexibleDecimalModelBinder();

        return null;
    }
}
