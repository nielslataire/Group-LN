using FacadeCore.Signing;

namespace ServiceCore.Signing;

/// <summary>
/// Fase 0-plaatsvervanger (ONDERTEKENEN_VOORSTEL.md §10): het ondertekende document en het
/// auditrapport worden pas in fase 2 gerenderd (CPMCore, QuestPDF + PdfSharpCore). Tot dan kan een
/// dossier wel aangemaakt, aangeboden en opgevolgd worden, maar niet gefinaliseerd — en dat
/// faalt hier luid en duidelijk i.p.v. stil een leeg bestand te bewaren. De service vangt dit op als
/// <c>FinalizationFailed</c>-event en probeert later opnieuw zodra een echte renderer geregistreerd is.
/// </summary>
public sealed class NotAvailableSigningDocumentRenderer : ISigningDocumentRenderer
{
    public Task<byte[]> ComposeFinalPdfAsync(FinalDocumentInput input, CancellationToken ct = default)
        => throw new NotSupportedException("Renderen van het ondertekende document is nog niet beschikbaar (fase 2 van ONDERTEKENEN_VOORSTEL.md).");

    public Task<byte[]> RenderAuditReportAsync(AuditReportInput input, CancellationToken ct = default)
        => throw new NotSupportedException("Renderen van het auditrapport is nog niet beschikbaar (fase 2 van ONDERTEKENEN_VOORSTEL.md).");
}
