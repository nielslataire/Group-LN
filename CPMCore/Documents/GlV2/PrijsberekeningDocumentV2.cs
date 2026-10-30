using CPMCore.Services.Verkoop;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static CPMCore.Documents.GlV2.GlV2PdfComponents;

namespace CPMCore.Documents.GlV2;

/// <summary>Eén eenheid in de prijsberekening: de rij (met eventueel een eigen grond- en bouwbedrag uit de Berekenen-pagina) en wat het budget erover weet (enkel voor de interne versie).</summary>
public sealed record PrijsberekeningEenheid(VerkoopRijVm Rij, KoperBerekening Koper, VerkoopBudgetInfo? Budget);

/// <summary>Opties van de prijsberekening (design-handoff 45d): versie koper of intern, kosten koper, detail van de berekening en een overzichtspagina vooraan.</summary>
public sealed record PrijsberekeningOpties(bool Intern = false, bool KostenKoper = true, bool Detail = true, bool Overzicht = true);

/// <summary>Prijsberekening per eenheid (design-handoff 45e): A4 staand, één pagina per eenheid, met grond, constructie, kosten koper en het totaal voor de koper.
/// Rekent met <see cref="KoperBerekening"/>, dezelfde bedragen als de Te koop-pagina en de Berekenen-pagina.</summary>
public sealed class PrijsberekeningDocumentV2 : GlV2PdfDocumentBase
{
    private readonly string _project;
    private readonly IReadOnlyList<PrijsberekeningEenheid> _eenheden;
    private readonly PrijsberekeningOpties _opties;

    public PrijsberekeningDocumentV2(string project, IReadOnlyList<PrijsberekeningEenheid> eenheden, PrijsberekeningOpties opties, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
    {
        _project = project; _eenheden = eenheden; _opties = opties;
    }

    public override DocumentMetadata GetMetadata() => new() { Title = $"Prijsberekening — {_project}", Author = Company.Name };
    protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
    protected override string DocumentTitle => "Prijsberekening";
    protected override string? Kicker => _opties.Intern ? "INTERN · NIET VOOR DE KOPER" : null;
    protected override string? DocumentNumber => _project;
    protected override string? CompactNote => (_eenheden.Count == 1 ? $"{_eenheden[0].Rij.TypeNaam} {_eenheden[0].Rij.Naam}".Trim() : $"{_eenheden.Count} eenheden") + " · opgemaakt " + DateTime.Today.ToString("dd/MM/yyyy");
    protected override string? CompactSubtitle => _project;

    /// <summary>Rijen staan dicht op elkaar: met de notaris opgesplitst moet de eenheid met logo en voorbehoud op Ã©Ã©n pagina blijven.</summary>
    private bool _dicht;
    private static string E(decimal v) => v.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("nl-BE"));
    private static string M2(decimal v) => v.ToString("#,##0.##", System.Globalization.CultureInfo.GetCultureInfo("nl-BE")) + " m²";

    protected override void Content(IContainer c)
    {
        c.Column(col =>
        {
            var overzicht = _opties.Overzicht && _eenheden.Count > 1;
            if (overzicht) { col.Item().Element(Overzicht); col.Item().PageBreak(); }
            for (var i = 0; i < _eenheden.Count; i++)
            {
                if (i > 0) col.Item().PageBreak();
                var e = _eenheden[i];
                col.Item().Element(x => Eenheid(x, e));
            }
        });
    }

