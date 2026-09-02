using System;
using System.Collections.Generic;
using System.Linq;
using CPMCore.Models.Projecten;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Aannemerslijst voor een werf: een PROJECTFICHE en een tabel met alle aannemers,
    /// studiebureaus en nutspartijen, gegroepeerd per lot/deel. De vijf statuskolommen
    /// (Contract verstuurd/getekend, VGM charter, Werfmelding, PID-attesten) zijn optioneel
    /// via <see cref="SupplierListColumns"/>. Kop/voet/kleuren komen van <see cref="GroupLnPdfDocument"/>.
    /// </summary>
    public class SupplierListDocument : GroupLnPdfDocument
    {
        private readonly SupplierListModel _m;
        private readonly SupplierListColumns _cols;

        public SupplierListDocument(SupplierListModel model, SupplierListColumns cols,
            byte[] logoBytes = null, string fontFamily = null, int version = 1)
            : base(logoBytes, fontFamily, version)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _cols = cols ?? new SupplierListColumns(true, true, true, true, true);
        }

        public override DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Aannemerslijst {_m.ProjectName} - {DateTime.Now:dd/MM/yyyy}"
        };

        protected override string DocumentTitle => "Aannemerslijst";

        protected override string WerfTitel => _m.Project?.ProjectName ?? _m.ProjectName;

        private string Opgemaakt => (_m.Project?.LaatstBijgewerkt == default
            ? DateTime.Now
            : _m.Project.LaatstBijgewerkt).ToString("dd/MM/yyyy", Culture);

        protected override string HeaderMetaLine =>
            $"Opgemaakt {Opgemaakt} · versie {Version} · t.b.v. veiligheidscoördinatie & postinterventiedossier";

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

        // ── Inhoud ────────────────────────────────────────────────────────────────
        protected override void Content(IContainer c)
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

        // ── Projectfiche ──────────────────────────────────────────────────────────
        private void Fiche(IContainer c)
        {
            var p = _m.Project ?? new SupplierListProjectInfo { ProjectName = _m.ProjectName };

            string Dot(params string[] parts) =>
                string.Join(" · ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));

            var werfmelding = p.WerfmeldingDate.HasValue
                ? $"Ingediend {p.WerfmeldingDate.Value.ToString("dd/MM/yyyy", Culture)}"
                : "Nog niet ingediend";
            var werfmeldingSub = string.IsNullOrWhiteSpace(p.WerfmeldingDossier) ? null : $"Dossier {p.WerfmeldingDossier}";

            var partijenSub = p.LaatstBijgewerkt == default ? null
                : $"Laatst bijgewerkt: {p.LaatstBijgewerkt.ToString("dd/MM/yyyy", Culture)}"
                  + (string.IsNullOrWhiteSpace(p.LaatstBijgewerktDoor) ? "" : $" door {p.LaatstBijgewerktDoor}");

            var boxes = new (string Label, string Value, string Small)[]
            {
                ("Werf / adres", string.IsNullOrWhiteSpace(p.AddressLine) ? p.ProjectName : p.AddressLine, p.CityLine),
                ("Opdrachtgever", p.OpdrachtgeverName, p.OpdrachtgeverAddress),
                ("Projectcoördinatie", p.ProjectcoordinatieName, Dot(p.ProjectcoordinatiePhone, p.ProjectcoordinatieEmail)),
                ("Veiligheidscoördinator", p.VeiligheidscoordinatorName, Dot(p.VeiligheidscoordinatorAddress, p.VeiligheidscoordinatorEmail)),
                ("Aard van de werken", p.AardVanDeWerken, null),
                ("Startdatum werf", p.StartDatumWerf?.ToString("dd/MM/yyyy", Culture), null),
                ("Werfmelding", werfmelding, werfmeldingSub),
                ("Aantal loten / partijen", $"{p.AantalPartijen} partijen", partijenSub),
            };

            FicheGrid(c, boxes);
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
    }
}
