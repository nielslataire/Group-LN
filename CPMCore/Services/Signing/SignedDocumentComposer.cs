using CPMCore.Documents;
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

    public SignedDocumentComposer(IWebHostEnvironment env, ILogger<SignedDocumentComposer> logger, IEpcQrService qr)
    {
        _env = env;
        _logger = logger;
        _qr = qr;
    }

    public Task<byte[]> ComposeFinalPdfAsync(FinalDocumentInput input, CancellationToken ct = default)
    {
        var fontFamily = GroupLnFonts.EnsureAvenirRegistered(_env, _logger);
        var qrPng = SafeQrPng(input.VerificationUrl);
        var evidencePdf = new SigningEvidenceDocument(input, LogoBytes(), fontFamily, qrPng).GeneratePdf();
        return Task.FromResult(MergePdfDocuments(input.OriginalPdf, evidencePdf));
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
