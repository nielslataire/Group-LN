using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static CPMCore.Documents.GlV2.GlV2PdfComponents;

namespace CPMCore.Documents.GlV2
{
    /// <summary>Eén post van een tabel: omschrijving, toelichting/basis en bedrag.</summary>
    public sealed record BudgetPost(string Post, string Basis, decimal Bedrag);

    /// <summary>Eén activiteit in "bouwkosten per activiteit".</summary>
    public sealed record BudgetActiviteitRij(string Naam, string Basis, decimal Bedrag);

    /// <summary>Een deel (lot) met zijn activiteiten en subtotaal.</summary>
    public sealed record BudgetDeel(string Titel, IReadOnlyList<BudgetActiviteitRij> Lijnen, decimal Subtotaal);

    public sealed record BudgetGrondAankoopRij(int Nr, string Omschrijving, string Datum, decimal Bedrag);

    public sealed record BudgetVerkoopRij(string Lot, string Type, decimal Grond, decimal Bouw, decimal Vraagprijs, decimal PerM2);

    public sealed record BudgetResultaatRij(string Lot, decimal Verkoop, decimal Verkoopkosten, decimal Kostprijs, decimal Resultaat, decimal Marge);

    /// <summary>Eén rij van de oppervlaktetabel (stap 2); kolommen zonder enige waarde worden niet getoond.</summary>
    public sealed class BudgetOppRij
    {
        public int Nr { get; set; }
        public string Naam { get; set; } = "";
        public string Hoofdtype { get; set; } = "";
        public string Subtype { get; set; } = "";
        public decimal Bewoonbaar { get; set; }
        public decimal Tuin { get; set; }
        public decimal TerrasPrefab { get; set; }
        public decimal TerrasGelijkvloers { get; set; }
        public decimal Dakterras { get; set; }
        public decimal GarageBovengronds { get; set; }
        public decimal BergingOndergronds { get; set; }
        public decimal BergingGelijkvloers { get; set; }
        public decimal Carport { get; set; }
        public decimal Poorten { get; set; }
        public decimal DoorritGvl { get; set; }
        public decimal Zolder { get; set; }
        public decimal GemeenschappelijkeDelen { get; set; }
        public decimal Wegenis { get; set; }
        public decimal Grond { get; set; }
        public decimal Grond2 { get; set; }
        /// <summary>Grond + grond 2; enkel gevuld in de interne versie (de bank ziet één samengetelde grondkolom).</summary>
        public decimal GrondTotaal { get; set; }
        public decimal Gereduceerd { get; set; }
    }

    /// <summary>Alles wat het budgetdocument (35q intern, 35r bank) nodig heeft; gevuld door <c>BudgetPdfBuilder</c>.</summary>
    public sealed class BudgetPdfModel
    {
        public bool Intern { get; set; }
        public string ProjectNaam { get; set; } = "";
        public string VersieKort { get; set; } = "";          // "v2"
        public string DatumTekst { get; set; } = "";          // "13/10/2026"
        public string Ondertitel { get; set; } = "";           // regel onder de projectnaam in de kop
        public string VoetMidden { get; set; } = "";           // "Budget intern · v2 · 13/10/2026"

        // Projectfiche
        public IReadOnlyList<FicheVeld> Fiche { get; set; } = Array.Empty<FicheVeld>();

        // Samenvatting
        public int AantalEenheden { get; set; }
        public string KostprijsToelichting { get; set; } = "";
        public decimal Kostprijs { get; set; }
        public decimal Verkoopwaarde { get; set; }
        public decimal Verkoopkosten { get; set; }
        public decimal Resultaat { get; set; }
        public decimal MargeOpKost { get; set; }
        public decimal MargeOpVerkoop { get; set; }
        public decimal ResultaatPerEenheid { get; set; }
        public decimal BreakEven { get; set; }
        public bool HeeftVerkoop { get; set; }
        public string Aandachtstekst { get; set; } = "";

        public IReadOnlyList<BudgetPost> Kostenoverzicht { get; set; } = Array.Empty<BudgetPost>();

        // Bouwkosten
        public string IndexPeildatum { get; set; } = "";
        public IReadOnlyList<BudgetDeel> Delen { get; set; } = Array.Empty<BudgetDeel>();
        public decimal TotaalBouw { get; set; }
        public decimal TotaalGba { get; set; }

