using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2;

/// <summary>Eén onderneming in de controlelijst. Situatie = "obligation", "noObligation", "notInDomain" of leeg; <c>Fout</c> is gevuld als de bevraging mislukte.</summary>
public sealed record InhoudingsplichtPdfRij(
    string? Naam, string? Nummer,
    string? SociaalSituatie, int? SociaalPercentage,
    string? FiscaalSituatie, int? FiscaalPercentage, long? FiscaalAttest,
    DateTime? GeldigTot, string? Referentie, string? TraceId, string? Fout)
{
    public bool Mislukt => !string.IsNullOrEmpty(Fout);
    public bool Inhouden => !Mislukt && (SociaalSituatie == "obligation" || FiscaalSituatie == "obligation");
}

public sealed record InhoudingsplichtPdfModel(
    string? LijstNaam, DateTime GecontroleerdOp, string? DoorNaam, Guid LijstId, bool Testomgeving,
    IReadOnlyList<InhoudingsplichtPdfRij> Rijen, string? AfgedruktDoor, DateTime AfgedruktOp);

/// <summary>
/// Controlelijst inhoudingsplicht (design-handoff 35s, A4 staand): bewijs van een RSZ-controle met de standaard gl-v2-kop (kicker, titel, datum),
/// gegevens van de controle + opdrachtgever, samenvatting, resultaat per onderneming (referentie per rij), "bij betaling" en de wettelijke vermelding
/// tegen de voet. Bij een controle uit de testomgeving komen het kader "Testomgeving" en een diagonaal watermerk erbij.
/// </summary>
public sealed class InhoudingsplichtDocumentV2 : GlV2PdfDocumentBase
{
    private static readonly CultureInfo NlBe = CultureInfo.GetCultureInfo("nl-BE");
    private const string Rood = "#8B2A2A";
    private readonly InhoudingsplichtPdfModel _m;
    private readonly IReadOnlyList<InhoudingsplichtPdfRij> _rijen;

