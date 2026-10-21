using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    // ── Model (design-handoff 35k werfverslag pagina 1, 35l pagina 2, 35m bijlage plannen) ─────────────────────────────

    public sealed record WerfverslagPdfNieuw(int Num, string Nr, string Titel, string Eenheid, string Aannemer, string Status, bool Kritiek,
        string? Beschrijving, string? Deadline, string? Plan, IReadOnlyList<byte[]> Fotos);

    public sealed record WerfverslagPdfOpen(string Nr, string Titel, string? Notitie, string Eenheid, string Aannemer, string Vorig, string Nu);

    public sealed record WerfverslagPdfVerdeling(string Naam, string? Email, string Bereik);

    public sealed record WerfverslagPdfPlanPunt(string Label, string Nr, string Tekst, bool Kritiek);

    public sealed record WerfverslagPdfPlan(string Naam, string Sub, byte[] Image, IReadOnlyList<WerfverslagPdfPlanPunt> Punten);

    public sealed class WerfverslagPdfModel
    {
        public string Naam { get; init; } = "";
        public string TypeLabel { get; init; } = "Werfverslag";
        public string ProjectName { get; init; } = "";
        public string? ProjectAdres { get; init; }
        public DateOnly Datum { get; init; }
        public string? Uur { get; init; }
        public string? EindUur { get; init; }
        public string? VorigBezoek { get; init; }
        public IReadOnlyList<string> Aanwezigen { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Verontschuldigd { get; init; } = Array.Empty<string>();
        public string? Weer { get; init; }
        public string? Opmerkingen { get; init; }
        public DateTime? VolgendBezoek { get; init; }
        public IReadOnlyList<WerfverslagPdfNieuw> Nieuwe { get; init; } = Array.Empty<WerfverslagPdfNieuw>();
        public IReadOnlyList<WerfverslagPdfOpen> Open { get; init; } = Array.Empty<WerfverslagPdfOpen>();
        public IReadOnlyList<WerfverslagPdfOpen> Afgesloten { get; init; } = Array.Empty<WerfverslagPdfOpen>();
        public int Kritiek { get; init; }
        public IReadOnlyList<WerfverslagPdfVerdeling> Verdeling { get; init; } = Array.Empty<WerfverslagPdfVerdeling>();
        public IReadOnlyList<WerfverslagPdfPlan> Plannen { get; init; } = Array.Empty<WerfverslagPdfPlan>();
    }

    /// <summary>Kleuren en bouwstenen uit de mockups 35k/35l/35m (zelfde tokens als de rest van de gl-v2-documenten).</summary>
    internal static class WerfverslagStijl
    {
        public static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
        public const string Groen = "#00532D";
        public const string Inkt = "#1F2A1E";
        public const string Gedempt = "#56664F";
        public const string Rood = "#8B2A2A";
        public const string Goud = "#8A6A32";
        public const string Rand = "#241F2A1E";   // rgba(31,42,30,.14)
        public const string VlakGroen = "#F3F7F0";

        /// <summary>Sectiekop: 6,5pt 700, letterspatiëring .16em, groen, met een fijne lijn tot het einde (35k).</summary>
        public static void Sectie(IContainer c, string label, string bodyFont)
        {
            c.Row(row =>
            {
                row.Spacing(4, Unit.Millimetre);
                row.AutoItem().Text(label.ToUpperInvariant()).FontFamily(bodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(Groen);
                row.RelativeItem().AlignMiddle().LineHorizontal(0.3f, Unit.Millimetre).LineColor(Rand);
            });
        }

        /// <summary>Genummerd bolletje (35k/35m): zelfde nummer in het verslag en op het plan; groen = nieuw, rood = kritiek, goud "O" = open uit een vorig verslag.</summary>
        public static void Bol(IContainer c, string label, bool kritiek, string bodyFont, float mm = 4.6f)
        {
            var kleur = kritiek ? Rood : (label == "O" ? Goud : Groen);
            c.Width(mm, Unit.Millimetre).Height(mm, Unit.Millimetre).Background(kleur).CornerRadius(mm / 2f, Unit.Millimetre)
                .AlignCenter().AlignMiddle().Text(label).FontFamily(bodyFont).FontSize(5.8f).Bold().FontColor("#FFFFFF");
        }

        /// <summary>Statuspil: afgeronde achtergrond met een eigen tint per status (35k/35l).</summary>
        public static void Pil(IContainer c, string tekst, string bodyFont)
        {
            var (bg, fg) = tekst switch
            {
                "Doorgestuurd" or "Goedgekeurd" => ("#E2ECDA", Groen),
                "Gemeld uitgevoerd" or "Ter goedkeuring" => ("#F6EEDC", Goud),
                "Afgesloten" => (Groen, "#FFFFFF"),
                "Kritiek" or "Afgewezen" => ("#F7E6E3", Rood),
                "Concept" => ("#EDEFEB", "#4A5B48"),
                _ => ("#EDEFEB", Gedempt),
            };
            c.Background(bg).CornerRadius(3, Unit.Millimetre).PaddingHorizontal(2.2f, Unit.Millimetre).PaddingVertical(0.7f, Unit.Millimetre)
                .Text(tekst).FontFamily(bodyFont).FontSize(6.3f).SemiBold().FontColor(fg);
        }
    }

    /// <summary>Werfverslag / oplevering — pagina 1 en 2 (A4 staand), exact volgens de mockups 35k en 35l: kerngegevens in een raster, samenvatting,
    /// nieuwe punten als kaarten (bol, nummer, titel, eenheid, aannemer, status, beschrijving, deadline, plan, foto's), openstaande en afgesloten
    /// punten als tabel met groene kop, algemene opmerkingen en verdeling. De plannen zitten in <see cref="WerfverslagBijlageDocumentV2"/>.</summary>
    public sealed class WerfverslagDocumentV2 : GlV2PdfDocumentBase
    {
        private readonly WerfverslagPdfModel _m;
        private readonly int? _totaal;

        /// <param name="totaalPaginas">Totaal aantal bladzijden van het samengevoegde geheel (verslag + bijlage-pagina's) voor de "x / y" in de voet; null = enkel dit document.</param>
        public WerfverslagDocumentV2(WerfverslagPdfModel model, GlV2PdfCompanyInfo company, bool fontsAvailable, int? totaalPaginas = null) : base(company, fontsAvailable)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _totaal = totaalPaginas;
        }

        public override DocumentMetadata GetMetadata() => new() { Title = $"{_m.Naam} — {_m.ProjectName}", Author = Company.Name, Subject = _m.TypeLabel };

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
        protected override string DocumentTitle => _m.Naam;
        protected override string? Kicker => _m.TypeLabel.ToUpperInvariant();
        protected override string? DocumentNumber => _m.Datum.ToString("dd/MM/yyyy", WerfverslagStijl.Nl) + " · versie 1";
        protected override string? CompactSubtitle => _m.ProjectName;
        protected override int? TotalPagesOverride => _totaal;

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(3.6f, Unit.Millimetre);
                col.Item().Element(Meta);
                col.Item().PaddingTop(1.5f, Unit.Millimetre).Element(Samenvatting);

                // Nieuwe punten: de kop blijft bij de eerste kaart; elke kaart blijft heel
                col.Item().PaddingTop(1.5f, Unit.Millimetre).EnsureSpace(128).Element(x => WerfverslagStijl.Sectie(x, $"Nieuwe punten ({_m.Nieuwe.Count})", BodyFont));
                if (_m.Nieuwe.Count == 0)
                    col.Item().Text("Geen nieuwe punten tijdens dit bezoek.").FontFamily(BodyFont).FontSize(7.8f).FontColor(WerfverslagStijl.Gedempt);
                foreach (var p in _m.Nieuwe)
                    col.Item().ShowEntire().Element(x => NieuwPunt(x, p));

                if (_m.Open.Count > 0 || _m.Afgesloten.Count > 0)
                    col.Item().PaddingTop(1.5f, Unit.Millimetre).EnsureSpace(100).Element(x => WerfverslagStijl.Sectie(x, $"Openstaande punten uit vorige verslagen ({_m.Open.Count})", BodyFont));
                if (_m.Open.Count > 0) col.Item().Element(x => OpenTabel(x, _m.Open));
                if (_m.Afgesloten.Count > 0)
                {
                    col.Item().PaddingTop(1.5f, Unit.Millimetre).EnsureSpace(85).Element(x => WerfverslagStijl.Sectie(x, $"Afgesloten sinds het vorige verslag ({_m.Afgesloten.Count})", BodyFont));
                    col.Item().Element(x => OpenTabel(x, _m.Afgesloten));
                }

                // korte blokken blijven samen (kop + inhoud op één pagina)
                if (!string.IsNullOrWhiteSpace(_m.Opmerkingen))
                    col.Item().PaddingTop(1.5f, Unit.Millimetre).ShowEntire().Column(k =>
                    {
                        k.Spacing(3.6f, Unit.Millimetre);
                        k.Item().Element(x => WerfverslagStijl.Sectie(x, "Algemene opmerkingen", BodyFont));
                        k.Item().Element(Opmerkingen);
                    });

                if (_m.Verdeling.Count > 0)
                    col.Item().PaddingTop(1.5f, Unit.Millimetre).ShowEntire().Column(k =>
                    {
                        k.Spacing(3.6f, Unit.Millimetre);
                        k.Item().Element(x => WerfverslagStijl.Sectie(x, "Verdeling", BodyFont));
                        k.Item().Element(Verdeling);
                        k.Item().PaddingTop(2, Unit.Millimetre).Text("Opmerkingen op dit verslag binnen 8 dagen schriftelijk; daarna geldt het als aanvaard.")
                            .FontFamily(BodyFont).FontSize(6.8f).LineHeight(1.5f).FontColor(WerfverslagStijl.Gedempt);
                    });
            });
        }

        // ── 35k: kerngegevens in een raster van 3 kolommen met fijne lijnen ──
        private void Cel(IContainer c, string label, string value, string? sub, bool rechts, bool onder)
        {
            var box = c;
            if (rechts) box = box.BorderRight(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand);
            if (onder) box = box.BorderBottom(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand);
            box.PaddingHorizontal(3.2f, Unit.Millimetre).PaddingVertical(2.4f, Unit.Millimetre).Column(k =>
            {
                k.Spacing(0.8f, Unit.Millimetre);
                k.Item().Text(label.ToUpperInvariant()).FontFamily(BodyFont).FontSize(5.8f).SemiBold().LetterSpacing(0.12f).FontColor(WerfverslagStijl.Gedempt);
                k.Item().Text(value).FontFamily(BodyFont).FontSize(7.8f).Bold().FontColor(WerfverslagStijl.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                if (!string.IsNullOrWhiteSpace(sub)) k.Item().Text(sub).FontFamily(BodyFont).FontSize(6.5f).LineHeight(1.4f).FontColor(WerfverslagStijl.Gedempt);
            });
        }

        private void Meta(IContainer c)
        {
            var datum = _m.Datum.ToString("dddd d MMMM yyyy", WerfverslagStijl.Nl);
            // 35k: onder de datum staat "16:29 – 17:40"; het weer hoort er ook bij
            var tijd = string.IsNullOrWhiteSpace(_m.Uur) ? null : _m.Uur + (string.IsNullOrWhiteSpace(_m.EindUur) ? "" : " – " + _m.EindUur);
            var sub = string.Join(" · ", new[] { tijd, string.IsNullOrWhiteSpace(_m.Weer) ? null : _m.Weer }.Where(x => !string.IsNullOrWhiteSpace(x)));
            c.Border(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).Column(g =>
            {
                g.Item().Row(r =>
                {
                    r.RelativeItem().Element(x => Cel(x, "Datum", datum, sub == "" ? null : sub, true, true));
                    r.RelativeItem().Element(x => Cel(x, "Project", _m.ProjectName, _m.ProjectAdres, true, true));
                    r.RelativeItem().Element(x => Cel(x, "Vorig bezoek", _m.VorigBezoek ?? "—", null, false, true));
                });
                g.Item().Row(r =>
                {
                    r.RelativeItem().Element(x => Cel(x, "Aanwezig", _m.Aanwezigen.Count == 0 ? "—" : string.Join(", ", _m.Aanwezigen), null, true, false));
                    r.RelativeItem().Element(x => Cel(x, "Verontschuldigd", _m.Verontschuldigd.Count == 0 ? "—" : string.Join(", ", _m.Verontschuldigd), null, true, false));
                    r.RelativeItem().Element(x => Cel(x, "Volgend bezoek",
                        _m.VolgendBezoek.HasValue ? _m.VolgendBezoek.Value.ToString("dddd dd/MM/yyyy", WerfverslagStijl.Nl) + " · " + _m.VolgendBezoek.Value.ToString("HH:mm") : "—", null, false, false));
                });
            });
        }

        private void Samenvatting(IContainer c)
        {
            void Stat(IContainer x, int n, string label, bool rood = false)
            {
                x.Border(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).PaddingHorizontal(3, Unit.Millimetre).PaddingVertical(2.2f, Unit.Millimetre)
                    .Text(t =>
                    {
                        t.Span(n.ToString()).FontFamily(BodyFont).FontSize(12f).Bold().FontColor(rood && n > 0 ? WerfverslagStijl.Rood : WerfverslagStijl.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                        t.Span("  " + label).FontFamily(BodyFont).FontSize(6.8f).FontColor(WerfverslagStijl.Gedempt);
                    });
            }
            c.ShowEntire().Column(col =>
            {
                col.Spacing(3.6f, Unit.Millimetre);
                col.Item().Element(x => WerfverslagStijl.Sectie(x, "Samenvatting", BodyFont));
                col.Item().Row(r =>
                {
                    r.Spacing(3, Unit.Millimetre);
                    r.RelativeItem().Element(x => Stat(x, _m.Nieuwe.Count, "Nieuw"));
                    r.RelativeItem().Element(x => Stat(x, _m.Open.Count, "Open uit vorig verslag"));
                    r.RelativeItem().Element(x => Stat(x, _m.Afgesloten.Count, "Afgesloten sinds vorig"));
                    r.RelativeItem().Element(x => Stat(x, _m.Kritiek, "Kritiek", rood: true));
                });
            });
        }

        // ── 35k: een nieuw punt als kaart ──
        private void NieuwPunt(IContainer c, WerfverslagPdfNieuw p)
        {
            c.Border(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).Column(k =>
            {
                // kop: bol · nummer · titel · eenheid · aannemer · status
                k.Item().Background(WerfverslagStijl.VlakGroen).BorderBottom(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand)
                    .PaddingHorizontal(3, Unit.Millimetre).PaddingVertical(2.2f, Unit.Millimetre).Row(r =>
                    {
                        r.Spacing(3, Unit.Millimetre);
                        r.ConstantItem(5, Unit.Millimetre).AlignMiddle().Element(x => WerfverslagStijl.Bol(x, p.Num.ToString(), p.Kritiek, BodyFont));
                        r.ConstantItem(15, Unit.Millimetre).AlignMiddle().Text(p.Nr).FontFamily(BodyFont).FontSize(7f).Bold().FontColor(WerfverslagStijl.Groen).EnableFontFeature(FontFeatures.TabularFigures);
                        r.RelativeItem().AlignMiddle().Text(p.Titel).FontFamily(BodyFont).FontSize(8.3f).Bold().FontColor(WerfverslagStijl.Inkt);
                        r.ConstantItem(28, Unit.Millimetre).AlignMiddle().Text(p.Eenheid).FontFamily(BodyFont).FontSize(7f).FontColor(WerfverslagStijl.Gedempt);
                        r.ConstantItem(34, Unit.Millimetre).AlignMiddle().Text(p.Aannemer).FontFamily(BodyFont).FontSize(7f).SemiBold().FontColor(WerfverslagStijl.Inkt);
                        r.AutoItem().AlignMiddle().Element(x => WerfverslagStijl.Pil(x, p.Kritiek ? "Kritiek" : p.Status, BodyFont));
                    });
                // inhoud: tekst + deadline/plan links, foto's rechts (niets te tonen = geen lege balk)
                if (!string.IsNullOrWhiteSpace(p.Beschrijving) || !string.IsNullOrWhiteSpace(p.Deadline) || !string.IsNullOrWhiteSpace(p.Plan) || p.Fotos.Count > 0)
                k.Item().PaddingHorizontal(3, Unit.Millimetre).PaddingVertical(2.6f, Unit.Millimetre).Row(r =>
                {
                    r.Spacing(3.5f, Unit.Millimetre);
                    r.RelativeItem().Column(t =>
                    {
                        t.Spacing(1.4f, Unit.Millimetre);
                        if (!string.IsNullOrWhiteSpace(p.Beschrijving))
                            t.Item().Text(p.Beschrijving!.Trim()).FontFamily(BodyFont).FontSize(7.8f).LineHeight(1.5f).FontColor(WerfverslagStijl.Inkt);
                        t.Item().Text(x =>
                        {
                            x.DefaultTextStyle(s => s.FontFamily(BodyFont).FontSize(6.5f).FontColor(WerfverslagStijl.Gedempt));
                            if (!string.IsNullOrWhiteSpace(p.Deadline))
                            {
                                x.Span("Uit te voeren tegen ");
                                x.Span(p.Deadline!).Bold().FontColor(p.Kritiek ? WerfverslagStijl.Rood : WerfverslagStijl.Inkt);
                            }
                            if (!string.IsNullOrWhiteSpace(p.Plan))
                            {
                                x.Span((string.IsNullOrWhiteSpace(p.Deadline) ? "" : "        ") + "Plan: ");
                                x.Span(p.Plan!).Bold().FontColor(WerfverslagStijl.Inkt);
                            }
                        });
                    });
                    foreach (var foto in p.Fotos.Take(3))
                        r.ConstantItem(30, Unit.Millimetre).Height(22, Unit.Millimetre).Border(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).Image(foto).FitArea();
                });
            });
        }

        // ── 35l: tabel met groene kop, rijen met een fijne lijn, statuspillen ──
        private void OpenTabel(IContainer c, IReadOnlyList<WerfverslagPdfOpen> punten)
        {
            void Kolommen(IContainer x, Action<IContainer, int> cel)
            {
                x.Row(r =>
                {
                    r.Spacing(3, Unit.Millimetre);
                    r.ConstantItem(15, Unit.Millimetre).Element(e => cel(e, 0));
                    r.RelativeItem().Element(e => cel(e, 1));
                    r.ConstantItem(22, Unit.Millimetre).Element(e => cel(e, 2));
                    r.ConstantItem(30, Unit.Millimetre).Element(e => cel(e, 3));
                    r.ConstantItem(26, Unit.Millimetre).Element(e => cel(e, 4));
                    r.ConstantItem(26, Unit.Millimetre).Element(e => cel(e, 5));
                });
            }
            var koppen = new[] { "NR.", "PUNT", "EENHEID", "AANNEMER", "VORIG VERSLAG", "NU" };
            c.Column(col =>
            {
                col.Item().Background(WerfverslagStijl.Groen).PaddingHorizontal(3, Unit.Millimetre).PaddingVertical(2, Unit.Millimetre)
                    .Element(x => Kolommen(x, (e, i) => e.Text(koppen[i]).FontFamily(BodyFont).FontSize(5.8f).Bold().LetterSpacing(0.08f).FontColor("#FFFFFF")));
                foreach (var p in punten)
                    col.Item().ShowEntire().BorderBottom(0.2f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).PaddingHorizontal(3, Unit.Millimetre).PaddingVertical(2, Unit.Millimetre)
                        .Element(x => Kolommen(x, (e, i) =>
                        {
                            switch (i)
                            {
                                case 0: e.Text(p.Nr).FontFamily(BodyFont).FontSize(6.8f).Bold().FontColor(WerfverslagStijl.Groen).EnableFontFeature(FontFeatures.TabularFigures); break;
                                case 1:
                                    e.Column(t =>
                                    {
                                        t.Spacing(0.5f, Unit.Millimetre);
                                        t.Item().Text(p.Titel).FontFamily(BodyFont).FontSize(7.3f).Bold().LineHeight(1.4f).FontColor(WerfverslagStijl.Inkt);
                                        if (!string.IsNullOrWhiteSpace(p.Notitie)) t.Item().Text(p.Notitie).FontFamily(BodyFont).FontSize(6.5f).LineHeight(1.4f).FontColor(WerfverslagStijl.Gedempt);
                                    });
                                    break;
                                case 2: e.Text(p.Eenheid).FontFamily(BodyFont).FontSize(7.3f).FontColor(WerfverslagStijl.Gedempt); break;
                                case 3: e.Text(p.Aannemer).FontFamily(BodyFont).FontSize(7.3f).FontColor(WerfverslagStijl.Inkt); break;
                                case 4: e.AlignLeft().AlignTop().Element(z => WerfverslagStijl.Pil(z, p.Vorig, BodyFont)); break;
                                default: e.AlignLeft().AlignTop().Element(z => WerfverslagStijl.Pil(z, p.Nu, BodyFont)); break;
                            }
                        }));
            });
        }

        private static readonly Regex Prefix = new(@"^\s*([^—:\-]{2,28}?)\s[—:\-]\s+(.+)$", RegexOptions.Compiled);

        private void Opmerkingen(IContainer c)
        {
            // regels als "Planning — …" krijgen de rubriek vet, zoals in 35l
            c.Column(k =>
            {
                k.Spacing(1.6f, Unit.Millimetre);
                foreach (var regel in (_m.Opmerkingen ?? "").Split('\n').Select(r => r.Trim()).Where(r => r != ""))
                {
                    var m = Prefix.Match(regel);
                    k.Item().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontFamily(BodyFont).FontSize(7.8f).LineHeight(1.55f).FontColor(WerfverslagStijl.Inkt));
                        if (m.Success) { t.Span(m.Groups[1].Value).Bold(); t.Span(" — " + m.Groups[2].Value); }
                        else t.Span(regel);
                    });
                }
            });
        }

        private void Verdeling(IContainer c)
        {
            c.Column(k =>
            {
                k.Spacing(1.2f, Unit.Millimetre);
                foreach (var d in _m.Verdeling)
                    k.Item().Row(r =>
                    {
                        r.Spacing(4, Unit.Millimetre);
                        r.RelativeItem().Text(d.Naam).FontFamily(BodyFont).FontSize(7.3f).Bold().FontColor(WerfverslagStijl.Inkt);
                        r.RelativeItem().Text(d.Email ?? "—").FontFamily(BodyFont).FontSize(7.3f).FontColor(WerfverslagStijl.Groen);
                        r.ConstantItem(30, Unit.Millimetre).Text(d.Bereik).FontFamily(BodyFont).FontSize(7.3f).FontColor(WerfverslagStijl.Gedempt);
                    });
            });
        }
    }

    /// <summary>Bijlage plannen (35m): één pagina (A4 liggend) per plan met punten. Links het plan, rechts de punten met dezelfde nummers als in het verslag
    /// (groen = nieuw, rood = kritiek, goud "O" = open uit een vorig verslag). Elk plan is een eigen document zodat elke bladzijde de volledige kop (enkel
    /// het logo) heeft; de bladzijdenummers lopen door vanaf het verslag.</summary>
    public sealed class WerfverslagBijlageDocumentV2 : GlV2PdfDocumentBase
    {
        private readonly WerfverslagPdfModel _m;
        private readonly int _index;
        private readonly int _paginaOffset;
        private readonly int _totaal;

        public WerfverslagBijlageDocumentV2(WerfverslagPdfModel model, int planIndex, int paginaOffset, int totaalPaginas, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _index = planIndex;
            _paginaOffset = paginaOffset;
            _totaal = totaalPaginas;
        }

        public override DocumentMetadata GetMetadata() => new() { Title = $"{_m.Naam} — plannen", Author = Company.Name };

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Liggend;
        protected override string DocumentTitle => _m.Naam;
        protected override string? Kicker => "BIJLAGE · PLANNEN";
        protected override string? DocumentNumber => $"plan {_index + 1} van {_m.Plannen.Count}";
        protected override string? CompactSubtitle => _m.ProjectName;
        protected override int PageNumberOffset => _paginaOffset;
        protected override int? TotalPagesOverride => _totaal;

        protected override void Content(IContainer c)
        {
            var plan = _m.Plannen[_index];
            c.Row(row =>
            {
                row.Spacing(6, Unit.Millimetre);
                row.RelativeItem().Column(l =>
                {
                    l.Spacing(2, Unit.Millimetre);
                    l.Item().Text(t =>
                    {
                        t.Span(plan.Naam).FontFamily(BodyFont).FontSize(8.5f).Bold().FontColor(WerfverslagStijl.Inkt);
                        t.Span("   " + plan.Sub).FontFamily(BodyFont).FontSize(6.5f).FontColor(WerfverslagStijl.Gedempt);
                    });
                    l.Item().Height(108, Unit.Millimetre).Border(0.25f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).Background("#F5F7F3")
                        .AlignCenter().AlignMiddle().Image(plan.Image).FitArea();
                });
                row.ConstantItem(64, Unit.Millimetre).Column(k =>
                {
                    k.Spacing(2, Unit.Millimetre);
                    k.Item().PaddingTop(1.5f, Unit.Millimetre).Element(x => WerfverslagStijl.Sectie(x, "Punten op dit plan", BodyFont));
                    foreach (var p in plan.Punten)
                        k.Item().ShowEntire().BorderBottom(0.2f, Unit.Millimetre).BorderColor(WerfverslagStijl.Rand).PaddingVertical(1.6f, Unit.Millimetre).Row(pr =>
                        {
                            pr.Spacing(2.4f, Unit.Millimetre);
                            pr.ConstantItem(4.6f, Unit.Millimetre).Element(x => WerfverslagStijl.Bol(x, p.Label, p.Kritiek, BodyFont));
                            pr.RelativeItem().Column(t =>
                            {
                                t.Spacing(0.4f, Unit.Millimetre);
                                t.Item().Text(p.Nr).FontFamily(BodyFont).FontSize(6.5f).Bold().FontColor(WerfverslagStijl.Groen).EnableFontFeature(FontFeatures.TabularFigures);
                                t.Item().Text(p.Tekst).FontFamily(BodyFont).FontSize(7.3f).LineHeight(1.35f).FontColor(WerfverslagStijl.Inkt);
                            });
                        });
                    k.Item().PaddingTop(1, Unit.Millimetre).Text("Eén bladzijde per plan met punten. Plannen zonder punten worden niet afgedrukt. Groot plan → A3 liggend.")
                        .FontFamily(BodyFont).FontSize(6.3f).LineHeight(1.45f).FontColor(WerfverslagStijl.Gedempt);
                });
            });
        }
    }
}
