namespace FacadeCore.Signing;

/// <summary>
/// Rendert de definitieve documenten (ONDERTEKENEN_VOORSTEL.md §5.3): het ondertekende document
/// (origineel + ondertekeningsblad) en het auditrapport. Geïmplementeerd in CPMCore (fase 2) met
/// QuestPDF op de <c>GroupLnPdfDocument</c>-basis + PdfSharpCore om samen te voegen — de service-
/// laag blijft vrij van PDF-engines. In fase 0 is er een stub die duidelijk meldt dat rendering
/// nog niet beschikbaar is; finaliseren is dan ook onbereikbaar (geen publieke pagina).
/// </summary>
public interface ISigningDocumentRenderer
{
    Task<byte[]> ComposeFinalPdfAsync(FinalDocumentInput input, CancellationToken ct = default);

    Task<byte[]> RenderAuditReportAsync(AuditReportInput input, CancellationToken ct = default);
}