    public InhoudingsplichtDocumentV2(InhoudingsplichtPdfModel model, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
    {
        _m = model ?? throw new ArgumentNullException(nameof(model));
        // Met inhoudingsplicht bovenaan, dan mislukt, dan de rest (zelfde volgorde als het zijpaneel); daarbinnen op naam.
        _rijen = model.Rijen
            .OrderBy(r => r.Inhouden ? 0 : r.Mislukt ? 1 : 2)
            .ThenBy(r => r.Naam ?? "￿", StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public override DocumentMetadata GetMetadata() => new()
    {
        Title = $"Controlelijst inhoudingsplicht {_m.GecontroleerdOp:dd/MM/yyyy HH:mm}",
        Author = Company.Name,
        Subject = "Bewijs van een RSZ-controle op inhoudingsplicht (art. 30bis/30ter)",
    };

    protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
    protected override string DocumentTitle => "Controlelijst";
    protected override string? Kicker => "Inhoudingsplicht";
    protected override string? DocumentNumber => _m.GecontroleerdOp.ToString("dd/MM/yyyy", NlBe) + " · " + _m.GecontroleerdOp.ToString("HH:mm", NlBe);
    protected override string? CompactSubtitle => DocumentNumber;
    protected override string? Watermark => _m.Testomgeving ? "TESTGEGEVENS" : null;

    private static string Nummer(string? n)
    {
        var digits = new string((n ?? "").Where(char.IsDigit).ToArray());
        var isBe = (n ?? "").TrimStart().StartsWith("BE", StringComparison.OrdinalIgnoreCase) || (n ?? "").All(ch => char.IsDigit(ch) || ch is '.' or ' ' or '-');
        if (isBe && digits.Length == 9) digits = "0" + digits;
        return isBe && digits.Length == 10 ? $"BE {digits[..4]}.{digits.Substring(4, 3)}.{digits[7..]}" : (n ?? "—");
    }

    private static string Datum(DateTime d) => d.ToString("dd/MM/yyyy", NlBe);
    private string Naam(InhoudingsplichtPdfRij r) => string.IsNullOrWhiteSpace(r.Naam) ? Nummer(r.Nummer) : r.Naam!;

    // ── Voet: adres links, lijst in het midden, "Afgedrukt door … · x / y" rechts ─────────────────────
    protected override void Footer(IContainer c)
    {
        c.BorderTop(0.3f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).PaddingTop(3, Unit.Millimetre).Row(row =>
        {
            var adres = string.Join(" · ", new[] { Company.Name, string.Join(", ", new[] { Company.Street, Company.PostalCity }.Where(s => !string.IsNullOrWhiteSpace(s))), Company.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
            row.RelativeItem().Text(adres).FontFamily(BodyFont).FontSize(6.5f).LineHeight(1.45f).FontColor(GlV2PdfTheme.Gedempt);
            row.AutoItem().PaddingHorizontal(4, Unit.Millimetre).Text($"Controlelijst inhoudingsplicht · {_m.GecontroleerdOp:dd/MM/yyyy HH:mm} · lijst {_m.LijstId.ToString()[..8]}")
                .FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt));
                t.Span($"Afgedrukt {_m.AfgedruktOp:dd/MM/yyyy}" + (string.IsNullOrWhiteSpace(_m.AfgedruktDoor) ? "" : " door " + _m.AfgedruktDoor) + " · ");
                t.CurrentPageNumber().SemiBold().FontColor(GlV2PdfTheme.Inkt);
                t.Span(" / ").SemiBold().FontColor(GlV2PdfTheme.Inkt);
                t.TotalPages().SemiBold().FontColor(GlV2PdfTheme.Inkt);
            });
        });
    }

    protected override void Content(IContainer c)
    {
        var mislukt = _rijen.Count(r => r.Mislukt);
        var metPlicht = _rijen.Count(r => r.Inhouden);
        var zonder = _rijen.Count - mislukt - metPlicht;

        c.Column(col =>
        {
            col.Spacing(3.5f, Unit.Millimetre);

            col.Item().Element(Gegevens);

            if (_m.Testomgeving)
                col.Item().Background(GlV2PdfTheme.VlakWarm).PaddingVertical(2.6f, Unit.Millimetre).PaddingHorizontal(3.5f, Unit.Millimetre).Row(r =>
                {
                    r.AutoItem().Text("Testomgeving").FontFamily(BodyFont).FontSize(7.5f).Bold().FontColor(GlV2PdfTheme.Inkt);
                    r.ConstantItem(3, Unit.Millimetre);
                    r.RelativeItem().Text("Dit resultaat komt uit de testomgeving van de RSZ (vaste testgegevens). Het is geen echte controle en heeft geen bewijswaarde. In productie verdwijnen dit kader en het watermerk.")
                        .FontFamily(BodyFont).FontSize(7.5f).LineHeight(1.45f).FontColor(GlV2PdfTheme.Inkt);
                });

            col.Item().Element(x => GlV2PdfComponents.SectieKop(x, BodyFont, "Samenvatting"));
            col.Item().Element(x => GlV2PdfComponents.Fiche(x, BodyFont, new[]
            {
                new GlV2PdfComponents.FicheVeld("Gecontroleerd", _rijen.Count.ToString(NlBe), _rijen.Count == 1 ? "onderneming" : "ondernemingen"),
                new GlV2PdfComponents.FicheVeld("Zonder schulden", zonder.ToString(NlBe), "betalen zonder inhouding"),
                new GlV2PdfComponents.FicheVeld("Met inhoudingsplicht", metPlicht.ToString(NlBe), metPlicht > 0 ? "zie hieronder" : null),
                new GlV2PdfComponents.FicheVeld("Mislukt", mislukt.ToString(NlBe), mislukt > 0 ? "opnieuw controleren" : null),
            }, 4, 12));

            col.Item().Element(x => GlV2PdfComponents.SectieKop(x, BodyFont, "Resultaat per onderneming"));
            col.Item().Element(Resultaten);
            col.Item().Text("✓ geen schulden op het moment van de controle · ! inhouden bij betaling · ondernemingen met inhoudingsplicht staan bovenaan")
                .FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Licht);

            col.Item().Element(x => GlV2PdfComponents.SectieKop(x, BodyFont, "Bij betaling"));
            col.Item().Element(BijBetaling);

            // Onderste blok tegen de voet (35a): neemt de resterende hoogte van de pagina en plakt eraan.
            col.Item().Extend().AlignBottom().ShowEntire().PaddingBottom(3, Unit.Millimetre).Column(w =>
            {
                w.Spacing(1, Unit.Millimetre);
                w.Item().Text("WETTELIJKE VERMELDING").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(GlV2PdfTheme.Groen);
                w.Item().Text("Art. 30bis van de wet van 27 juni 1969 (sociale schulden) en art. 402–403 WIB 92 (fiscale schulden): een opdrachtgever die betaalt aan een aannemer met schulden op het moment van betaling, houdt de vermelde percentages in en stort ze door. Wie dat niet doet, kan hoofdelijk aansprakelijk worden gesteld voor de schulden van de aannemer. Deze lijst geeft het antwoord van de RSZ weer op het vermelde tijdstip; de referenties laten toe het antwoord bij de RSZ na te gaan.")
                    .FontFamily(BodyFont).FontSize(7).LineHeight(1.5f).FontColor(GlV2PdfTheme.Gedempt);
            });
        });
    }

