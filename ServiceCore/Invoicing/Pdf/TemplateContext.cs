using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf;

public sealed class TemplateContext
{
    public string? PrimaryColorHex { get; init; }
    public string? SecondaryColorHex { get; init; }
    public byte[]? Logo { get; init; }
    public string? FontFamily { get; init; }
    public string? FooterLegalText { get; init; }
    public string? StructuredMessage { get; init; }
    public byte[]? EpcQrPng { get; init; }
    public byte[]? PageBackgroundImage { get; init; }
    public LayoutConfig? Layout { get; init; }
}