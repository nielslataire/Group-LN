using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(value))
            return Task.CompletedTask;

        if (decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result))
        {
            context.Result = ModelBindingResult.Success(result);
        }
        else
        {
            context.ModelState.TryAddModelError(
                context.ModelName,
                $"'{value}' is geen geldig bedrag");
        }

        return Task.CompletedTask;
    }
}