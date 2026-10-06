using CPMCore.Documents;
using CPMCore.Documents.GlV2;
using FacadeCore.Signing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf;

namespace CPMCore.Services.Signing;

/// <summary>
/// De <see cref="ISigningDocumentRenderer"/> van fase 2 (ONDERTEKENEN_VOORSTEL.md §5.3): het
/// ondertekende document = origineel + <see cref="SigningEvidenceDocument"/> samengevoegd met
/// PdfSharpCore (zelfde mergepatroon als InvoicesController.MergePdfDocuments — origineel blijft
/// byte-identiek aan wat de ondertekenaar zag, geen re-render); het auditrapport is een eigen PDF
/// (<see cref="SigningAuditReportDocument"/>). Vervangt de fase-0-stub
/// <c>NotAvailableSigningDocumentRenderer</c>. De QR op het evidence-blad (fase 3, wijst naar
/// <c>/verifieer/{id}</c>) hergebruikt <see cref="IEpcQrService"/> — al generiek genoeg (enkel
/// payload-string naar PNG), geen nieuwe QR-dienst nodig.
/// </summary>
public sealed class SignedDocumentComposer : ISigningDocumentRenderer
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SignedDocumentComposer> _logger;
    private readonly IEpcQrService _qr;
    private readonly IServiceProvider _services;

    public SignedDocumentComposer(IWebHostEnvironment env, ILogger<SignedDocumentComposer> logger, IEpcQrService qr, IServiceProvider services)
    {
        _env = env;
        _logger = logger;
        _qr = qr;
        _services = services;
    }

    public async Task<byte[]> ComposeFinalPdfAsync(FinalDocumentInput input, CancellationToken ct = default)
    {
        var qrPng = SafeQrPng(input.VerificationUrl);

        // Wijzigingsopdracht: ondertekeningsblad in de gl-v2-layout (design-handoff 35j). Lukt dat niet (of is het
        // een ander documenttype), dan het bestaande blad — het ondertekenen mag hier nooit op stuk lopen.
        byte[]? evidencePdf = null;
        if (input.Case.DocumentType == ChangeOrderSigningSource.Key)
        {
            try { evidencePdf = await RenderChangeOrderEvidenceAsync(input, qrPng, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ondertekeningsblad (gl-v2) voor dossier {CaseId} mislukt; terugval op het standaardblad.", input.Case.CaseId); }
        }

        if (evidencePdf is null)
        {
            var fontFamily = GroupLnFonts.EnsureAvenirRegistered(_env, _logger);
            evidencePdf = new SigningEvidenceDocument(input, LogoBytes(), fontFamily, qrPng).GeneratePdf();
        }
        return MergePdfDocuments(input.OriginalPdf, evidencePdf);
    }

    private async Task<byte[]> RenderChangeOrderEvidenceAsync(FinalDocumentInput input, byte[]? qrPng, CancellationToken ct)
    {
        var builder = _services.GetRequiredService<ChangeOrderPdfBuilder>();
        var model = await builder.LoadAsync(input.Case.SourceEntityId, ct)
                    ?? throw new InvalidOperationException($"Wijzigingsopdracht {input.Case.SourceEntityId} bestaat niet.");
        var fontsAvailable = GlV2PdfFonts.EnsureRegistered(_env, _logger);
        var company = builder.BuildGlV2Company(model);
        int originalPages;
        using (var ms = new MemoryStream(input.OriginalPdf, writable: false))
        using (var src = PdfReader.Open(ms, PdfDocumentOpenMode.Import))
            originalPages = src.PageCount;
        return new SigningEvidenceDocumentV2(input, model, company, fontsAvailable, originalPages, qrPng).GeneratePdf();
    }

    /// <summary>QR-generatie mag het ondertekenen nooit laten mislukken — zonder QR blijft de
    /// verificatie-URL als tekst op het interne auditrapport staan.</summary>
    private byte[]? SafeQrPng(string verificationUrl)
    {
        try { return _qr.CreatePngFromPayload(verificationUrl); }
        catch (Exception ex) { _logger.LogWarning(ex, "QR-code voor {Url} kon niet gemaakt worden.", verificationUrl); return null; }
    }

    public Task<byte[]> RenderAuditReportAsync(AuditReportInput input, CancellationToken ct = default)
    {
        var fontFamily = GroupLnFonts.EnsureAvenirRegistered(_env, _logger);
        var report = new SigningAuditReportDocument(input, LogoBytes(), fontFamily).GeneratePdf();
        return Task.FromResult(report);
    }

    private byte[]? LogoBytes()
    {
        var path = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private static byte[] MergePdfDocuments(byte[] original, byte[] appendix)
    {
        using var output = new PdfDocument();
        AppendPages(output, original);
        AppendPages(output, appendix);

        using var stream = new MemoryStream();
        output.Save(stream, false);
        return stream.ToArray();
    }

    private static void AppendPages(PdfDocument target, byte[] sourcePdf)
    {
        using var sourceStream = new MemoryStream(sourcePdf, writable: false);
        using var source = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
        for (var i = 0; i < source.PageCount; i++)
            target.AddPage(source.Pages[i]);
    }
}
