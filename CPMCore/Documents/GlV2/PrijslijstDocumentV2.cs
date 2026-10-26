using CPMCore.Services.Verkoop;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static CPMCore.Documents.GlV2.GlV2PdfComponents;

namespace CPMCore.Documents.GlV2;

/// <summary>Opties van de prijslijst (design-handoff 45f "Opnemen" en "Opmaak").</summary>
public sealed record PrijslijstOpties(bool VerkochtZonderPrijs = true, bool TotaalKoper = true, bool OptieAlsTeKoop = false, bool Liggend = true);

/// <summary>Prijslijst van de eenheden van een project (design-handoff 45h): A4 liggend (of staand), verkochte eenheden zonder prijs.
/// Gebruikt dezelfde rijen en berekening als de Te koop-pagina (<see cref="VerkoopService.GetIndexAsync"/>).</summary>
public sealed class PrijslijstDocumentV2 : GlV2PdfDocumentBase
{
    private readonly string _project;
    private readonly IReadOnlyList<VerkoopRijVm> _rijen;
    private readonly PrijslijstOpties _opties;

    public PrijslijstDocumentV2(string project, IReadOnlyList<VerkoopRijVm> rijen, PrijslijstOpties opties, GlV2PdfCompanyInfo company, bool fontsAvailable) : base(company, fontsAvailable)
    {
        _project = project; _rijen = rijen; _opties = opties;
    }

    public override DocumentMetadata GetMetadata() => new() { Title = $"Prijslijst — {_project}", Author = Company.Name };
    protected override GlV2PdfFormat Format => _opties.Liggend ? GlV2PdfFormat.A4Liggend : GlV2PdfFormat.A4Staand;
    protected override string DocumentTitle => "Prijslijst";
    protected override string? Kicker => null;
    protected override string? DocumentNumber => _project;
    protected override string? CompactNote => "opgemaakt " + DateTime.Today.ToString("dd/MM/yyyy") + " · prijzen onder voorbehoud van verkoop";
    protected override string? CompactSubtitle => _project;

    private string Status(VerkoopRijVm r) => r.Status == "Optie" && _opties.OptieAlsTeKoop ? "Te koop" : r.Status;

    protected override void Content(IContainer c)
    {
        var beschikbaar = _rijen.Count(r => r.Status != "Verkocht");
        // Breedtes in mm per opmaak; null = kolom valt weg. Staand (A4 = 174 mm tussen de marges) laat Type en Perceel weg, anders past de tabel niet.
        var liggend = _opties.Liggend;
        var spec = new List<(string Kop, float? Breedte, bool Rechts, Func<VerkoopRijVm, bool, string> Waarde)>
        {
            ("Eenheid", liggend ? 24 : 18, false, (r, v) => r.Naam),
            ("Type", liggend ? 26 : null, false, (r, v) => r.TypeNaam),
            ("Perceel", liggend ? 20 : null, true, (r, v) => r.Perceel > 0m ? $"{r.Perceel:N0} m²" : "—"),
            ("Wonen", liggend ? 20 : 15, true, (r, v) => r.Wonen > 0m ? $"{r.Wonen:N0} m²" : "—"),
            ("Slpk.", liggend ? 12 : 9, true, (r, v) => r.Slaapkamers > 0 ? r.Slaapkamers.ToString() : "—"),
            ("Grond", liggend ? 26 : 21, true, (r, v) => v ? "verkocht" : Euro(r.Grond)),
            ("Bouw excl. btw", liggend ? 28 : 26, true, (r, v) => v ? "" : Euro(r.Bouw)),
            ("Prijs", liggend ? 26 : 21, true, (r, v) => v ? "" : Euro(r.Totaal)),
        };
        if (_opties.TotaalKoper) spec.Add(("Totaal koper*", liggend ? 28 : 23, true, (r, v) => v ? "" : Euro(r.Koper.Totaal)));
        spec = spec.Where(s => s.Breedte != null).ToList();
        var kolommen = spec.Select(s => new TabelKolom(s.Kop, s.Breedte, s.Rechts)).ToList();
        kolommen.Add(new("Status"));

        var rijen = _rijen.Select(r =>
        {
            var status = Status(r);
            var verborgen = status == "Verkocht" && _opties.VerkochtZonderPrijs;
            var cellen = spec.Select(s => new TabelCel(s.Waarde(r, verborgen))).ToList();
            cellen.Add(status == "Verkocht" ? new TabelCel("VERKOCHT", "#EDEFEB", "#56664F", 6f) : status == "Optie" ? new TabelCel("OPTIE", "#F6EEDC", "#8A6A32", 6f) : new TabelCel("TE KOOP", "#E4EEDD", "#2F6038", 6f));
            return (IReadOnlyList<TabelCel>)cellen;
        }).ToList();

        c.Column(col =>
        {
            col.Spacing(4, Unit.Millimetre);
            col.Item().Element(x => Fiche(x, BodyFont, new List<FicheVeld> { new("Project", _project, $"{beschikbaar} van {_rijen.Count} eenheden beschikbaar") }, 1));
            col.Item().Element(x => Tabel(x, BodyFont, kolommen, rijen, "Geen eenheden."));
            col.Item().Column(n =>
            {
                n.Spacing(1, Unit.Millimetre);
                if (_opties.TotaalKoper) n.Item().Text(Voetnoot()).FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt);
                n.Item().Text("Prijzen onder voorbehoud van verkoop en wijziging." + (_opties.VerkochtZonderPrijs ? " Verkochte eenheden worden zonder prijs vermeld." : "") + " Plannen en technische beschrijving op aanvraag.")
                    .FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt);
            });
        });
    }

    /// <summary>"* Totaal koper = grond + 12 % registratierechten + bouw + 21 % btw + kosten koper (€ 10.085: …)", volgens het stelsel van het project.</summary>
    private string Voetnoot()
    {
        var k = _rijen.Where(r => r.Status != "Verkocht").Select(r => r.Koper).FirstOrDefault() ?? _rijen.FirstOrDefault()?.Koper;
        if (k == null) return "* Totaal koper = prijs vermeerderd met belastingen en kosten koper.";
        var delen = new List<string> { "grond" };
        if (k.Registratierechten > 0m) delen.Add($"{k.RegistratiePercentage:0.##} % registratierechten");
        delen.Add("bouw");
        if (k.Btw > 0m) delen.Add($"{k.BtwPercentage:0.##} % btw");
        delen.Add("kosten koper");
        return $"* Totaal koper = {string.Join(" + ", delen)} (kosten koper en notaris: {Euro(k.KostenKoper + k.KostenBtw + k.Notaris.Totaal)})."
             .Replace("  ", " ");
    }
}