        // Erelonen, verzekeringen, nutsvoorzieningen
        public IReadOnlyList<BudgetPost> Erelonen { get; set; } = Array.Empty<BudgetPost>();
        public IReadOnlyList<BudgetPost> Verzekeringen { get; set; } = Array.Empty<BudgetPost>();
        public IReadOnlyList<BudgetPost> Overige { get; set; } = Array.Empty<BudgetPost>();   // aansluitingen, liften, ...

        // Grondkost
        public IReadOnlyList<BudgetGrondAankoopRij> Aankopen { get; set; } = Array.Empty<BudgetGrondAankoopRij>();
        public IReadOnlyList<BudgetPost> Grondposten { get; set; } = Array.Empty<BudgetPost>();
        public decimal TotaalGrond { get; set; }
        public string GrondPerEenheid { get; set; } = "";

        // Alleen intern
        public IReadOnlyList<BudgetPost> Financiering { get; set; } = Array.Empty<BudgetPost>();
        public IReadOnlyList<BudgetPost> Reserves { get; set; } = Array.Empty<BudgetPost>();
        public IReadOnlyList<BudgetVerkoopRij> Verkooptabel { get; set; } = Array.Empty<BudgetVerkoopRij>();
        public IReadOnlyList<BudgetResultaatRij> ResultaatPerLot { get; set; } = Array.Empty<BudgetResultaatRij>();

        public IReadOnlyList<BudgetPost> VerkoopkostenPosten { get; set; } = Array.Empty<BudgetPost>();

        // Oppervlaktes
        public IReadOnlyList<KpiTegel> OppTegels { get; set; } = Array.Empty<KpiTegel>();
        public IReadOnlyList<BudgetOppRij> Oppervlaktes { get; set; } = Array.Empty<BudgetOppRij>();
        public string OppNoot { get; set; } = "";
    }

    /// <summary>
    /// Budget in de gl-v2-huisstijl, twee varianten uit dezelfde klasse: intern (design-handoff 35q, rode kicker "Intern — niet verspreiden",
    /// 4 pagina's A4 staand + oppervlaktes A4 liggend) en bank (35r, groene kicker "Versie voor de bank", 3 + 1: zonder financiering, onvoorzien,
    /// publiciteit en verkoop per eenheid). Kop, voet en bouwstenen komen uit <see cref="GlV2PdfDocumentBase"/> en <see cref="GlV2PdfComponents"/>.
    /// </summary>
    public sealed class BudgetDocumentV2 : GlV2PdfDocumentBase
    {
        private static readonly System.Globalization.CultureInfo NlBe = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");
        private const string Rood = "#8B2A2A";
        private readonly BudgetPdfModel _m;

