using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    /// <summary>
    /// Gedeelde pagina-opbouw voor élk gl-v2-document (design-handoff punt 35a "Anatomie" + punt 36
    /// "Opmaakspecificaties"): groene band boven/onder (6mm, volle breedte), 18mm zijmarge, kop
    /// (volledig of compact) en voet (adres · contact · rechtsvorm+btw+IBAN · paginanummer).
    /// Concrete documenten (<see cref="ChangeOrderDocumentV2"/>, en later factuur/lijsten/budget)
    /// leveren enkel titel, paginaformaat en <see cref="Content"/> — kleuren, kop en voet komen hier
    /// vandaan zodat nieuwe gl-v2-PDF's er zonder extra werk hetzelfde uitzien. Losstaand van
    /// <see cref="GroupLnPdfDocument"/> (de bestaande werf-PDF's/offerte blijven ongewijzigd werken
    /// voor wie niet in gl-v2 zit) en van <c>ServiceCore/Invoicing/Pdf/</c> (de facturatie-layouts,
    /// per uitgevend bedrijf instelbaar — daar verandert dit niets aan).
    /// </summary>
    public abstract class GlV2PdfDocumentBase : IDocument
    {
        protected readonly GlV2PdfCompanyInfo Company;
        private readonly bool _fontsAvailable;

        protected GlV2PdfDocumentBase(GlV2PdfCompanyInfo company, bool fontsAvailable)
        {
            Company = company;
            _fontsAvailable = fontsAvailable;
        }

        protected string TitleFont => _fontsAvailable ? GlV2PdfTheme.Playfair : "Lato";
        protected string BodyFont => _fontsAvailable ? GlV2PdfTheme.Plex : "Lato";

        public abstract DocumentMetadata GetMetadata();

        /// <summary>Paginaformaat (design-handoff punt 35a "Formaten"); zie DESIGN.md voor welk
        /// document welk formaat gebruikt.</summary>
        protected abstract GlV2PdfFormat Format { get; }

        /// <summary>Documenten voor klanten (factuur, offerte, wijzigingsopdracht, klantenlijst)
        /// krijgen de volledige kop op de eerste pagina en de compacte kop op vervolgpagina's.
        /// Lijsten/overzichten gebruiken de compacte kop vanaf pagina 1. (Punt 35a/36.4.)</summary>
        protected virtual bool CompactHeaderFromFirstPage => false;

        /// <summary>True = de volledige kop (groot logo, titel, kicker) op elke pagina i.p.v. de compacte kop vanaf pagina 2 (budget).</summary>
        protected virtual bool FullHeaderOnEveryPage => false;

        /// <summary>Huiskleur van het document: banden, kicker, nummer, sectielabels (standaard het gl-v2-groen). Een document kan hem
        /// overschrijven met de kleur van het uitgevende bedrijf (factuur: <c>BrandPrimaryColor</c>).</summary>
        protected virtual string Accent => GlV2PdfTheme.Groen;

        protected abstract string DocumentTitle { get; }
        /// <summary>Kicker boven de titel, bv. "MEERWERK"/"RENOVATIE" (punt 35b/35c/35d); optioneel.</summary>
        protected virtual string? Kicker => null;
        /// <summary>Documentnummer rechts onder de titel, bv. "WO-006 · versie 1"; optioneel.</summary>
        protected virtual string? DocumentNumber => null;
        /// <summary>Ondertitel in de compacte kop (bv. projectnaam); optioneel.</summary>
        protected virtual string? CompactSubtitle => null;

        /// <summary>De eigenlijke pagina-inhoud.</summary>
        protected abstract void Content(IContainer c);

        /// <summary>Vast paginalabel in de voet (bv. "2 / 2" voor een bijlage die achter een ander PDF komt);
        /// null = "x / y" van dit document zelf.</summary>
        protected virtual string? PageLabelOverride => null;

        /// <summary>Optelling op het paginanummer in de voet (bijlage die achter een ander PDF komt); 0 = geen.</summary>
        protected virtual int PageNumberOffset => 0;
        /// <summary>Totaal aantal pagina's van het samengevoegde geheel in de voet; null = totaal van dit document.</summary>
        protected virtual int? TotalPagesOverride => null;

        /// <summary>Extra regel boven de titel in de compacte kop (bv. "INTERN — NIET VERSPREIDEN" bij het budget, 35q/35r); optioneel.</summary>
        protected virtual string? CompactKicker => null;
        /// <summary>Kleur van <see cref="CompactKicker"/>; standaard de huiskleur.</summary>
        protected virtual string? CompactKickerKleur => null;
        /// <summary>Kleine toelichtingsregel onder de ondertitel in de compacte kop (bv. "Budget v2 · definitief 13/10/2026"); optioneel.</summary>
        protected virtual string? CompactNote => null;

        public virtual void Compose(IDocumentContainer container) => ComposePage(container, Format, Content);

        /// <summary>Eén pagina-reeks met de standaardkop/-voet; een document met meerdere formaten (budget: A4 staand + A4 liggend)
        /// roept dit meermaals aan vanuit zijn eigen <see cref="Compose"/>.</summary>
        protected void ComposePage(IDocumentContainer container, GlV2PdfFormat format, Action<IContainer> content, bool? compactVanafEerste = null)
        {
            container.Page(page =>
            {
                page.Size(format.ToPageSize());
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(8f).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.45f));

                page.Header().Column(col =>
                {
                    col.Item().Height(GlV2PdfTheme.BandHoogteMm, Unit.Millimetre).Background(Accent);
                    col.Item().PaddingHorizontal(GlV2PdfTheme.ZijMargeMm, Unit.Millimetre).PaddingTop(10, Unit.Millimetre).Column(head =>
                    {
                        if (FullHeaderOnEveryPage)
                        {
                            head.Item().Element(FullHeader);
                        }
                        else if (compactVanafEerste ?? CompactHeaderFromFirstPage)
                        {
                            head.Item().Element(CompactHeader);
                        }
                        else
                        {
                            head.Item().ShowOnce().Element(FullHeader);
                            head.Item().SkipOnce().Element(CompactHeader);
                        }
                    });
                });

                page.Content()
                    .PaddingHorizontal(GlV2PdfTheme.ZijMargeMm, Unit.Millimetre)
                    .PaddingTop(10, Unit.Millimetre)
                    .Element(content);

                page.Footer().Column(col =>
                {
                    col.Item().PaddingHorizontal(GlV2PdfTheme.ZijMargeMm, Unit.Millimetre).Element(Footer);
                    col.Item().Height(4, Unit.Millimetre);
                    col.Item().Height(GlV2PdfTheme.BandHoogteMm, Unit.Millimetre).Background(Accent);
                });
            });
        }

        // ── Kop (punt 35a "KOP") — enkel het logo, over de volledige hoogte van de kopzone; geen
        // bedrijfsnaam ernaast (Niels, 2026-10-05: afwijking van de 35b-mockup die wel een naam toont).
        private void FullHeader(IContainer c)
        {
            c.Row(row =>
            {
                row.RelativeItem().Height(GlV2PdfTheme.KopHoogteMm, Unit.Millimetre).AlignLeft().Element(logo =>
                {
                    if (Company.LogoBytes is { Length: > 0 })
                        logo.Image(Company.LogoBytes).FitHeight();
                });
                row.ConstantItem(CompactNote is null ? 90 : 120, Unit.Millimetre).AlignRight().Column(title =>
                {
                    if (!string.IsNullOrWhiteSpace(Kicker))
                        title.Item().AlignRight().Text(Kicker!.ToUpperInvariant()).FontFamily(BodyFont).FontSize(7).Bold().LetterSpacing(0.18f).FontColor(CompactKickerKleur ?? Accent);
                    title.Item().AlignRight().Text(DocumentTitle).FontFamily(TitleFont).FontSize(22).FontColor(GlV2PdfTheme.Inkt);
                    if (!string.IsNullOrWhiteSpace(DocumentNumber))
                        title.Item().AlignRight().Text(DocumentNumber!).FontFamily(BodyFont).FontSize(9).SemiBold().FontColor(Accent)
                            .EnableFontFeature(FontFeatures.TabularFigures);
                    if (!string.IsNullOrWhiteSpace(CompactNote))
                        title.Item().AlignRight().Text(CompactNote!).FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
                });
            });
        }

        /// <summary>Compacte kop (punt 35a/35e): kleiner logo, titel links de bedrijfsnaam, rechts de
        /// documenttitel/ondertitel — gebruikt voor lijsten vanaf pagina 1, en voor élk ander
        /// documenttype vanaf de tweede pagina.</summary>
        private void CompactHeader(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Row(logoRow =>
                    {
                        if (Company.LogoBytes is { Length: > 0 })
                            logoRow.ConstantItem(GlV2PdfTheme.LogoCompactMm, Unit.Millimetre).AlignMiddle().Image(Company.LogoBytes).FitWidth();
                        logoRow.ConstantItem(2, Unit.Millimetre);
                        logoRow.AutoItem().AlignMiddle().Text(Company.Name).FontFamily(BodyFont).FontSize(11).Bold().FontColor(GlV2PdfTheme.Bedrijfsnaam);
                    });
                    row.ConstantItem(CompactNote is null ? 90 : 120, Unit.Millimetre).AlignRight().Column(title =>
                    {
                        if (!string.IsNullOrWhiteSpace(CompactKicker))
                            title.Item().AlignRight().Text(CompactKicker!.ToUpperInvariant()).FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.18f).FontColor(CompactKickerKleur ?? Accent);
                        title.Item().AlignRight().Text(DocumentTitle).FontFamily(TitleFont).FontSize(18).FontColor(GlV2PdfTheme.Inkt);
                        if (!string.IsNullOrWhiteSpace(CompactSubtitle))
                            title.Item().AlignRight().Text(CompactSubtitle!).FontFamily(BodyFont).FontSize(8.5f).Bold().FontColor(Accent);
                        else if (!string.IsNullOrWhiteSpace(DocumentNumber))
                            title.Item().AlignRight().Text(DocumentNumber!).FontFamily(BodyFont).FontSize(8).SemiBold().FontColor(Accent)
                                .EnableFontFeature(FontFeatures.TabularFigures);
                        if (!string.IsNullOrWhiteSpace(CompactNote))
                            title.Item().AlignRight().Text(CompactNote!).FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
                    });
                });
                col.Item().PaddingTop(4, Unit.Millimetre).BorderBottom(0.6f, Unit.Millimetre).BorderColor(Accent);
            });
        }

        // ── Voet (punt 35a "VOET", 36.5) - kolommen zoals het offerte-voorbeeld (35d): adres (smal)
        // en contact, allebei links uitgelijnd; rechtsvorm+btw+IBAN rechts uitgelijnd; enkel
        // "x / y" (geen "pagina"-tekst), vast en rechts uitgelijnd op de rand.
        protected virtual void Footer(IContainer c)
        {
            c.BorderTop(0.3f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                .PaddingTop(3, Unit.Millimetre).Row(row =>
            {
                void Col(string text, float weight, bool right = false)
                {
                    var item = row.RelativeItem(weight).Text(text).FontFamily(BodyFont).FontSize(6.5f).LineHeight(1.45f).FontColor(GlV2PdfTheme.Gedempt)
                        .EnableFontFeature(FontFeatures.TabularFigures);
                    if (right) item.AlignRight();
                }

                Col($"{Company.Street}\n{Company.PostalCity}", 0.7f);
                // Zelfde twee regels als het offerte-voorbeeld (35d): "tel · mail" op regel 1, website op regel 2.
                var contactLine1 = string.Join(" · ", new[] { Company.Phone, Company.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
                Col(string.Join("\n", new[] { contactLine1, Company.Website }.Where(s => !string.IsNullOrWhiteSpace(s))), 1f);
                // "rechtsvorm + btw" op regel 1, IBAN op regel 2 (35d) - IBAN met een niet-brekende
                // spatie i.p.v. een gewone spatie zodat die niet midden in het rekeningnummer afbreekt.
                var legalLine1 = string.Join(" · ", new[] { Company.LegalForm, FormatVat(Company.VatNumber) }.Where(s => !string.IsNullOrWhiteSpace(s)));
                Col(string.Join("\n", new[] { legalLine1, Company.Iban?.Replace(" ", " ") }.Where(s => !string.IsNullOrWhiteSpace(s))), 1.3f, right: true);

                row.ConstantItem(16, Unit.Millimetre).AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(6.5f).SemiBold().FontColor(GlV2PdfTheme.Inkt));
                    if (PageLabelOverride is { } label) { t.Span(label); return; }
                    if (PageNumberOffset != 0) t.CurrentPageNumber().Format(n => ((n ?? 0) + PageNumberOffset).ToString());
                    else t.CurrentPageNumber();
                    t.Span(" / ");
                    if (TotalPagesOverride is { } tot) t.Span(tot.ToString());
                    else t.TotalPages();
                });
            });
        }

        private static string? FormatVat(string? vat) => string.IsNullOrWhiteSpace(vat) ? null : $"btw {vat}";

        // ── Gedeelde bouwstenen voor afgeleide documenten ────────────────────────
        /// <summary>Sectielabel (punt 36.3/36.5): kleine groene hoofdletterkop boven een blok.</summary>
        protected void SectionLabel(IContainer c, string text, bool first = false)
        {
            c.PaddingTop(first ? 0 : 10).PaddingBottom(3).Text(text.ToUpperInvariant())
                .FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(Accent);
        }
    }
}