    // ── Gegevens van de controle (links) + opdrachtgever (rechts, 72mm) ───────────────────────────────
    private void Gegevens(IContainer c)
    {
        var traceIds = _rijen.Select(r => r.TraceId).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        var velden = new List<(string Label, string Waarde, bool Klein)>();
        if (!string.IsNullOrWhiteSpace(_m.LijstNaam)) velden.Add(("Naam", _m.LijstNaam!, false));
        velden.Add(("Gecontroleerd op", $"{Datum(_m.GecontroleerdOp)} om {_m.GecontroleerdOp:HH:mm}", false));
        if (!string.IsNullOrWhiteSpace(_m.DoorNaam)) velden.Add(("Door", _m.DoorNaam!, false));
        velden.Add(("Bron", "RSZ — dienst Inhoudingsplicht (sociaal en fiscaal)", false));
        velden.Add(("Lijst-ID", _m.LijstId.ToString(), true));
        if (traceIds.Count > 0) velden.Add(("Trace-ID", string.Join(", ", traceIds), true));

        c.Row(row =>
        {
            row.RelativeItem().Table(t =>
            {
                t.ColumnsDefinition(d => { d.ConstantColumn(30, Unit.Millimetre); d.RelativeColumn(); });
                foreach (var (label, waarde, klein) in velden)
                {
                    t.Cell().PaddingBottom(1.4f, Unit.Millimetre).Text(label).FontFamily(BodyFont).FontSize(8).SemiBold().LineHeight(1.35f).FontColor(GlV2PdfTheme.Inkt);
                    t.Cell().PaddingBottom(1.4f, Unit.Millimetre).Text(waarde).FontFamily(BodyFont).FontSize(klein ? 7.5f : 8).LineHeight(1.35f).FontColor(GlV2PdfTheme.Inkt)
                        .EnableFontFeature(FontFeatures.TabularFigures);
                }
            });
            row.ConstantItem(8, Unit.Millimetre);
            row.ConstantItem(GlV2PdfTheme.AdresKolomBreedteMm, Unit.Millimetre).Column(a =>
            {
                a.Spacing(1, Unit.Millimetre);
                a.Item().Text("OPDRACHTGEVER").FontFamily(BodyFont).FontSize(6).SemiBold().LetterSpacing(0.12f).FontColor(GlV2PdfTheme.Gedempt);
                var naam = string.IsNullOrWhiteSpace(Company.LegalForm) ? Company.Name : Company.LegalForm;
                a.Item().Text(naam).FontFamily(BodyFont).FontSize(8.5f).Bold().LineHeight(1.35f).FontColor(GlV2PdfTheme.Inkt);
                if (!string.IsNullOrWhiteSpace(Company.VatNumber))
                    a.Item().Text("btw " + Company.VatNumber).FontFamily(BodyFont).FontSize(8.5f).LineHeight(1.35f).FontColor(GlV2PdfTheme.Inkt);
                if (!string.IsNullOrWhiteSpace(Company.Street))
                    a.Item().Text(Company.Street).FontFamily(BodyFont).FontSize(8.5f).LineHeight(1.35f).FontColor(GlV2PdfTheme.Inkt);
                if (!string.IsNullOrWhiteSpace(Company.PostalCity))
                    a.Item().Text(Company.PostalCity).FontFamily(BodyFont).FontSize(8.5f).LineHeight(1.35f).FontColor(GlV2PdfTheme.Inkt);
            });
        });
    }

