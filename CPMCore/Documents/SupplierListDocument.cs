using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CPMCore.Models.Projecten;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Aannemerslijst voor een werf. De layout/styling is 1-op-1 overgenomen van het
    /// aangeleverde Claude-Design-referentiebestand: A4 liggend, groene huisstijl van Group LN,
    /// een PROJECTFICHE (4-koloms raster met randen) en een tabel met alle aannemers,
    /// studiebureaus en nutspartijen, gegroepeerd per lot/deel. De vijf statuskolommen
    /// (Contract verstuurd/getekend, VGM charter, Werfmelding, PID-attesten) zijn optioneel
    /// via <see cref="SupplierListColumns"/>.
    ///
    /// De referentie gebruikt Playfair Display / Inter / JetBrains Mono; die woff2-fonts kan
    /// QuestPDF niet laden. We gebruiken Avenir (de échte Group LN-woordmerkfont uit de
    /// WWWCOPRO-menubalk) voor alles; kleuren, maten, spatiëring en randen volgen de referentie.
    /// </summary>
    public class SupplierListDocument : IDocument
    {
        private readonly SupplierListModel _m;
        private readonly SupplierListColumns _cols;
        private readonly byte[] _logoBytes;
        private readonly string _fontFamily;
        private readonly int _version;
        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("nl-BE");

        // Kleurtokens uit de Claude-Design-referentie
        private const string Green900 = "#0d3a24";   // thead-achtergrond, bandrij-tekst
        private const string Green700 = "#0f5132";   // koplijn, lot-tekst, vinkje
        private const string GreenAcc = "#2f6b46";   // titelaccent, sectielabels, e-mail
        private const string GreenSoft = "#e7eee8";  // bandrij-achtergrond
        private const string Sand = "#cdb885";       // linkerrand legende, opsommingsteken
        private const string Heading = "#36423a";    // titel, fiche-waarden, bedrijfsnaam
        private const string Ink = "#2c322e";        // bodytekst, telefoon
        private const string Muted = "#76807a";      // meta, subtekst, labels
        private const string BorderCol = "#dde1d9";  // fiche-randen, scheidingslijnen
        private const string RowLine = "#ecefe9";    // onderlijn tabelrijen
        private const string GrpLine = "#c8d6cc";    // boven-/onderlijn bandrij
        private const string HeadSep = "#356047";    // verticale scheiding in de kop
        private const string Zebra = "#fafbf9";      // even datarijen
        private const string No = "#c9cec6";         // streepje / n.v.t.
        private const string BrandName = "#5b6660";  // "GROUP"
        private const string BrandSub = "#97a09a";   // "PROJECTONTWIKKELING"

        // Group LN-gegevens (kunnen later uit een IssuerCompany komen).
        private const string LnName = "Group LN";
        private const string LnStreet = "Klaverdries 53";
        private const string LnCity = "9031 Drongen";
        private const string LnEmail = "info@groupln.be";

        public SupplierListDocument(SupplierListModel model, SupplierListColumns cols,
            byte[] logoBytes = null, string fontFamily = null, int version = 1)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _cols = cols ?? new SupplierListColumns(true, true, true, true, true);
            _logoBytes = logoBytes;
            _fontFamily = string.IsNullOrWhiteSpace(fontFamily) ? "Lato" : fontFamily;
            _version = version < 1 ? 1 : version;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Aannemerslijst {_m.ProjectName} - {DateTime.Now:dd/MM/yyyy}"
        };

        private string WerfTitel => _m.Project?.ProjectName ?? _m.ProjectName;

        private string Opgemaakt => (_m.Project?.LaatstBijgewerkt == default
            ? DateTime.Now
            : _m.Project.LaatstBijgewerkt).ToString("dd/MM/yyyy", _culture);

        // (kopregel 1, kopregel 2, is-contractkolom, waardeselector)
        private IEnumerable<(string L1, string L2, bool Contract, Func<SupplierListRow, bool> Value)> StatusColumns()
        {
            if (_cols.Sent) yield return ("Contract", "verst.", true, r => r.SentDate.HasValue || !string.IsNullOrWhiteSpace(r.SentNote));
            if (_cols.Signed) yield return ("Contract", "get.", true, r => r.Signed);
            if (_cols.Vgm) yield return ("VGM", "charter", false, r => r.Vgm);
            if (_cols.Notification) yield return ("Werf-", "melding", false, r => r.Notification);
            if (_cols.Pid) yield return ("PID", "attesten", false, r => r.Pid);
        }

        private int TotalColumns => 4 + StatusColumns().Count();

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(26);
                page.MarginHorizontal(28);
                page.DefaultTextStyle(x => x.FontSize(7.4f).FontFamily(_fontFamily).FontColor(Ink).LineHeight(1.45f));

                page.Header().Element(Header);
                page.Content().PaddingTop(14).Element(Content);
                page.Footer().PaddingTop(12).Element(Footer);
            });
        }

        // ── Kop ───────────────────────────────────────────────────────────────────
        private void Header(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().PaddingBottom(9).Row(row =>
                {
                    row.AutoItem().AlignBottom().Row(b =>
                    {
                        if (_logoBytes is { Length: > 0 })
                            b.ConstantItem(26).AlignMiddle().Image(_logoBytes).FitWidth();
                        b.ConstantItem(11);
                        b.AutoItem().AlignMiddle().Column(t =>
                        {
                            t.Item().Text(x =>
                            {
                                x.Span("GROUP ").FontSize(13.5f).Bold().FontColor(BrandName).LetterSpacing(0.02f);
                                x.Span("LN").FontSize(13.5f).Bold().FontColor(GreenAcc).LetterSpacing(0.02f);
                            });
                            t.Item().PaddingTop(3).Text("PROJECTONTWIKKELING")
                                .FontSize(6.4f).Medium().FontColor(BrandSub).LetterSpacing(0.23f);
                        });
                    });

                    row.RelativeItem().PaddingLeft(24).AlignBottom().Column(r =>
                    {
                        r.Item().AlignRight().Text("Aannemerslijst")
                            .FontSize(15.75f).Medium().FontColor(Heading).LetterSpacing(0.01f);
                        r.Item().PaddingTop(4).AlignRight().Text(WerfTitel)
                            .FontSize(8.5f).SemiBold().FontColor(GreenAcc).LetterSpacing(0.03f);
                        r.Item().PaddingTop(3).AlignRight().Text(
                            $"Opgemaakt {Opgemaakt} · versie {_version} · t.b.v. veiligheidscoördinatie & postinterventiedossier")
                            .FontSize(6.8f).FontColor(Muted).LetterSpacing(0.02f);
                    });
                });

                col.Item().LineHorizontal(2).LineColor(Green700);
            });
        }

        // ── Inhoud ────────────────────────────────────────────────────────────────
        private void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Projectfiche", first: true));
                col.Item().PaddingBottom(16).Element(Fiche);
                col.Item().Element(x => SectionLabel(x, "Aannemers, studiebureaus & nutspartijen"));
                col.Item().Element(PartyTable);
                col.Item().PaddingTop(16).Element(Legend);
            });
        }

        private void SectionLabel(IContainer c, string text, bool first = false)
        {
            c.PaddingTop(first ? 4 : 12).PaddingBottom(8).Row(row =>
            {
                row.AutoItem().Text(text.ToUpperInvariant())
                    .FontSize(6.8f).Bold().FontColor(GreenAcc).LetterSpacing(0.26f);
                row.ConstantItem(12);
                row.RelativeItem().AlignMiddle().LineHorizontal(1).LineColor(BorderCol);
            });
        }

        // ── Projectfiche ──────────────────────────────────────────────────────────
        private void Fiche(IContainer c)
        {
            var p = _m.Project ?? new SupplierListProjectInfo { ProjectName = _m.ProjectName };

            string Dot(params string[] parts) =>
                string.Join(" · ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));

            var werfmelding = p.WerfmeldingDate.HasValue
                ? $"Ingediend {p.WerfmeldingDate.Value.ToString("dd/MM/yyyy", _culture)}"
                : "Nog niet ingediend";
            var werfmeldingSub = string.IsNullOrWhiteSpace(p.WerfmeldingDossier) ? null : $"Dossier {p.WerfmeldingDossier}";

            var partijenSub = p.LaatstBijgewerkt == default ? null
                : $"Laatst bijgewerkt: {p.LaatstBijgewerkt.ToString("dd/MM/yyyy", _culture)}"
                  + (string.IsNullOrWhiteSpace(p.LaatstBijgewerktDoor) ? "" : $" door {p.LaatstBijgewerktDoor}");

            var boxes = new (string Label, string Value, string Small)[]
            {
                ("Werf / adres", string.IsNullOrWhiteSpace(p.AddressLine) ? p.ProjectName : p.AddressLine, p.CityLine),
                ("Opdrachtgever", p.OpdrachtgeverName, p.OpdrachtgeverAddress),
                ("Projectcoördinatie", p.ProjectcoordinatieName, Dot(p.ProjectcoordinatiePhone, p.ProjectcoordinatieEmail)),
                ("Veiligheidscoördinator", p.VeiligheidscoordinatorName, Dot(p.VeiligheidscoordinatorAddress, p.VeiligheidscoordinatorEmail)),
                ("Aard van de werken", p.AardVanDeWerken, null),
                ("Startdatum werf", p.StartDatumWerf?.ToString("dd/MM/yyyy", _culture), null),
                ("Werfmelding", werfmelding, werfmeldingSub),
                ("Aantal loten / partijen", $"{p.AantalPartijen} partijen", partijenSub),
            };

            c.Border(0.75f).BorderColor(BorderCol).Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn();
                    d.RelativeColumn();
                    d.RelativeColumn();
                    d.RelativeColumn();
                });

                for (int i = 0; i < boxes.Length; i++)
                {
                    var (label, value, small) = boxes[i];
                    IContainer cell = t.Cell().BorderColor(BorderCol);
                    if (i % 4 != 3) cell = cell.BorderRight(0.75f);
                    if (i < 4) cell = cell.BorderBottom(0.75f);
                    cell.PaddingVertical(8).PaddingHorizontal(11).Column(b =>
                    {
                        b.Item().Text(label.ToUpperInvariant())
                            .FontSize(6.4f).Bold().FontColor(Muted).LetterSpacing(0.15f);
                        b.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                            .FontSize(8).SemiBold().FontColor(Heading).LineHeight(1.4f).WrapAnywhere();
                        if (!string.IsNullOrWhiteSpace(small))
                            b.Item().PaddingTop(1).Text(small).FontSize(7).FontColor(Muted).LineHeight(1.4f).WrapAnywhere();
                    });
                }
            });
        }

        // ── Aannemerstabel ────────────────────────────────────────────────────────
        private void PartyTable(IContainer c)
        {
            var statusCols = StatusColumns().ToList();

            if (_m.Rows.Count == 0)
            {
                c.Text("Er zijn nog geen aannemers of contracten voor dit project.")
                    .Italic().FontColor(Muted);
                return;
            }

            var groups = _m.Rows
                .GroupBy(r => new { r.GroupLot, r.GroupName })
                .OrderBy(g => g.Key.GroupLot)
                .ThenBy(g => g.Key.GroupName)
                .ToList();

            c.Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(16f);   // LOT
                    d.RelativeColumn(19f);   // AANNEMER / PARTIJ
                    d.RelativeColumn(18f);   // ADRES
                    d.RelativeColumn(18f);   // CONTACT & E-MAIL
                    foreach (var _ in statusCols) d.RelativeColumn(5.8f);
                });

                t.Header(h =>
                {
                    void HeadCell(bool center, bool last, Action<IContainer> content)
                    {
                        var cell = h.Cell().Background(Green900);
                        cell = center
                            ? cell.PaddingVertical(7).PaddingHorizontal(3)
                            : cell.PaddingVertical(7).PaddingHorizontal(8);
                        if (!last) cell = cell.BorderRight(1).BorderColor(HeadSep);
                        content(cell.AlignBottom());
                    }

                    void HeadText(IContainer x, string s) =>
                        x.Text(s).FontSize(6.6f).Bold().FontColor(Colors.White).LetterSpacing(0.125f);

                    HeadCell(false, false, x => HeadText(x, "Lot"));
                    HeadCell(false, false, x => HeadText(x, "Aannemer / partij"));
                    HeadCell(false, false, x => HeadText(x, "Adres"));
                    HeadCell(false, false, x => HeadText(x, "Contact & e-mail"));
                    for (int i = 0; i < statusCols.Count; i++)
                    {
                        var sc = statusCols[i];
                        HeadCell(true, i == statusCols.Count - 1, x => x.Column(cc =>
                        {
                            cc.Item().AlignCenter().Text(sc.L1).FontSize(6.6f).Bold().FontColor(Colors.White).LineHeight(1.2f);
                            cc.Item().AlignCenter().Text(sc.L2).FontSize(6.6f).Bold().FontColor(Colors.White).LineHeight(1.2f);
                        }));
                    }
                });

                var dataIdx = 0;
                foreach (var g in groups)
                {
                    var lotLabel = g.Key.GroupLot > 0
                        ? $"Deel {g.Key.GroupLot} — {g.Key.GroupName}"
                        : "Algemeen";
                    t.Cell().ColumnSpan((uint)TotalColumns)
                        .Background(GreenSoft)
                        .BorderTop(1).BorderBottom(1).BorderColor(GrpLine)
                        .PaddingVertical(5).PaddingHorizontal(8)
                        .Text(lotLabel.ToUpperInvariant())
                        .FontSize(7).Bold().FontColor(Green900).LetterSpacing(0.17f);

                    foreach (var r in g.OrderBy(x => x.IsSynthesized)
                                       .ThenBy(x => x.ActivityName)
                                       .ThenBy(x => x.CompanyName))
                    {
                        var bg = (dataIdx % 2) == 1 ? Zebra : "#ffffff";
                        dataIdx++;

                        IContainer Cell() => t.Cell().Background(bg).BorderBottom(1).BorderColor(RowLine)
                            .PaddingVertical(6).PaddingHorizontal(8);
                        IContainer StatusCell() => t.Cell().Background(bg).BorderBottom(1).BorderColor(RowLine)
                            .PaddingVertical(6).PaddingHorizontal(3);

                        // LOT
                        Cell().Text(r.ActivityName).FontSize(7.4f).SemiBold().FontColor(Green700);

                        // AANNEMER / PARTIJ
                        Cell().Column(a =>
                        {
                            a.Item().Text(string.IsNullOrWhiteSpace(r.CompanyName) ? "—" : r.CompanyName)
                                .FontSize(7.4f).Bold().FontColor(Heading).LetterSpacing(0.01f);
                            if (!string.IsNullOrWhiteSpace(r.Vat))
                                a.Item().PaddingTop(1).Text($"BE {r.Vat}").FontSize(6.6f).FontColor(Muted);
                        });

                        // ADRES
                        Cell().Column(a =>
                        {
                            if (string.IsNullOrWhiteSpace(r.Address))
                                a.Item().Text("—").FontSize(7.4f).Italic().FontColor(Muted);
                            else
                                foreach (var line in r.Address.Split('\n'))
                                    a.Item().Text(line).FontSize(7.4f).Italic().FontColor(Muted);
                        });

                        // CONTACT & E-MAIL
                        Cell().Column(a =>
                        {
                            if (r.ContactIsGeneral)
                                a.Item().Text($"Algemeen — {r.CompanyName}").FontSize(7.4f).Italic().FontColor(Muted);
                            else if (!string.IsNullOrWhiteSpace(r.ContactName))
                                a.Item().Text(r.ContactName).FontSize(7.4f).FontColor(Ink);
                            if (!string.IsNullOrWhiteSpace(r.ContactPhone))
                                a.Item().Text(r.ContactPhone).FontSize(6.8f).FontColor(Ink);
                            if (!string.IsNullOrWhiteSpace(r.ContactEmail))
                                a.Item().Text(r.ContactEmail).FontSize(7.4f).FontColor(GreenAcc).WrapAnywhere();
                        });

                        // STATUSKOLOMMEN
                        foreach (var sc in statusCols)
                        {
                            var cell = StatusCell().AlignCenter().AlignMiddle();
                            if (r.IsSynthesized && !sc.Contract)
                                cell.Text("n.v.t.").FontSize(6.6f).FontColor(No);
                            else if (r.IsSynthesized)
                                cell.Text("—").FontSize(9).FontColor(No);
                            else if (sc.Value(r))
                                cell.Text("✓").FontSize(9).Bold().FontColor(Green700).LineHeight(1f);
                            else
                                cell.Text("—").FontSize(9).FontColor(No);
                        }
                    }
                }
            });
        }

        // ── Legende ───────────────────────────────────────────────────────────────
        private void Legend(IContainer c)
        {
            c.BorderLeft(2).BorderColor(Sand).PaddingLeft(12).PaddingVertical(2).Column(col =>
            {
                col.Item().PaddingBottom(5).Text("LEGENDE & GEBRUIK")
                    .FontSize(6.8f).Bold().FontColor(GreenAcc).LetterSpacing(0.2f);

                void L(string term, string body) => col.Item().PaddingBottom(2).Text(t =>
                {
                    t.Span("— ").FontColor(Sand);
                    t.Span(term).FontSize(7).Bold().FontColor(Muted);
                    t.Span(" — " + body).FontSize(7).FontColor(Muted).LineHeight(1.6f);
                });

                L("VGM charter", "ondertekende verklaring inzake veiligheid, gezondheid en milieu, inclusief risicoanalyse van de eigen werkzaamheden.");
                L("Werfmelding", "aanwezigheidsmelding en aanmelding onderaannemers vóór aanvang van de eigen werken.");
                L("Attesten PID", "as-builtgegevens, materiaalfiches, keuringsverslagen en onderhoudsinstructies voor het postinterventiedossier.");
            });
        }

        // ── Voet ──────────────────────────────────────────────────────────────────
        private void Footer(IContainer c)
        {
            c.Column(fc =>
            {
                fc.Item().LineHorizontal(1).LineColor(BorderCol);
                fc.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text($"{LnName} · {LnStreet}, {LnCity} · {LnEmail}")
                        .FontSize(6.6f).FontColor(Muted).LetterSpacing(0.034f);
                    row.RelativeItem().AlignMiddle().AlignCenter().Text(
                        $"Aannemerslijst — {WerfTitel} · v{_version} · {DateTime.Now.ToString("dd/MM/yyyy", _culture)}")
                        .FontSize(6.6f).FontColor(Muted).LetterSpacing(0.034f);
                    row.RelativeItem().AlignMiddle().AlignRight().Text(t =>
                    {
                        t.Span("Vertrouwelijk — enkel voor projectbetrokkenen · ").FontSize(6.6f).FontColor(Muted).LetterSpacing(0.034f);
                        t.CurrentPageNumber().FontSize(6.6f).FontColor(Muted);
                        t.Span("/").FontSize(6.6f).FontColor(Muted);
                        t.TotalPages().FontSize(6.6f).FontColor(Muted);
                    });
                });
            });
        }
    }
}