    private void Overzicht(IContainer c)
    {
        var kolommen = new List<TabelKolom> { new("Eenheid", 26), new("Status", 24), new("Grond", 28, true), new("Bouw excl. btw", 30, true), new("Vraagprijs", 28, true), new("Totaal koper") { Rechts = true } };
        var rijen = _eenheden.Select(e => (IReadOnlyList<TabelCel>)new List<TabelCel>
        {
            new(e.Rij.Naam), new(e.Rij.Status), new(E(e.Koper.Grond)), new(E(e.Koper.Bouw)), new(E(e.Koper.Vraagprijs)), new(E(e.Koper.Totaal)),
        }).ToList();
        c.Column(col =>
        {
            col.Spacing(4, Unit.Millimetre);
            col.Item().Element(x => Fiche(x, BodyFont, new List<FicheVeld> { new("Project", _project, $"{_eenheden.Count} eenheden in deze berekening") }, 1));
            col.Item().Element(x => SectieKop(x, BodyFont, "Overzicht"));
            col.Item().Element(x => Tabel(x, BodyFont, kolommen, rijen));
            col.Item().Text("Bedragen volgens de berekening per eenheid op de volgende pagina's.").FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt);
        });
    }

    private void Eenheid(IContainer c, PrijsberekeningEenheid e)
    {
        var r = e.Rij; var k = e.Koper;
        c.Column(col =>
        {
            col.Spacing(3f, Unit.Millimetre);
            col.Item().Element(x => Fiche(x, BodyFont, new List<FicheVeld>
            {
                new("Eenheid", $"{r.TypeNaam} {r.Naam}".Trim(), r.Verdieping),
                new("Perceel", r.Perceel > 0m ? M2(r.Perceel.Value) : "—", string.IsNullOrWhiteSpace(r.PreKad) ? null : "prekad " + r.PreKad),
                new("Woonoppervlakte", r.Wonen > 0m ? M2(r.Wonen.Value) : "—", r.Slaapkamers > 0 ? r.Slaapkamers + (r.Slaapkamers == 1 ? " slaapkamer" : " slaapkamers") : null),
                new("Status", r.Status),
            }));

            // GROND
            col.Item().Element(x => SectieKop(x, BodyFont, "Grond"));
            col.Item().Column(g =>
            {
                g.Item().Element(x => Regel(x, "Grondwaarde", _opties.Detail && r.Perceel > 0m ? $"{M2(r.Perceel!.Value)} × {E(Math.Round(k.Grond / r.Perceel.Value, 2))}" : null, E(k.Grond)));
                if (k.Regime == VerkoopRegime.Gemengd && k.Registratierechten > 0m) g.Item().Element(x => Regel(x, $"Registratierechten {k.RegistratiePercentage:0.##} %", _opties.Detail ? "op de grondwaarde" : null, E(k.Registratierechten)));
                g.Item().Element(x => Regel(x, "Subtotaal grond", null, E(k.Grond + (k.Regime == VerkoopRegime.Gemengd ? k.Registratierechten : 0m)), subtotaal: true));
            });

            // CONSTRUCTIE
            col.Item().Element(x => SectieKop(x, BodyFont, "Constructie"));
            col.Item().Column(g =>
            {
                string? bouwDetail = null;
                if (_opties.Detail && e.Budget is { OppGereduceerd: > 0m } b) bouwDetail = $"{M2(b.OppGereduceerd)} gered. × {E(Math.Round(k.Bouw / b.OppGereduceerd, 0))}";
                g.Item().Element(x => Regel(x, "Bouwwaarde excl. btw", bouwDetail, E(k.Bouw)));
                var opWat = k.Regime == VerkoopRegime.Gemengd ? "op de bouwwaarde" : "op grond en bouw";
                if (k.Btw > 0m) g.Item().Element(x => Regel(x, $"Btw {k.BtwPercentage:0.##} %", _opties.Detail ? opWat : null, E(k.Btw)));
                if (k.Regime == VerkoopRegime.Registratie && k.Registratierechten > 0m) g.Item().Element(x => Regel(x, $"Registratierechten {k.RegistratiePercentage:0.##} %", _opties.Detail ? opWat : null, E(k.Registratierechten)));
                g.Item().Element(x => Regel(x, "Subtotaal constructie", null, E(k.Bouw + k.Btw + (k.Regime == VerkoopRegime.Registratie ? k.Registratierechten : 0m)), subtotaal: true));
            });

            // KOSTEN KOPER
            if (_opties.KostenKoper)
            {
                _dicht = true;
                col.Item().Element(x => SectieKop(x, BodyFont, "Kosten notaris + koper"));
                col.Item().Column(g =>
                {
                    foreach (var p in k.Posten) g.Item().Element(x => Regel(x, p.Naam == "Aansluitkosten" ? "Raming aansluitkosten" : p.Naam, _opties.Detail ? p.Toelichting : null, E(p.Bedrag)));
                    if (k.KostenBtw > 0m) g.Item().Element(x => Regel(x, "Btw op de kosten", null, E(k.KostenBtw)));
                    var n = k.Notaris;
                    if (n.Totaal > 0m)
                    {
                        g.Item().Element(x => Regel(x, "Ereloon notaris", _opties.Detail ? $"schaal {n.Schaal} op {E(n.Grondslag)}" : null, E(n.Ereloon)));
                        g.Item().Element(x => Regel(x, "Administratieve kosten", _opties.Detail ? "vacaties (art. 2 §2)" : null, E(n.Vacaties)));
                        g.Item().Element(x => Regel(x, "Uitgaven aan derden", null, E(n.Uitgaven)));
                        if (n.Overschrijving > 0m) g.Item().Element(x => Regel(x, "Kosten overschrijving", _opties.Detail ? "hypotheekkantoor" : null, E(n.Overschrijving)));
                        g.Item().Element(x => Regel(x, "Recht op geschriften", null, E(n.RechtOpGeschriften)));
                        g.Item().Element(x => Regel(x, "Registratierecht op bijlagen", null, E(n.RegistratieBijlagen)));
                        g.Item().Element(x => Regel(x, $"Btw {NotarisTarief.Btw:0.##} % op de notariskosten", _opties.Detail ? "op ereloon en kosten" : null, E(n.Btw)));
                    }
                    g.Item().Element(x => Regel(x, "Subtotaal kosten notaris + koper", null, E(k.KostenKoper + k.KostenBtw + k.Notaris.Totaal), subtotaal: true));
                });
                _dicht = false;
            }

            // TOTAAL
            // 45e: rechts uitgelijnd blok van 92 mm — vraagprijs in gewone tekst, daaronder het totaal op een donkergroen vlak.
            col.Item().PaddingTop(2, Unit.Millimetre).AlignRight().Width(92, Unit.Millimetre).Column(t =>
            {
                t.Item().PaddingHorizontal(3, Unit.Millimetre).PaddingVertical(1.6f, Unit.Millimetre).Row(x =>
                {
                    x.RelativeItem().Text("Vraagprijs (grond + bouw excl. btw)").FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt);
                    x.AutoItem().Text(E(k.Vraagprijs)).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                });
                t.Item().Background(GlV2PdfTheme.Groen).Padding(3, Unit.Millimetre).Row(x =>
                {
                    x.RelativeItem().AlignMiddle().Text(_opties.KostenKoper ? "Totaal voor de koper" : "Totaal voor de koper (zonder kosten koper)").FontFamily(BodyFont).FontSize(9).Bold().FontColor(GlV2PdfTheme.Wit);
                    x.AutoItem().AlignMiddle().Text(E(_opties.KostenKoper ? k.Totaal : k.Totaal - k.KostenKoper - k.KostenBtw - k.Notaris.Totaal)).FontFamily(BodyFont).FontSize(12).Bold().FontColor(GlV2PdfTheme.Wit).EnableFontFeature(FontFeatures.TabularFigures);
                });
            });

            // Voorbehoud onderaan de pagina op een lichtgroen vlak (45e).
            col.Item().ExtendVertical().AlignBottom().PaddingBottom(4, Unit.Millimetre).Background(GlV2PdfTheme.VlakGroen).CornerRadius(1.5f, Unit.Millimetre)
                .PaddingVertical(3, Unit.Millimetre).PaddingHorizontal(3.5f, Unit.Millimetre)
                .Text(Voetnoot(k)).FontFamily(BodyFont).FontSize(6.8f).LineHeight(1.5f).FontColor(GlV2PdfTheme.Gedempt);

            // De interne versie krijgt het budgetblok op een eigen pagina erachter, zodat het nooit over twee pagina's breekt.
            if (_opties.Intern) { col.Item().PageBreak(); col.Item().Element(x => Intern(x, e)); }
        });
    }

    /// <summary>Enkel in de interne versie: wat het definitieve budget (stap 8 en 9) over de eenheid weet, naast de vraagprijs.</summary>
    private void Intern(IContainer c, PrijsberekeningEenheid e)
    {
        var b = e.Budget; var vraag = e.Koper.Vraagprijs;
        c.Column(col =>
        {
            col.Spacing(1, Unit.Millimetre);
            col.Item().Element(x => SectieKop(x, BodyFont, "Intern", null, "#8A6A32"));
            if (b == null)
            {
                col.Item().Text("Deze eenheid is niet gekoppeld aan een verkooplijn van het definitieve budget: er zijn geen kostprijs en minimum bekend.").FontFamily(BodyFont).FontSize(7.5f).FontColor(GlV2PdfTheme.Gedempt);
                return;
            }
            col.Item().Element(x => Regel(x, "Kostprijs", "volgens het definitieve budget", E(b.Kostprijs)));
            col.Item().Element(x => Regel(x, "Minimumverkoopprijs", $"doelmarge {b.MargePerc * 100m:0.#} % op de bouw", E(b.Minimum)));
            col.Item().Element(x => Regel(x, "Resultaat op de vraagprijs", "vraagprijs min kostprijs", E(vraag - b.Kostprijs)));
            if (b.Kostprijs > 0m) col.Item().Element(x => Regel(x, "Marge op kostprijs", null, ((vraag - b.Kostprijs) / b.Kostprijs * 100m).ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("nl-BE")) + " %"));
            col.Item().Element(x => Regel(x, "Ruimte boven het minimum", "vraagprijs min minimumverkoopprijs", E(vraag - b.Minimum)));
            if (b.Commissie > 0m) col.Item().Element(x => Regel(x, $"Makelaarscommissie {b.Commissie * 100m:0.##} %", "op de vraagprijs", E(Math.Round(vraag * b.Commissie, 2))));
        });
    }

    private static string Voetnoot(KoperBerekening k)
    {
        var s = new List<string>();
        if (k.Registratierechten > 0m) s.Add("Registratierechten volgens het tarief in Vlaanderen; een verlaagd tarief is mogelijk bij enige eigen woning.");
        if (k.Btw > 0m) s.Add("Btw volgens het geldende tarief op datum van facturatie.");
        s.Add("De notariskosten zijn een raming volgens het tarief van de notarissen. Deze berekening is informatief en niet bindend.");
        return string.Join(" ", s);
    }

    /// <summary>Eén regel: label (met kleine subregel) links, bedrag rechts; een subtotaal krijgt een lijn erboven en vet bedrag.</summary>
    private void Regel(IContainer c, string label, string? sub, string bedrag, bool subtotaal = false)
    {
        // 45e: drie kolommen (omschrijving | toelichting 46 mm | bedrag 30 mm, 4 mm ertussen), rij 1,8 mm hoog met hairline eronder;
        // een subtotaal heeft het lichtgroene groepsvlak, 2,2 mm padding en vette tekst.
        var cont = c.Background(subtotaal ? GlV2PdfTheme.VlakGroepsrij : GlV2PdfTheme.Wit)
            .BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
            .PaddingVertical(subtotaal ? 2f : 1.2f, Unit.Millimetre).PaddingHorizontal(3, Unit.Millimetre);
        cont.Row(r =>
        {
            r.Spacing(4, Unit.Millimetre);
            var t = r.RelativeItem().Text(label).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt);
            if (subtotaal) t.Bold();
            r.ConstantItem(46, Unit.Millimetre).Text(sub ?? "").FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt);
            var b = r.ConstantItem(30, Unit.Millimetre).AlignRight().Text(bedrag).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
            if (subtotaal) b.Bold();
        });
    }
}