        public BudgetDocumentV2(BudgetPdfModel model, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override DocumentMetadata GetMetadata() => new()
        {
            Title = $"Budget {_m.VersieKort} - {_m.ProjectNaam}{(_m.Intern ? " (intern)" : "")}",
            Author = Company.Name,
            Subject = _m.Ondertitel,
        };

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
        protected override string DocumentTitle => "Budget";
        protected override string? CompactSubtitle => _m.ProjectNaam;
        protected override bool FullHeaderOnEveryPage => true;   // op elke pagina dezelfde kop als pagina 1 (Niels, 14/10/2026)
        // Volledige kop zoals de wijzigingsopdracht (groot logo, geen naam) op pagina 1; compacte kop erna. Projectnaam onder de titel.
        protected override string? Kicker => _m.Intern ? "Intern — niet verspreiden" : null;
        protected override string? CompactKicker => Kicker;
        protected override string? CompactKickerKleur => Rood;
        protected override string? DocumentNumber => _m.ProjectNaam;
        protected override string? CompactNote => _m.Ondertitel;

        // ── Formattering (36 · 7 Regels: "6 %" met spatie, bedragen nl-BE) ───────────────────────────
        private static string E(decimal v) => (v < 0 ? "− € " : "€ ") + Math.Abs(v).ToString("N0", NlBe);
        private static string E2(decimal v) => (v < 0 ? "− € " : "€ ") + Math.Abs(v).ToString("N2", NlBe);
        private static string Min(decimal v) => "− € " + Math.Abs(v).ToString("N0", NlBe);
        private static string P1(decimal fractie) => (fractie * 100m).ToString("0.0", NlBe) + " %";
        private static string M2(decimal v) => v.ToString("N2", NlBe);

        /// <summary>Eerste pagina-reeks (A4 staand) + de oppervlaktetabel op een eigen A4-liggende pagina.</summary>
        public override void Compose(IDocumentContainer container)
        {
            ComposePage(container, GlV2PdfFormat.A4Staand, Content);
            ComposePage(container, GlV2PdfFormat.A4Liggend, Oppervlaktes, compactVanafEerste: true);
        }

        protected override void Footer(IContainer c)
        {
            c.BorderTop(0.3f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).PaddingTop(3, Unit.Millimetre).Row(row =>
            {
                var adres = string.Join(" · ", new[] { Company.Name, string.Join(", ", new[] { Company.Street, Company.PostalCity }.Where(s => !string.IsNullOrWhiteSpace(s))), Company.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
                row.RelativeItem().Text(adres).FontFamily(BodyFont).FontSize(6.5f).LineHeight(1.45f).FontColor(GlV2PdfTheme.Gedempt);
                row.AutoItem().PaddingHorizontal(4, Unit.Millimetre).Text(_m.VoetMidden).FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt));
                    t.Span("Vertrouwelijk — enkel voor geadresseerde · ");
                    t.CurrentPageNumber().SemiBold().FontColor(GlV2PdfTheme.Inkt);
                    t.Span(" / ").SemiBold().FontColor(GlV2PdfTheme.Inkt);
                    t.TotalPages().SemiBold().FontColor(GlV2PdfTheme.Inkt);
                });
            });
        }

