using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    /// <summary>Gegevens voor het werfverslag-PDF (Punten, design-handoff 40i/40l — layout van documentlayout 35/36).</summary>
    public sealed class WerfverslagPdfModel
    {
        public string Naam { get; init; } = "";
        public string TypeLabel { get; init; } = "Werfverslag";
        public string ProjectName { get; init; } = "";
        public DateOnly Datum { get; init; }
        public string? Uur { get; init; }
        public IReadOnlyList<string> Aanwezigen { get; init; } = Array.Empty<string>();
        public string? Weer { get; init; }
        public string? Opmerkingen { get; init; }
        public DateTime? VolgendBezoek { get; init; }
        public IReadOnlyList<WerfverslagPdfPunt> Nieuwe { get; init; } = Array.Empty<WerfverslagPdfPunt>();
        public IReadOnlyList<WerfverslagPdfPunt> Open { get; init; } = Array.Empty<WerfverslagPdfPunt>();
    }

    public sealed record WerfverslagPdfPunt(string Nr, string Titel, string Eenheid, string Aannemer, string Status, string TerPlaatse, string Opmerking);

    /// <summary>Werfverslag / oplevering als A4-document: meta (datum, aanwezigen, weer, volgend bezoek), algemene opmerkingen,
    /// nieuwe punten en de openstaande punten van het vorige verslag met wat ter plaatse vastgesteld werd.</summary>
    public sealed class WerfverslagDocumentV2 : GlV2PdfDocumentBase
    {
        private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
        private readonly WerfverslagPdfModel _m;

        public WerfverslagDocumentV2(WerfverslagPdfModel model, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override DocumentMetadata GetMetadata() => new() { Title = $"{_m.Naam} — {_m.ProjectName}", Author = Company.Name, Subject = _m.TypeLabel };

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
        protected override string DocumentTitle => _m.Naam;
        protected override string? Kicker => _m.TypeLabel.ToUpperInvariant();
        protected override string? DocumentNumber => _m.Datum.ToString("dd/MM/yyyy", Nl);
        protected override string? CompactSubtitle => _m.ProjectName;

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(4, Unit.Millimetre);

                col.Item().Element(x => Sectie(x, "Algemeen"));
                col.Item().Element(Meta);

                if (!string.IsNullOrWhiteSpace(_m.Opmerkingen))
                {
                    col.Item().Element(x => Sectie(x, "Algemene opmerkingen"));
                    col.Item().Text(_m.Opmerkingen!.Trim()).FontFamily(BodyFont).FontSize(8.5f).LineHeight(1.5f);
                }

                col.Item().Element(x => Sectie(x, $"Nieuwe punten ({_m.Nieuwe.Count})"));
                col.Item().Element(c2 => Punten(c2, _m.Nieuwe, metTerPlaatse: false, "Geen nieuwe punten tijdens dit bezoek."));

                col.Item().Element(x => Sectie(x, $"Openstaande punten uit het vorige verslag ({_m.Open.Count})"));
                col.Item().Element(c2 => Punten(c2, _m.Open, metTerPlaatse: true, "Geen openstaande punten."));
            });
        }

        private void Sectie(IContainer c, string label)
        {
            c.Row(row =>
            {
                row.Spacing(4, Unit.Millimetre);
                row.AutoItem().Text(label.ToUpperInvariant()).FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(GlV2PdfTheme.Groen);
                row.RelativeItem().AlignMiddle().LineHorizontal(0.3f, Unit.Millimetre).LineColor(GlV2PdfTheme.Lijn);
            });
        }

        private void Meta(IContainer c)
        {
            var rijen = new List<(string Label, string Waarde)>
            {
                ("Datum", _m.Datum.ToString("dddd d MMMM yyyy", Nl) + (string.IsNullOrWhiteSpace(_m.Uur) ? "" : " · " + _m.Uur)),
                ("Aanwezig", _m.Aanwezigen.Count == 0 ? "—" : string.Join(", ", _m.Aanwezigen)),
                ("Weer", string.IsNullOrWhiteSpace(_m.Weer) ? "—" : _m.Weer!),
                ("Volgend bezoek", _m.VolgendBezoek.HasValue ? _m.VolgendBezoek.Value.ToString("dd/MM/yyyy HH:mm", Nl) : "—"),
            };
            c.Column(col =>
            {
                col.Spacing(1.2f, Unit.Millimetre);
                foreach (var (label, waarde) in rijen)
                {
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(32, Unit.Millimetre).Text(label).FontFamily(BodyFont).FontSize(8f).FontColor(GlV2PdfTheme.Gedempt);
                        r.RelativeItem().Text(waarde).FontFamily(BodyFont).FontSize(8.5f).SemiBold();
                    });
                }
            });
        }

        private void Punten(IContainer c, IReadOnlyList<WerfverslagPdfPunt> punten, bool metTerPlaatse, string leeg)
        {
            if (punten.Count == 0)
            {
                c.Text(leeg).FontFamily(BodyFont).FontSize(8f).FontColor(GlV2PdfTheme.Gedempt);
                return;
            }
            var kolommen = new List<GlV2PdfComponents.TabelKolom>
            {
                new("NR.", 13), new("PUNT"), new("EENHEID", 22), new("AANNEMER", 30), new("STATUS", 24),
            };
            if (metTerPlaatse) kolommen.Add(new("TER PLAATSE", 26));
            var rijen = punten.Select(p =>
            {
                var cellen = new List<GlV2PdfComponents.TabelCel>
                {
                    new(p.Nr), new(p.Titel + (string.IsNullOrWhiteSpace(p.Opmerking) ? "" : "\n" + p.Opmerking)), new(p.Eenheid), new(p.Aannemer), new(p.Status),
                };
                if (metTerPlaatse) cellen.Add(new(p.TerPlaatse));
                return (IReadOnlyList<GlV2PdfComponents.TabelCel>)cellen;
            });
            GlV2PdfComponents.Tabel(c, BodyFont, kolommen, rijen);
        }
    }
}