    // ── Resultaat per onderneming: kolommen vast in mm, kop herhaalt, een rij splitst nooit ───────────
    private void Resultaten(IContainer c)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(d =>
            {
                d.RelativeColumn();
                d.ConstantColumn(29.5f, Unit.Millimetre);
                d.ConstantColumn(29, Unit.Millimetre);
                d.ConstantColumn(29, Unit.Millimetre);
                d.ConstantColumn(19.5f, Unit.Millimetre);
                d.ConstantColumn(26, Unit.Millimetre);
            });

            t.Header(h =>
            {
                void Kop(string tekst) => h.Cell().BorderBottom(0.5f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Groen).PaddingBottom(1.4f, Unit.Millimetre).PaddingHorizontal(1.25f, Unit.Millimetre)
                    .AlignBottom().Text(tekst).FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.1f).LineHeight(1.25f).FontColor(GlV2PdfTheme.Inkt);
                Kop("ONDERNEMING"); Kop("ONDERNEMINGSNR."); Kop("SOCIAAL (RSZ)"); Kop("FISCAAL (FOD FIN.)"); Kop("GELDIG T.E.M."); Kop("REFERENTIE RSZ");
            });

            foreach (var r in _rijen)
            {
                IContainer Cel() => t.Cell().ShowEntire().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                    .PaddingVertical(1.6f, Unit.Millimetre).PaddingHorizontal(1.25f, Unit.Millimetre);

                Cel().Column(n =>
                {
                    if (string.IsNullOrWhiteSpace(r.Naam))
                    {
                        n.Item().Text("Naam niet ontvangen").FontFamily(BodyFont).FontSize(8).SemiBold().Italic().LineHeight(1.4f).FontColor(GlV2PdfTheme.Gedempt);
                        n.Item().Text("van de RSZ — controleer het nummer").FontFamily(BodyFont).FontSize(6.8f).FontColor(GlV2PdfTheme.Gedempt);
                    }
                    else n.Item().Text(r.Naam!).FontFamily(BodyFont).FontSize(8).Bold().LineHeight(1.4f).FontColor(GlV2PdfTheme.Inkt);
                });
                Cel().Text(Nummer(r.Nummer)).FontFamily(BodyFont).FontSize(8).LineHeight(1.4f).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);

                if (r.Mislukt)
                {
                    t.Cell().ColumnSpan(4).ShowEntire().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).PaddingVertical(1.6f, Unit.Millimetre).PaddingHorizontal(1.25f, Unit.Millimetre).Column(f =>
                    {
                        f.Item().Text("Mislukt — " + r.Fout).FontFamily(BodyFont).FontSize(7.5f).SemiBold().LineHeight(1.4f).FontColor(Rood);
                        if (!string.IsNullOrWhiteSpace(r.TraceId))
                            f.Item().Text("Trace-ID " + r.TraceId).FontFamily(BodyFont).FontSize(6.8f).FontColor(GlV2PdfTheme.Gedempt);
                    });
                    continue;
                }

                Cel().Element(x => Situatie(x, r.SociaalSituatie, r.SociaalPercentage, null));
                Cel().Element(x => Situatie(x, r.FiscaalSituatie, r.FiscaalPercentage, r.FiscaalAttest));
                Cel().Text(!r.Inhouden && r.GeldigTot is { } g ? Datum(g) : "—").FontFamily(BodyFont).FontSize(8).LineHeight(1.4f).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                Cel().Text(string.IsNullOrWhiteSpace(r.Referentie) ? "—" : r.Referentie).FontFamily(BodyFont).FontSize(7.5f).LineHeight(1.4f).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
            }
        });
    }

    private void Situatie(IContainer c, string? situatie, int? percentage, long? attest)
    {
        c.Column(s =>
        {
            switch (situatie)
            {
                case "obligation":
                    s.Item().Text(t => t.Span($" ! Inhouden{(percentage is int p ? " " + p.ToString(NlBe) + " %" : "")} ").BackgroundColor(GlV2PdfTheme.VlakWarm)
                        .FontFamily(BodyFont).FontSize(7.5f).Bold().FontColor(GlV2PdfTheme.Inkt));
                    break;
                case "noObligation":
                    s.Item().Text("✓ Geen schulden").FontFamily(BodyFont).FontSize(7.5f).SemiBold().FontColor(GlV2PdfTheme.Groen);
                    break;
                case "notInDomain":
                    s.Item().Text("Buiten toepassing").FontFamily(BodyFont).FontSize(7.5f).SemiBold().FontColor(GlV2PdfTheme.Gedempt);
                    break;
                default:
                    s.Item().Text("—").FontFamily(BodyFont).FontSize(7.5f).FontColor(GlV2PdfTheme.Gedempt);
                    break;
            }
            if (attest is long a)
                s.Item().Text("attest " + a.ToString(CultureInfo.InvariantCulture)).FontFamily(BodyFont).FontSize(6.8f).FontColor(GlV2PdfTheme.Gedempt).EnableFontFeature(FontFeatures.TabularFigures);
        });
    }

    // ── Bij betaling: per onderneming met inhoudingsplicht wat te doen, daarna de overige ─────────────
    private void BijBetaling(IContainer c)
    {
        var metPlicht = _rijen.Where(r => r.Inhouden).ToList();
        var overige = _rijen.Where(r => !r.Inhouden && !r.Mislukt).ToList();
        var mislukt = _rijen.Where(r => r.Mislukt).ToList();
        var geldigTot = overige.Where(r => r.GeldigTot.HasValue).Select(r => r.GeldigTot!.Value).DefaultIfEmpty().Min();
        var tot = geldigTot == default ? _m.GecontroleerdOp : geldigTot;

        c.Background(GlV2PdfTheme.VlakGroen).PaddingVertical(3.5f, Unit.Millimetre).PaddingHorizontal(4, Unit.Millimetre).Column(col =>
        {
            col.Spacing(1.6f, Unit.Millimetre);
            void Regel(Action<TextDescriptor> bouw) => col.Item().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(8.5f).LineHeight(1.5f).FontColor(GlV2PdfTheme.Inkt));
                bouw(t);
            });

            foreach (var r in metPlicht)
            {
                Regel(t =>
                {
                    t.Span(Nummer(r.Nummer)).Bold();
                    if (!string.IsNullOrWhiteSpace(r.Naam)) t.Span(" " + r.Naam).Bold();
                    t.Span(" — houd ");
                    var delen = new List<(int Pct, string Wie)>();
                    if (r.SociaalSituatie == "obligation") delen.Add((r.SociaalPercentage ?? 0, "RSZ"));
                    if (r.FiscaalSituatie == "obligation") delen.Add((r.FiscaalPercentage ?? 0, "FOD Financiën"));
                    for (var i = 0; i < delen.Count; i++)
                    {
                        if (i > 0) t.Span(", en ");
                        t.Span(delen[i].Pct.ToString(NlBe) + " %").Bold();
                        if (i == 0) t.Span(" van het factuurbedrag excl. btw in en stort het aan de ");
                        else t.Span(" aan de ");
                        t.Span(delen[i].Wie).Bold();
                    }
                    t.Span(". Vermeld het ondernemingsnummer van de aannemer. Betaal de rest aan de aannemer.");
                });
            }
            if (overige.Count > 0)
                Regel(t =>
                {
                    t.Span(metPlicht.Count > 0 ? "Overige ondernemingen" : "Alle ondernemingen").Bold();
                    t.Span($" — geen inhouding nodig voor een betaling tot en met {Datum(tot)}. Betaal je later, controleer dan opnieuw en voeg die lijst bij.");
                });
            if (mislukt.Count > 0)
                Regel(t =>
                {
                    t.Span("Niet gecontroleerd").Bold();
                    t.Span(" — " + string.Join(", ", mislukt.Select(Naam)) + ". Controleer deze opnieuw vóór je betaalt.");
                });
            if (_rijen.Count == 0)
                Regel(t => t.Span("Deze lijst bevat geen ondernemingen."));
        });
    }
}
