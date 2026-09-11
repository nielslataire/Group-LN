using System.Text.Json;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Leest optionele velden uit de kleine JSON-payload (<c>MijlpaalTrigger.ActieParametersJson</c>).</summary>
internal static class TriggerParamHelper
{
    internal static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonDocument.Parse(json).RootElement; }
        catch (JsonException) { return null; }
    }

    internal static string? GetString(JsonElement? root, string name) =>
        root is JsonElement el && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    internal static int? GetInt(JsonElement? root, string name) =>
        root is JsonElement el && el.TryGetProperty(name, out var v) && v.TryGetInt32(out var i)
            ? i : null;

    internal static bool? GetBool(JsonElement? root, string name) =>
        root is JsonElement el && el.TryGetProperty(name, out var v)
            && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False)
            ? v.GetBoolean() : null;
}
