using CPMCore.Documents.GlV2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf;

namespace CPMCore.Services.Invoicing;

/// <summary>
/// De factuurlayout "layoutA" in de gl-v2-documentlayout (design-handoff 35b), Niels 2026-10-07. Vervangt de JSON-layout met dezelfde sleutel; "layoutB"
/// (en wat daar nog bij hoort) blijft de bestaande JSON-pijplijn. De JSON-instellingen van een bedrijf voor layoutA (kleuren, secties) worden hier
/// niet meer gebruikt: kleuren, lettertypes en opbouw komen uit <see cref="GlV2PdfTheme"/> en <see cref="InvoiceDocumentV2"/>. Logo, EPC-QR en
/// gestructureerde mededeling komen uit de bestaande <see cref="TemplateContext"/> (<c>InvoicePdfService.Render</c> bereidt ze voor).
/// </summary>
public sealed class GlV2InvoiceTemplate : NamedInvoiceTemplate
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GlV2InvoiceTemplate> _logger;

    public GlV2InvoiceTemplate(IWebHostEnvironment env, ILogger<GlV2InvoiceTemplate> logger) : base("layoutA")
    {
        _env = env;
        _logger = logger;
    }

    public override void Compose(IDocumentContainer container, InvoiceVm vm, TemplateContext ctx)
    {
        var fontsAvailable = GlV2PdfFonts.EnsureRegistered(_env, _logger);
        var issuer = vm.IssuerCompany;
        var company = new GlV2PdfCompanyInfo
        {
            Name = issuer.Name ?? "",
            Street = issuer.AddressLine ?? "",
            PostalCity = string.Join(" ", new[] { issuer.Postal, issuer.City }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Phone = issuer.Phone,
            Email = issuer.Email ?? "",
            Website = issuer.Website,
            LegalForm = string.Join(" ", new[] { issuer.LegalName ?? issuer.Name, issuer.LegalFormAbbreviation }.Where(s => !string.IsNullOrWhiteSpace(s))),
            VatNumber = issuer.VAT,
            Iban = issuer.DefaultIban ?? issuer.IBAN,
            LogoBytes = ctx.Logo,
        };
        new InvoiceDocumentV2(vm, ctx, company, fontsAvailable).Compose(container);
    }
}
