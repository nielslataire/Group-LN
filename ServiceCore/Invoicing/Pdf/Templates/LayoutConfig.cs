using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceCore.Invoicing.Pdf.Templates;

public class LayoutConfig
{
    [JsonProperty("version")]
    public int Version { get; set; } = 1;

    [JsonProperty("page")]
    public PageConfig Page { get; set; } = new();

    [JsonProperty("theme")]
    public ThemeConfig Theme { get; set; } = new();

    [JsonProperty("sections")]
    public IList<SectionConfig> Sections { get; set; } = new List<SectionConfig>();
}

public class PageConfig
{
    [JsonProperty("margin")]
    public float Margin { get; set; } = 30f;

    [JsonProperty("bands")]
    public PageBandsConfig? Bands { get; set; }
        = new PageBandsConfig();
}

public class PageBandsConfig
{
    [JsonProperty("top")]
    public BandConfig? Top { get; set; }
        = new BandConfig();

    [JsonProperty("bottom")]
    public BandConfig? Bottom { get; set; }
        = new BandConfig();
}

public class BandConfig
{
    [JsonProperty("height")]
    public float Height { get; set; } = 0f;

    [JsonProperty("color")]
    public string? Color { get; set; }
        = "#FFFFFF";
}

public class ThemeConfig
{
    [JsonProperty("primary")]
    public string? Primary { get; set; }
        = "#000000";

    [JsonProperty("secondary")]
    public string? Secondary { get; set; }
        = "#666666";

    [JsonProperty("fontFamily")]
    public string? FontFamily { get; set; }
        = "Inter";

    [JsonProperty("logoSource")]
    public string? LogoSource { get; set; }
        = "";
}

[JsonConverter(typeof(SectionConfigConverter))]
public abstract class SectionConfig
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("visible")]
    public bool Visible { get; set; } = true;
}

public sealed class BandsSectionConfig : SectionConfig
{
    [JsonProperty("top")]
    public BandConfig? Top { get; set; }
        = new BandConfig();

    [JsonProperty("bottom")]
    public BandConfig? Bottom { get; set; }
        = new BandConfig();
}

public sealed class HeaderSectionConfig : SectionConfig
{
    [JsonProperty("left")]
    public IList<string> Left { get; set; } = new List<string>();

    [JsonProperty("right")]
    public HeaderRightConfig? Right { get; set; }
        = new HeaderRightConfig();
}

public sealed class HeaderRightConfig
{
    [JsonProperty("image")]
    public string? Image { get; set; }
        = string.Empty;

    [JsonProperty("width")]
    public float? Width { get; set; }
        = null;
}

public sealed class HeadlineSectionConfig : SectionConfig
{
    [JsonProperty("title")]
    public string? Title { get; set; }
        = string.Empty;

    [JsonProperty("meta")]
    public IList<string> Meta { get; set; } = new List<string>();
}

public sealed class PartiesSectionConfig : SectionConfig
{
    [JsonProperty("columns")]
    public IList<PartyColumnConfig> Columns { get; set; } = new List<PartyColumnConfig>();
}

public sealed class PartyColumnConfig
{
    [JsonProperty("title")]
    public string? Title { get; set; }
        = string.Empty;

    [JsonProperty("lines")]
    public IList<string> Lines { get; set; } = new List<string>();
}

public sealed class LinesTableSectionConfig : SectionConfig
{
    [JsonProperty("showVat")]
    public bool ShowVat { get; set; } = true;

    [JsonProperty("columns")]
    public IList<LinesTableColumnConfig> Columns { get; set; } = new List<LinesTableColumnConfig>();

    [JsonProperty("emptyText")]
    public string? EmptyText { get; set; }
        = null;
}

public sealed class LinesTableColumnConfig
{
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("label")]
    public string? Label { get; set; }
        = string.Empty;

    [JsonProperty("width")]
    public string? Width { get; set; }
        = "*";

    [JsonProperty("align")]
    public string? Align { get; set; }
        = "left";

    [JsonProperty("format")]
    public string? Format { get; set; }
        = null;

    [JsonProperty("visible")]
    public bool? Visible { get; set; } = null;
}

public sealed class TotalsSectionConfig : SectionConfig
{
    [JsonProperty("layout")]
    public string? Layout { get; set; }
        = "right";

    [JsonProperty("rows")]
    public IList<TotalRowConfig> Rows { get; set; } = new List<TotalRowConfig>();
}

public sealed class TotalRowConfig
{
    [JsonProperty("label")]
    public string? Label { get; set; }
        = string.Empty;

    [JsonProperty("value")]
    public string? Value { get; set; }
        = string.Empty;

    [JsonProperty("bold")]
    public bool? Bold { get; set; }
        = null;

    [JsonProperty("size")]
    public float? Size { get; set; }
        = null;
}

public sealed class PaymentSectionConfig : SectionConfig
{
    [JsonProperty("showStructuredMessage")]
    public bool ShowStructuredMessage { get; set; } = true;

    [JsonProperty("structuredLabel")]
    public string? StructuredLabel { get; set; }
        = string.Empty;

    [JsonProperty("structuredValue")]
    public string? StructuredValue { get; set; }
        = string.Empty;

    [JsonProperty("showQr")]
    public bool ShowQr { get; set; } = true;

    [JsonProperty("qrSize")]
    public float? QrSize { get; set; }
        = 120f;

    [JsonProperty("note")]
    public string? Note { get; set; }
        = string.Empty;
}

public sealed class LegalSectionConfig : SectionConfig
{
    [JsonProperty("text")]
    public string? Text { get; set; }
        = string.Empty;

    [JsonProperty("size")]
    public float? Size { get; set; }
        = 8f;
}

public sealed class FooterSectionConfig : SectionConfig
{
    [JsonProperty("center")]
    public string? Center { get; set; }
        = string.Empty;

    [JsonProperty("size")]
    public float? Size { get; set; }
        = 8f;
}

internal sealed class SectionConfigConverter : JsonConverter
{
    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType) => typeof(SectionConfig).IsAssignableFrom(objectType);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var type = jo.Value<string>("type") ?? string.Empty;
        SectionConfig target = type switch
        {
            "header" => new HeaderSectionConfig(),
            "headline" => new HeadlineSectionConfig(),
            "parties" => new PartiesSectionConfig(),
            "linesTable" => new LinesTableSectionConfig(),
            "totals" => new TotalsSectionConfig(),
            "payment" => new PaymentSectionConfig(),
            "legal" => new LegalSectionConfig(),
            "footer" => new FooterSectionConfig(),
            "bands" => new BandsSectionConfig(),
            _ => new UnknownSectionConfig(type)
        };

        serializer.Populate(jo.CreateReader(), target);
        return target;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => throw new NotSupportedException();
}

public sealed class UnknownSectionConfig : SectionConfig
{
    public UnknownSectionConfig(string type)
    {
        Type = type;
        Visible = false;
    }
}