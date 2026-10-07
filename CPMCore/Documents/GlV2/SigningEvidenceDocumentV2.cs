using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FacadeCore.Signing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    /// <summary>
    /// Ondertekeningsblad in de gl-v2-layout (design-handoff 35j): de bijlage die na de laatste handtekening achter de
    /// wijzigingsopdracht (35c) komt. Kicker "BIJLAGE · PAGINA n", intro, rij Document/Bedrag/Status, een blok per
    /// eigenaar (handtekening, naam + aandeel, tijdstip, methode, verificatie-ID, SHA-256) en "Echtheid controleren"
    /// met QR. Bedrijfsgegevens in kop/voet = het facturatiebedrijf van het document. Het oude
    /// <see cref="SigningEvidenceDocument"/> blijft de terugval voor documenttypes zonder wijzigingsopdracht-gegevens.
    /// De SHA-256 is die van het origineel (zoals ondertekend), niet van het uiteindelijke PDF (kip-en-ei).
    /// </summary>
    public sealed class SigningEvidenceDocumentV2 : GlV2PdfDocumentBase
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("nl-BE");
        private static readonly TimeZoneInfo Brussels = FindBrussels();

        private readonly FinalDocumentInput _input;
        private readonly ChangeOrderPdfModel _m;
        private readonly int _originalPages;
        private readonly byte[]? _qrPng;

        public SigningEvidenceDocumentV2(FinalDocumentInput input, ChangeOrderPdfModel model, GlV2PdfCompanyInfo company,
            bool fontsAvailable, int originalPages, byte[]? qrPng)
            : base(company, fontsAvailable)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _originalPages = Math.Max(1, originalPages);
            _qrPng = qrPng;
        }

        private static TimeZoneInfo FindBrussels()
        {
            foreach (var id in new[] { "Europe/Brussels", "Romance Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { }
            }
            return TimeZoneInfo.Local;
        }

        /// <summary>Opgeslagen als UTC (SigningService.Now); toont de Belgische tijd.</summary>
        private static DateTime Local(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Brussels);

        public override DocumentMetadata GetMetadata() => new()
        {
            Title = $"Ondertekeningsblad {_input.Case.Title}",
            Author = Company.Name,
            Subject = "Bewijs van elektronische ondertekening",
        };

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
        protected override string DocumentTitle => "Ondertekeningsblad";
        protected override string? Kicker => $"BIJLAGE · PAGINA {_originalPages + 1}";
        protected override string? DocumentNumber =>
            $"{_m.Reference} · voltooid {Local(_input.Case.CompletedAt ?? DateTime.UtcNow).ToString("dd/MM/yyyy HH:mm", Culture)}";
        protected override string? CompactSubtitle => _m.ProjectName;
        protected override string? PageLabelOverride => $"{_originalPages + 1} / {_originalPages + 1}";

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(4, Unit.Millimetre);

                col.Item().Element(x => Sectie(x, "Kopie met elektronische handtekening"));
                col.Item().Element(Intro);
                col.Item().Element(Overzicht);

                col.Item().PaddingTop(1, Unit.Millimetre).Element(x => Sectie(x, "Handtekeningen"));
                foreach (var s in _input.Signers)
                    col.Item().ShowEntire().Element(x => Handtekening(x, s));

                if (_qrPng is { Length: > 0 })
                {
                    col.Item().PaddingTop(1, Unit.Millimetre).Element(x => Sectie(x, "Echtheid controleren"));
                    col.Item().ShowEntire().Element(Echtheid);
                }
                // Zonder QR (generatie mislukt) geen kale link op het klantdocument — zelfde keuze als het oude blad.
            });
        }

        /// <summary>Sectielabel met een fijne lijn die de rest van de breedte vult (35j).</summary>
        private void Sectie(IContainer c, string label)
        {
            c.Row(row =>
            {
                row.Spacing(4, Unit.Millimetre);
                row.AutoItem().Text(label.ToUpperInvariant()).FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(GlV2PdfTheme.Groen);
                row.RelativeItem().AlignMiddle().LineHorizontal(0.3f, Unit.Millimetre).LineColor(GlV2PdfTheme.Lijn);
            });
        }

        private void Intro(IContainer c)
        {
            var naam = _m.Reference + (string.IsNullOrWhiteSpace(_m.Subject) ? "" : " · " + _m.Subject.Trim());
            var paginas = _originalPages == 1 ? "(pagina 1)" : $"(pagina 1 t/m {_originalPages})";
            var wie = _input.Signers.Count > 1 ? "alle eigenaars" : "de eigenaar";
            c.Text(t =>
            {
                t.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(8.5f).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f));
                t.Span("Dit blad maakt integraal deel uit van wijzigingsopdracht ");
                t.Span(naam).Bold();
                t.Span($" {paginas} en bewijst de elektronische ondertekening ervan door {wie}.");
            });
        }

        /// <summary>Drie vakken naast elkaar: Document · Bedrag · Status (rand 0,25mm, scheidingslijnen).</summary>
        private void Overzicht(IContainer c)
        {
            var laatste = _input.Signers.Count == 0 ? (DateTime?)null : _input.Signers.Max(s => s.SignedAt);
            var totaal = _input.Case.Parties.Count;
            var getekend = _input.Signers.Count;
            var sub = string.Join(" · ", new[] { _m.UnitsLine, _m.ProjectName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var status = totaal > 0 && getekend < totaal ? "Ondertekend" : "Volledig ondertekend";
            var statusSub = $"{getekend} van {Math.Max(totaal, getekend)}" + (laatste.HasValue ? " · " + Local(laatste.Value).ToString("dd/MM/yyyy HH:mm", Culture) : "");

            c.Border(0.25f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).Row(row =>
            {
                void Vak(string kop, string waarde, string onder, string? kleur, bool lijnRechts)
                {
                    var cell = row.RelativeItem();
                    if (lijnRechts) cell = cell.BorderRight(0.25f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn);
                    cell.PaddingVertical(2.6f, Unit.Millimetre).PaddingHorizontal(3.5f, Unit.Millimetre).Column(col =>
                    {
                        col.Spacing(0.8f, Unit.Millimetre);
                        col.Item().Text(kop).FontFamily(BodyFont).FontSize(6).SemiBold().LetterSpacing(0.12f).FontColor(GlV2PdfTheme.Gedempt);
                        col.Item().Text(waarde).FontFamily(BodyFont).FontSize(8).Bold().FontColor(kleur ?? GlV2PdfTheme.Inkt)
                            .EnableFontFeature(FontFeatures.TabularFigures);
                        col.Item().Text(onder).FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
                    });
                }
                Vak("DOCUMENT".ToUpperInvariant(), _m.Reference, sub, null, true);
                Vak("BEDRAG", GlV2PdfComponents.Euro(_m.TotalInclByLines), "incl. btw", null, true);
                Vak("STATUS", status, statusSub, GlV2PdfTheme.Groen, false);
            });
        }

        /// <summary>Eén blok per ondertekenaar: links de handtekening (62mm), rechts naam + aandeel en de bewijsvelden.</summary>
        private void Handtekening(IContainer c, SignedPartyInfo s)
        {
            var signer = _m.Signers.FirstOrDefault(x => string.Equals(x.Name, s.DisplayName, StringComparison.OrdinalIgnoreCase));
            var capacity = string.IsNullOrWhiteSpace(s.Capacity) || s.Capacity is "Klant" or "Mede-eigenaar" ? "eigenaar" : s.Capacity!.ToLowerInvariant();
            if (_input.Signers.Count > 1 && signer?.Percentage is decimal pct) capacity += " · " + GlV2PdfComponents.Btw(pct);
            var signedAt = Local(s.SignedAt);

            c.Border(0.25f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).Row(row =>
            {
                row.ConstantItem(62, Unit.Millimetre).BorderRight(0.25f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                    .Padding(4, Unit.Millimetre).Column(col =>
                    {
                        col.Spacing(2, Unit.Millimetre);
                        col.Item().Height(22, Unit.Millimetre).AlignMiddle().AlignLeft().Element(x =>
                        {
                            if (s.SignatureImagePng is { Length: > 0 } png) x.Image(png).FitArea();
                        });
                        col.Item().Row(r =>
                        {
                            r.Spacing(1.6f, Unit.Millimetre);
                            r.ConstantItem(3.2f, Unit.Millimetre).Svg(Vink);
                            r.RelativeItem().Text("Elektronisch ondertekend").FontFamily(BodyFont).FontSize(7).Bold().FontColor(GlV2PdfTheme.Groen);
                        });
                    });

                row.RelativeItem().PaddingVertical(4, Unit.Millimetre).PaddingHorizontal(4.5f, Unit.Millimetre).Column(col =>
                {
                    col.Spacing(2.6f, Unit.Millimetre);
                    col.Item().Text(t =>
                    {
                        t.Span(s.DisplayName).FontFamily(BodyFont).FontSize(9).Bold().FontColor(GlV2PdfTheme.Inkt);
                        t.Span("   " + capacity).FontFamily(BodyFont).FontSize(7.5f).FontColor(GlV2PdfTheme.Gedempt);
                    });
                    col.Item().Column(rows =>
                    {
                        rows.Spacing(1.6f, Unit.Millimetre);
                        Veld(rows, "ONDERTEKEND", $"{signedAt.ToString("dd/MM/yyyy", Culture)} om {signedAt.ToString("HH:mm", Culture)}", 8);
                        Veld(rows, "METHODE", MethodeLabel(s.VerificationMethod), 8);
                        Veld(rows, "VERIFICATIE-ID", s.PartyVerificationId.ToString("D").Substring(0, 23), 7);
                        Veld(rows, "SHA-256", _input.OriginalSha256 ?? "", 7);
                    });
                });
            });
        }

        private void Veld(ColumnDescriptor rows, string label, string waarde, float grootte)
        {
            rows.Item().Row(r =>
            {
                r.Spacing(3, Unit.Millimetre);
                r.ConstantItem(24, Unit.Millimetre).PaddingTop(0.4f, Unit.Millimetre)
                    .Text(label).FontFamily(BodyFont).FontSize(6).SemiBold().LetterSpacing(0.12f).FontColor(GlV2PdfTheme.Gedempt);
                r.RelativeItem().Text(waarde).FontFamily(BodyFont).FontSize(grootte).FontColor(GlV2PdfTheme.Inkt)
                    .EnableFontFeature(FontFeatures.TabularFigures);
            });
        }

        /// <summary>Vlak #F3F7F0 met QR (24mm, wit) en de uitleg + verificatielink (35j).</summary>
        private void Echtheid(IContainer c)
        {
            var url = _input.VerificationUrl;
            var link = url.Replace("https://", "").Replace("http://", "");
            c.Background(GlV2PdfTheme.VlakGroen).CornerRadius(1.5f, Unit.Millimetre).Padding(4, Unit.Millimetre).Row(row =>
            {
                row.Spacing(5, Unit.Millimetre);
                row.ConstantItem(24, Unit.Millimetre).Height(24, Unit.Millimetre).Background(GlV2PdfTheme.Wit).Hyperlink(url).Image(_qrPng!).FitArea();
                row.RelativeItem().AlignMiddle().Column(col =>
                {
                    col.Spacing(1.4f, Unit.Millimetre);
                    col.Item().Text("Scan de code of ga naar de verificatiepagina").FontFamily(BodyFont).FontSize(8.5f).SemiBold().FontColor(GlV2PdfTheme.Inkt);
                    // Echte, klikbare link (PDF-annotatie), niet enkel tekst: wie het blad op een scherm leest klikt gewoon door.
                    col.Item().Hyperlink(url).Text(link).FontFamily(BodyFont).FontSize(8).SemiBold().FontColor(GlV2PdfTheme.Groen).Underline();
                    col.Item().Text("De SHA-256-vingerafdruk is berekend op het document zoals ondertekend. Elke wijziging achteraf geeft een andere vingerafdruk — zo zie je dat het document echt is.")
                        .FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.5f);
                });
            });
        }

        private static string MethodeLabel(string? method) => method switch
        {
            "email-otp" => "e-mailcode",
            "sms-otp" => "sms-code",
            null or "" => "—",
            _ => method,
        };

        private const string Vink =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\"><path d=\"M3.4 8.4l3 3 6.2-6.8\" fill=\"none\" stroke=\"#00532D\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";
    }
}