        // ── Pagina's A4 staand ───────────────────────────────────────────────────────────────────────
        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(3.5f, Unit.Millimetre);
                Pagina1(col);
                col.Item().PageBreak();
                Pagina2(col);
                col.Item().PageBreak();
                Pagina3(col);
                if (_m.Intern)
                {
                    col.Item().PageBreak();
                    Pagina4(col);
                }
            });
        }

        private void Pagina1(ColumnDescriptor col)
        {
            col.Item().Element(x => SectieKop(x, BodyFont, "Projectfiche"));
            col.Item().Element(x => Fiche(x, BodyFont, _m.Fiche));

            col.Item().Element(x => SectieKop(x, BodyFont, "Samenvatting"));
            col.Item().Element(x => KpiTegels(x, BodyFont, new[]
            {
                new KpiTegel("Totale kostprijs", E(_m.Kostprijs), _m.KostprijsToelichting),
                new KpiTegel("Verkoopwaarde", _m.HeeftVerkoop ? E(_m.Verkoopwaarde) : "—", _m.HeeftVerkoop ? $"{_m.AantalEenheden} eenheden{(_m.Intern ? " · p. {p:verkoop}" : "")}" : "geen vraagprijzen"),
                new KpiTegel("Resultaat", _m.HeeftVerkoop ? E(_m.Resultaat) : "—", _m.HeeftVerkoop ? $"na verkoopkosten {E(_m.Verkoopkosten)}" : null),
                new KpiTegel("Marge op kostprijs", _m.HeeftVerkoop ? P1(_m.MargeOpKost) : "—", _m.HeeftVerkoop && _m.Intern ? $"op verkoop {P1(_m.MargeOpVerkoop)}" : null, Donker: true),
            }));

            col.Item().Element(x => SectieKop(x, BodyFont, "Kostenoverzicht", "bedragen excl. btw"));
            col.Item().Element(x => BudgetTabel(x, BodyFont,
                new[] { new BKolom("Post", Gewicht: 1.3f), new BKolom("Basis", Gewicht: 1.5f, Gedempt: true), new BKolom("Bedrag", 30, Rechts: true), new BKolom("% kostprijs", 20, Rechts: true) },
                _m.Kostenoverzicht.Select(k => BRegel.Rij(k.Post, k.Basis, E(k.Bedrag), _m.Kostprijs > 0 ? P1(k.Bedrag / _m.Kostprijs) : "—"))
                    .Append(BRegel.Totaal("Totale kostprijs", "", E(_m.Kostprijs), "100,0 %")), 7.2f));

            col.Item().Element(x => SectieKop(x, BodyFont, "Resultaat", _m.Intern ? "per eenheid op p. {p:verkoop}" : "oppervlaktes op p. {p:opp}"));
            if (_m.HeeftVerkoop)
            {
                col.Item().Element(x => ResultaatStrook(x, BodyFont,
                    new[] { "Verkoopprijs", "− Verkoopkosten", "− Totale kostprijs", "= Resultaat", "Marge op kost" },
                    new[] { $"{_m.AantalEenheden} eenheden", "makelaar + overige", _m.Intern ? "alle kosten" : "", "", "" },
                    new BCel[]
                    {
                        new(E(_m.Verkoopwaarde)),
                        new(Min(_m.Verkoopkosten), Rood),
                        new(Min(_m.Kostprijs), Rood),
                        new(E(_m.Resultaat), _m.Resultaat >= 0 ? GlV2PdfTheme.Groen : Rood, true),
                        new(P1(_m.MargeOpKost), _m.Resultaat >= 0 ? GlV2PdfTheme.Groen : Rood, true),
                    }));
            }
            else
            {
                col.Item().Text("Nog geen vraagprijzen vastgelegd in stap 8: geen verkoopwaarde of resultaat.").FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt);
            }
            if (!string.IsNullOrWhiteSpace(_m.Aandachtstekst))
                col.Item().Element(x => Aandachtsblok(x, BodyFont, _m.Aandachtstekst, groen: !_m.Intern));
        }

        private void Pagina2(ColumnDescriptor col)
        {
            col.Item().Section("bouw").Element(x => SectieKop(x, BodyFont, "Bouwkosten per activiteit", string.IsNullOrWhiteSpace(_m.IndexPeildatum) ? null : $"geïndexeerd tegen peildatum {_m.IndexPeildatum}"));
            var regels = new List<BRegel>();
            foreach (var deel in _m.Delen)
            {
                regels.Add(BRegel.Groep(deel.Titel));
                foreach (var l in deel.Lijnen)
                    regels.Add(BRegel.Rij(l.Naam, l.Basis, E(l.Bedrag), PerM2(l.Bedrag)));
                regels.Add(BRegel.Subtotaal($"Subtotaal {deel.Titel.ToLowerInvariant()}", "", E(deel.Subtotaal), PerM2(deel.Subtotaal)));
            }
            regels.Add(BRegel.Totaal("Totaal bouwkost", "", E(_m.TotaalBouw), PerM2(_m.TotaalBouw)));
            col.Item().Element(x => BudgetTabel(x, BodyFont,
                new[] { new BKolom("Activiteit", Gewicht: 1.5f), new BKolom("Basis", Gewicht: 1f, Gedempt: true), new BKolom("Bedrag", 28, Rechts: true), new BKolom("€/m² GBA", 20, Rechts: true) },
                regels, 6.9f));

            col.Item().Section("erelonen").Element(x => SectieKop(x, BodyFont, "Erelonen, studies & verzekeringen"));
            var r2 = new List<BRegel>();
            r2.AddRange(_m.Erelonen.Select(p => BRegel.Rij(p.Post, p.Basis, E(p.Bedrag))));
            if (_m.Erelonen.Count > 0) r2.Add(BRegel.Subtotaal("Subtotaal erelonen", "", E(_m.Erelonen.Sum(p => p.Bedrag))));
            r2.AddRange(_m.Verzekeringen.Select(p => BRegel.Rij(p.Post, p.Basis, E(p.Bedrag))));
            if (_m.Verzekeringen.Count > 0) r2.Add(BRegel.Subtotaal("Subtotaal verzekeringen", "", E(_m.Verzekeringen.Sum(p => p.Bedrag))));
            r2.AddRange(_m.Overige.Select(p => BRegel.Rij(p.Post, p.Basis, E(p.Bedrag))));
            col.Item().Element(x => BudgetTabel(x, BodyFont,
                new[] { new BKolom("Post", Gewicht: 1.5f), new BKolom("Basis", Gewicht: 1.3f, Gedempt: true), new BKolom("Bedrag", 28, Rechts: true) }, r2, 6.9f));
        }

        private string PerM2(decimal bedrag) => _m.TotaalGba > 0 ? E(bedrag / _m.TotaalGba) : "—";

        private void Pagina3(ColumnDescriptor col)
        {
            col.Item().Section("grond").Element(x => SectieKop(x, BodyFont, "Grondkost — detailberekening"));
            var g = new List<BRegel>();
            foreach (var a in _m.Aankopen)
                g.Add(BRegel.Rij(a.Nr.ToString(NlBe), a.Omschrijving, a.Datum, "", E2(a.Bedrag)));
            foreach (var p in _m.Grondposten)
                g.Add(BRegel.Rij("", p.Post, "", p.Basis, E2(p.Bedrag)));
            g.Add(BRegel.Totaal("", "Totale grondkost", "", _m.GrondPerEenheid, E2(_m.TotaalGrond)));
            col.Item().Element(x => BudgetTabel(x, BodyFont,
                new[] { new BKolom("#", 6), new BKolom("Aankoop / post", Gewicht: 1.5f), new BKolom("Datum akte", 22), new BKolom("Basis", Gewicht: 1.2f, Gedempt: true), new BKolom("Bedrag", 28, Rechts: true) }, g, 6.9f));

            if (_m.Intern)
            {
                if (_m.Financiering.Count > 0)
                {
                    col.Item().Section("financiering").Element(x => SectieKop(x, BodyFont, "Financieringskosten"));
                    var f = _m.Financiering.Select(p => BRegel.Rij(p.Post, p.Basis, E(p.Bedrag))).ToList();
                    f.Add(BRegel.Totaal("Totaal financiering", "", E(_m.Financiering.Sum(p => p.Bedrag))));
                    col.Item().Element(x => BudgetTabel(x, BodyFont, new[] { new BKolom("Post", Gewicht: 1.5f), new BKolom("Berekening", Gewicht: 1.3f, Gedempt: true), new BKolom("Bedrag", 28, Rechts: true) }, f, 6.9f));
                }
                if (_m.Reserves.Count > 0)
                {
                    col.Item().Element(x => SectieKop(x, BodyFont, "Reserves & commercieel"));
                    col.Item().Element(x => BudgetTabel(x, BodyFont, new[] { new BKolom("Post", Gewicht: 1.5f), new BKolom("Basis", Gewicht: 1.3f, Gedempt: true), new BKolom("Bedrag", 28, Rechts: true) },
                        _m.Reserves.Select(p => BRegel.Rij(p.Post, p.Basis, E(p.Bedrag))), 6.9f));
                }
            }

            if (_m.VerkoopkostenPosten.Count > 0)
            {
                col.Item().Element(x => SectieKop(x, BodyFont, "Verkoopkosten"));
                var v = _m.VerkoopkostenPosten.Select(p => BRegel.Rij(p.Post, p.Basis, E(p.Bedrag))).ToList();
                v.Add(BRegel.Totaal("Totaal verkoopkosten", "", E(_m.VerkoopkostenPosten.Sum(p => p.Bedrag))));
                col.Item().Element(x => BudgetTabel(x, BodyFont, new[] { new BKolom("Post", Gewicht: 1.5f), new BKolom("Berekening", Gewicht: 1.5f, Gedempt: true), new BKolom("Bedrag", 28, Rechts: true) }, v, 6.9f));
            }
        }

        private void Pagina4(ColumnDescriptor col)
        {
            col.Item().Section("verkoop").Element(x => SectieKop(x, BodyFont, "Verkooptabel", "grond excl. registratierechten · bouw excl. btw"));
            var v = _m.Verkooptabel.Select(r => BRegel.Rij(r.Lot, r.Type, E(r.Grond), E(r.Bouw), E(r.Vraagprijs), E(r.PerM2))).ToList();
            v.Add(BRegel.Totaal("Totaal", "", E(_m.Verkooptabel.Sum(r => r.Grond)), E(_m.Verkooptabel.Sum(r => r.Bouw)), E(_m.Verkooptabel.Sum(r => r.Vraagprijs)), _m.TotaalGba > 0 ? E(_m.Verkooptabel.Sum(r => r.Vraagprijs) / _m.TotaalGba) : "—"));
            col.Item().Element(x => BudgetTabel(x, BodyFont,
                new[] { new BKolom("Lot", Gewicht: 1f), new BKolom("Type", Gewicht: 1.2f, Gedempt: true), new BKolom("Grondaandeel", 26, Rechts: true), new BKolom("Bouwaandeel", 26, Rechts: true), new BKolom("Vraagprijs", 26, Rechts: true), new BKolom("€/m² GBA", 20, Rechts: true) },
                v, 7f));

            col.Item().Element(x => SectieKop(x, BodyFont, "Resultaat per eenheid", "kostprijs verdeeld: grondkost volgens grondoppervlakte, rest volgens gereduceerde oppervlakte"));
            var r = _m.ResultaatPerLot.Select(l => BRegel.Rij(l.Lot, E(l.Verkoop), Min(l.Verkoopkosten), Min(l.Kostprijs), new BCel(E(l.Resultaat), l.Resultaat >= 0 ? GlV2PdfTheme.Groen : Rood, true), P1(l.Marge))).ToList();
            var tv = _m.ResultaatPerLot.Sum(l => l.Verkoop); var tk = _m.ResultaatPerLot.Sum(l => l.Verkoopkosten); var tp = _m.ResultaatPerLot.Sum(l => l.Kostprijs);
            r.Add(BRegel.Totaal("Totaal", E(tv), Min(tk), Min(tp), E(tv - tk - tp), tp > 0 ? P1((tv - tk - tp) / tp) : "—"));
            col.Item().Element(x => BudgetTabel(x, BodyFont,
                new[] { new BKolom("Lot", Gewicht: 1f), new BKolom("Verkoopprijs", 27, Rechts: true), new BKolom("− Verkoopkosten", 27, Rechts: true), new BKolom("− Totale kostprijs", 27, Rechts: true), new BKolom("= Resultaat", 27, Rechts: true), new BKolom("Marge op kost", 22, Rechts: true) },
                r, 7f));

            col.Item().Element(x => KpiTegels(x, BodyFont, new[]
            {
                new KpiTegel("Marge op kostprijs", P1(_m.MargeOpKost), "resultaat / kostprijs", Donker: true),
                new KpiTegel("Marge op verkoop", P1(_m.MargeOpVerkoop), "resultaat / verkoopwaarde"),
                new KpiTegel("Resultaat per eenheid", E(_m.ResultaatPerEenheid), $"{_m.AantalEenheden} eenheden"),
                new KpiTegel("Break-even", E(_m.BreakEven), "kostprijs + verkoopkosten"),
            }));
        }

        // ── Oppervlaktes (A4 liggend) ────────────────────────────────────────────────────────────────
        private sealed record OppKolom(string Kop, string Groep, Func<BudgetOppRij, decimal> Waarde, bool Geheel = false, bool AltijdTonen = false);

        private static readonly OppKolom[] OppKolommen =
        {
            new("Bew. m²", "Bew.", r => r.Bewoonbaar, AltijdTonen: true),
            new("Tuin", "Buitenruimtes", r => r.Tuin),
            new("T. prefab", "Buitenruimtes", r => r.TerrasPrefab),
            new("T. gvl.", "Buitenruimtes", r => r.TerrasGelijkvloers),
            new("Dakterras", "Buitenruimtes", r => r.Dakterras),
            new("Gar. bgr.", "Parkeren", r => r.GarageBovengronds),
            new("Berg. ond.", "Parkeren", r => r.BergingOndergronds),
            new("Berg. gvl.", "Parkeren", r => r.BergingGelijkvloers),
            new("Carport", "Parkeren", r => r.Carport),
            new("Poorten", "Parkeren", r => r.Poorten, Geheel: true),
            new("Doorrit", "Overige", r => r.DoorritGvl),
            new("Zolder", "Overige", r => r.Zolder),
            new("Gem. delen", "Overige", r => r.GemeenschappelijkeDelen),
            new("Wegenis", "Overige", r => r.Wegenis),
            new("Grond m²", "Grond", r => r.Grond, AltijdTonen: true),
            new("Grond 2", "Grond", r => r.Grond2),
            new("Totaal", "Grond", r => r.GrondTotaal),
        };

        private void Oppervlaktes(IContainer c)
        {
            var rijen = _m.Oppervlaktes;
            var kol = OppKolommen.Where(k => k.AltijdTonen || rijen.Any(r => k.Waarde(r) != 0m)).ToList();
            c.Section("opp").Column(col =>
            {
                col.Spacing(3.5f, Unit.Millimetre);
                col.Item().Element(x => SectieKop(x, BodyFont, "Samenvatting oppervlaktes", "m²"));
                col.Item().Element(x => KpiTegels(x, BodyFont, _m.OppTegels));
                col.Item().Element(x => SectieKop(x, BodyFont, "Oppervlaktetabel per eenheid", "kolommen zonder waarden worden weggelaten"));
                col.Item().Table(t =>
                {
                    const float Gap = 1.5f;
                    t.ColumnsDefinition(d =>
                    {
                        d.ConstantColumn(6 + Gap, Unit.Millimetre);
                        d.RelativeColumn(1.4f);
                        d.ConstantColumn(22 + Gap, Unit.Millimetre);
                        d.ConstantColumn(22 + Gap, Unit.Millimetre);
                        foreach (var _ in kol) d.ConstantColumn(13.5f + Gap, Unit.Millimetre);
                        d.ConstantColumn(15, Unit.Millimetre);
                    });

                    IContainer Kopcel(IContainer x) => x.Background(GlV2PdfTheme.Groen).PaddingVertical(1.2f, Unit.Millimetre).PaddingHorizontal(1, Unit.Millimetre).AlignBottom();
                    t.Header(h =>
                    {
                        // Rij 1: groepen
                        for (var i = 0; i < 4; i++) h.Cell().Element(Kopcel).Text("");
                        foreach (var groep in kol.GroupBy(k => k.Groep))
                            h.Cell().ColumnSpan((uint)groep.Count()).Element(Kopcel).AlignCenter().Text(groep.Key.ToUpperInvariant())
                                .FontFamily(BodyFont).FontSize(5.2f).Bold().LetterSpacing(0.04f).FontColor("#D6E5CC");
                        h.Cell().Element(Kopcel).Text("");
                        // Rij 2: kolomkoppen
                        void Kop(string tekst, bool rechts)
                        {
                            var tx = h.Cell().Element(Kopcel).Text(tekst.ToUpperInvariant()).FontFamily(BodyFont).FontSize(5.8f).Bold().LetterSpacing(0.06f).FontColor(GlV2PdfTheme.Wit);
                            if (rechts) tx.AlignRight();
                        }
                        Kop("#", false); Kop("Naam", false); Kop("Hoofdtype", false); Kop("Subtype", false);
                        foreach (var k in kol) Kop(k.Kop, true);
                        Kop("Gered. m²", true);
                    });

                    void Cel(string tekst, bool rechts = false, bool vet = false, string? kleur = null, bool totaal = false)
                    {
                        var x = t.Cell().ShowEntire().BorderBottom(totaal ? 0f : 0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn);
                        if (totaal) x = x.BorderTop(0.5f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Inkt);
                        var tx = x.PaddingVertical(1, Unit.Millimetre).PaddingHorizontal(1, Unit.Millimetre).Text(tekst).FontFamily(BodyFont).FontSize(6.6f)
                            .FontColor(kleur ?? GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                        if (vet) tx.Bold();
                        if (rechts) tx.AlignRight();
                    }
                    string F(OppKolom k, decimal v) => k.Geheel ? v.ToString("N0", NlBe) : M2(v);

                    foreach (var r in rijen)
                    {
                        Cel(r.Nr.ToString(NlBe), kleur: GlV2PdfTheme.Gedempt);
                        Cel(r.Naam, vet: true);
                        Cel(r.Hoofdtype, kleur: GlV2PdfTheme.Gedempt);
                        Cel(r.Subtype, kleur: GlV2PdfTheme.Gedempt);
                        foreach (var k in kol) Cel(k.Waarde(r) == 0m && !k.AltijdTonen ? "" : F(k, k.Waarde(r)), rechts: true);
                        Cel(M2(r.Gereduceerd), rechts: true, vet: true);
                    }

                    Cel("", totaal: true); Cel("Totaal", vet: true, totaal: true, kleur: GlV2PdfTheme.Groen); Cel("", totaal: true); Cel("", totaal: true);
                    foreach (var k in kol) Cel(F(k, rijen.Sum(r => k.Waarde(r))), rechts: true, vet: true, totaal: true, kleur: GlV2PdfTheme.Groen);
                    Cel(M2(rijen.Sum(r => r.Gereduceerd)), rechts: true, vet: true, totaal: true, kleur: GlV2PdfTheme.Groen);
                });
                if (!string.IsNullOrWhiteSpace(_m.OppNoot))
                    col.Item().Text(_m.OppNoot).FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
            });
        }
    }
}
