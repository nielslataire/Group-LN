using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2;

public sealed record ContactenLijstRij(string Naam, string Contact, string Bron, string Interesse, string Status, string Volgende, string Wie);

/// <summary>PDF-lijst van contacten (design-handoff 44d, formaat "PDF-lijst"): A4 liggend met de standaard gl-v2-kop en -voet.</summary>
public sealed class ContactenLijstDocumentV2 : GlV2PdfDocumentBase
{
    private readonly string _project;
    private readonly string _lijst;
    private readonly IReadOnlyList<ContactenLijstRij> _rijen;

    public ContactenLijstDocumentV2(string project, string lijstNaam, IReadOnlyList<ContactenLijstRij> rijen, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
    {
        _project = project; _lijst = lijstNaam; _rijen = rijen;
    }

    public override DocumentMetadata GetMetadata() => new() { Title = $"Contacten — {_project}", Author = Company.Name };
    protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Liggend;
    protected override string DocumentTitle => "Contacten";
    protected override string? Kicker => "CONTACTENLIJST";
    protected override string? DocumentNumber => DateTime.Today.ToString("dd/MM/yyyy") + " · " + _rijen.Count + " contacten";
    protected override string? CompactSubtitle => _project;

    protected override void Content(IContainer c)
    {
        c.Column(col =>
        {
            col.Spacing(3, Unit.Millimetre);
            col.Item().Text(t =>
            {
                t.Span(_project).FontFamily(BodyFont).FontSize(9).Bold().FontColor(GlV2PdfTheme.Inkt);
                if (!string.IsNullOrWhiteSpace(_lijst)) t.Span("   lijst \"" + _lijst + "\"").FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Gedempt);
            });
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(2.2f); d.RelativeColumn(2.6f); d.RelativeColumn(1f); d.RelativeColumn(1.6f); d.RelativeColumn(1.1f); d.RelativeColumn(1.9f); d.RelativeColumn(1.2f);
                });
                void Kop(string t) => table.Cell().Background(GlV2PdfTheme.Groen).PaddingHorizontal(2, Unit.Millimetre).PaddingVertical(1.6f, Unit.Millimetre)
                    .Text(t.ToUpperInvariant()).FontFamily(BodyFont).FontSize(6.3f).Bold().LetterSpacing(0.08f).FontColor("#FFFFFF");
                table.Header(h => { Kop("Contact"); Kop("Telefoon · e-mail"); Kop("Bron"); Kop("Interesse"); Kop("Status"); Kop("Volgende actie"); Kop("Wie"); });
                foreach (var r in _rijen)
                {
                    void Cel(string t, bool bold = false)
                    {
                        var tekst = table.Cell().BorderBottom(0.2f, Unit.Millimetre).BorderColor("#241F2A1E").PaddingHorizontal(2, Unit.Millimetre).PaddingVertical(1.6f, Unit.Millimetre)
                            .Text(string.IsNullOrWhiteSpace(t) ? "—" : t).FontFamily(BodyFont).FontSize(7.2f).FontColor(GlV2PdfTheme.Inkt);
                        if (bold) tekst.Bold();
                    }
                    Cel(r.Naam, true); Cel(r.Contact); Cel(r.Bron); Cel(r.Interesse); Cel(r.Status); Cel(r.Volgende); Cel(r.Wie);
                }
            });
        });
    }
}
