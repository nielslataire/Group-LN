using System;
using System.Collections.Generic;
using System.Linq;
using CPMCore.Models.Projecten;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Eenhedenlijst voor een project: een PROJECTFICHE en daaronder de eenhedentabel zoals
    /// Projecten/DetailUnitsV2 die op het scherm toont — per groep ("Woningen", "Los te koop") een
    /// bandrij, de gekoppelde bergingen/parkings ingesprongen onder hun hoofdeenheid, en een
    /// totaalregel. Kop/voet/kleuren komen van <see cref="GroupLnPdfDocument"/>, exact zoals
    /// <see cref="ClientListDocument"/>.
    ///
    /// Eén document voor beide varianten van design-handoff punt 16: bij een project mét basisakte is
    /// de voorlaatste kolom AANDEEL, zonder basisakte wordt dat KOPER — dezelfde
    /// <see cref="DetailUnitsV2Vm.HasBasisakte"/>-schakel als de webpagina, zodat print en scherm
    /// nooit een andere kolommenset laten zien.
    /// </summary>
    public class UnitListDocument : GroupLnPdfDocument
    {
        private readonly DetailUnitsV2Vm _m;
        private readonly UnitListProjectInfo _p;

        public UnitListDocument(DetailUnitsV2Vm model, UnitListProjectInfo projectInfo, byte[] logoBytes = null, string fontFamily = null, int version = 1)
            : base(logoBytes, fontFamily, version)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _p = projectInfo ?? new UnitListProjectInfo();
        }

        public override DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Eenhedenlijst {_m.ProjectName} - {DateTime.Now:dd/MM/yyyy}"
        };

        protected override string DocumentTitle => "Eenhedenlijst";

        protected override string WerfTitel => _m.ProjectName;

        protected override string HeaderMetaLine =>
            $"Opgemaakt {DateTime.Now.ToString("dd/MM/yyyy", Culture)} · versie {Version} · "
            + (_m.HasBasisakte
                ? "eenheden, prijzen & aandelen basisakte"
                : "eenheden, prijzen & kopers — losse loten, geen mede-eigendom");

        private string Euro(decimal value) => value.ToString("C0", Culture);
        private string Num(decimal value) => value.ToString("#,##0.##", Culture);
        private string M2(decimal? value) => (value ?? 0m) > 0m ? Num(value.Value) + " m²" : "—";

        // ── Inhoud ────────────────────────────────────────────────────────────────
        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Projectfiche", first: true));
                col.Item().PaddingBottom(16).Element(Fiche);
                col.Item().Element(x => SectionLabel(x, _m.HasBasisakte ? "Eenheden & aandelen" : "Eenheden & kopers"));
                col.Item().Element(UnitsTable);
            });
        }

        private void Fiche(IContainer c)
        {
            var verkochtPct = _m.MainCount > 0
                ? $"{Math.Round(_m.SoldMainCount * 100m / _m.MainCount)} %"
                : null;

            var boxes = new List<(string Label, string Value, string Small)>
            {
                ("Werf / adres", string.IsNullOrWhiteSpace(_p.AddressLine) ? _m.ProjectName : _p.AddressLine, _p.CityLine),
                ("Bouwheer", _p.OpdrachtgeverName, _p.OpdrachtgeverAddress),
                ("Eenheden", $"{_m.MainCount} + {_m.SecondaryCount}", $"{_m.MainCount} hoofdeenheden · {_m.SecondaryCount} bergingen en parkings"),
                ("Verkoopwaarde", Euro(_m.SalesValueTotal), "bij afgewerkte oplevering"),
                ("Verkocht", $"{_m.SoldMainCount} / {_m.MainCount}", verkochtPct is null ? null : $"hoofdeenheden · {verkochtPct}"),
                (
                    _m.HasBasisakte ? "Basisakte" : "Grondoppervlakte",
                    _m.HasBasisakte ? $"{Num(_m.LandshareAssigned)} / {Num(_m.LandshareTotal)}" : M2(_m.GroundSurfaceTotal),
                    _m.HasBasisakte
                        ? (_m.LandshareAssigned == 0m ? "nog niet verdeeld over de eenheden"
                            : (_m.LandshareAssigned == _m.LandshareTotal ? "volledig verdeeld" : "wijkt af van het projecttotaal"))
                        : $"{_m.ParcelCount} percelen · volgens kadaster"
                ),
            };

            FicheGrid(c, boxes.ToArray(), columns: 3);
        }

        // ── Eenhedentabel ─────────────────────────────────────────────────────────
        private void UnitsTable(IContainer c)
        {
            if (_m.AllRows.Count == 0)
            {
                c.Text("Er zijn nog geen eenheden ingesteld voor dit project.").Italic().FontColor(Muted);
                return;
            }

            c.Border(0.75f).BorderColor(BorderCol).Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(3.0f);   // Eenheid (naam + adres/kadaster)
                    d.RelativeColumn(1.7f);   // Type
                    d.RelativeColumn(1.1f);   // Verdieping
                    d.RelativeColumn(1.3f);   // Oppervlakte
                    d.RelativeColumn(1.7f);   // Verkoopprijs
                    d.RelativeColumn(1.7f);   // Aandeel of Koper
                    d.RelativeColumn(1.2f);   // Status
                });

                t.Header(h =>
                {
                    void Head(string text, bool right = false)
                    {
                        var cell = h.Cell().Background(Green900).PaddingVertical(6).PaddingHorizontal(7);
                        var txt = cell.Text(text.ToUpperInvariant()).FontSize(6.4f).Bold().FontColor("#ffffff").LetterSpacing(0.14f);
                        if (right) txt.AlignRight();
                    }
                    Head("Eenheid");
                    Head("Type");
                    Head("Verdieping");
                    Head("Oppervlakte", right: true);
                    Head("Verkoopprijs", right: true);
                    Head(_m.HasBasisakte ? "Aandeel" : "Koper", right: _m.HasBasisakte);
                    Head("Status");
                });

                var zebra = false;
                foreach (var group in _m.Groups)
                {
                    // Bandrij per groep — zelfde rol als de groepskop op het scherm.
                    t.Cell().ColumnSpan(7)
                        .Background(GreenSoft).BorderTop(1).BorderBottom(1).BorderColor(GrpLine)
                        .PaddingVertical(4).PaddingHorizontal(7)
                        .Text($"{group.Label.ToUpperInvariant()}   ·   {group.TotalRowCount}")
                        .FontSize(6.8f).Bold().FontColor(Green900).LetterSpacing(0.16f);
                    zebra = false;

                    foreach (var top in group.Rows)
                    {
                        foreach (var r in new[] { top }.Concat(top.Children))
                        {
                            zebra = !zebra;
                            var bg = zebra ? "#ffffff" : Zebra;

                            IContainer Cell() => t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(RowLine)
                                                  .PaddingVertical(4).PaddingHorizontal(7);

                            // Eenheid: ingesprongen mét een liggend streepje wanneer ze aan een lot hangt —
                            // een PDF heeft geen hover of kleurvlak om dat anders te laten zien.
                            Cell().Column(col =>
                            {
                                col.Item().Text(text =>
                                {
                                    if (r.IsAttached)
                                        text.Span("└  ").FontSize(7.4f).FontColor(Sand);
                                    text.Span(r.Name).FontSize(7.8f).SemiBold()
                                        .FontColor(r.IsAttached ? Ink : Green700);
                                    if (r.IsLink)
                                        text.Span("   (koppeling)").FontSize(6.6f).FontColor(Muted);
                                });
                                if (!string.IsNullOrWhiteSpace(r.SubLine))
                                    col.Item().PaddingTop(1).Text(r.SubLine).FontSize(6.6f).FontColor(Muted).WrapAnywhere();
                            });

                            Cell().Column(col =>
                            {
                                col.Item().Text(r.TypeGroupLabel).FontSize(7.4f).FontColor(Ink);
                                if (!string.IsNullOrWhiteSpace(r.TypeName))
                                    col.Item().Text(r.TypeName).FontSize(6.6f).FontColor(Muted);
                            });

                            Cell().Text(r.LevelLabel).FontSize(7.4f).FontColor(Ink);

                            Cell().Column(col =>
                            {
                                col.Item().AlignRight().Text(M2(r.Surface)).FontSize(7.4f)
                                    .FontColor((r.Surface ?? 0m) > 0m ? Ink : No);
                                if ((r.GroundSurface ?? 0m) > 0m)
                                    col.Item().AlignRight().Text($"grond {Num(r.GroundSurface.Value)} m²").FontSize(6.6f).FontColor(Muted);
                            });

                            Cell().Column(col =>
                            {
                                if (r.PriceFrom.HasValue)
                                {
                                    col.Item().AlignRight().Text($"vanaf {Euro(r.PriceFrom.Value)}").FontSize(7.4f).SemiBold().FontColor(Heading);
                                    col.Item().AlignRight().Text($"afgewerkt {Euro(r.PriceFinished.Value)}").FontSize(6.6f).FontColor(Muted);
                                }
                                else
                                {
                                    col.Item().AlignRight().Text(Euro(r.Price)).FontSize(7.4f).SemiBold().FontColor(Heading);
                                    if (r.AttachedCount > 0)
                                        col.Item().AlignRight().Text($"{Euro(r.OwnPrice)} + {r.AttachedCount} gekoppeld").FontSize(6.6f).FontColor(Muted);
                                }
                            });

                            if (_m.HasBasisakte)
                            {
                                var share = r.Landshare ?? 0m;
                                Cell().AlignRight().Text(Num(share)).FontSize(7.4f)
                                    .FontColor(share > 0m ? Ink : No);
                            }
                            else if (r.IsAttached)
                            {
                                Cell().Text($"via {r.ParentName}").FontSize(7.4f).FontColor(Muted);
                            }
                            else if (!string.IsNullOrWhiteSpace(r.ClientName))
                            {
                                Cell().Column(col =>
                                {
                                    col.Item().Text(r.ClientName).FontSize(7.4f).SemiBold().FontColor(Green700).WrapAnywhere();
                                    if (r.SalesAgreementDate.HasValue)
                                        col.Item().Text($"compromis {r.SalesAgreementDate.Value.ToString("dd/MM/yyyy", Culture)}").FontSize(6.6f).FontColor(Muted);
                                });
                            }
                            else
                            {
                                Cell().Text("—").FontSize(7.4f).FontColor(No);
                            }

                            Cell().Text(r.Status.ToUpperInvariant()).FontSize(6.6f).SemiBold()
                                .FontColor(r.StatusTone == "is-positive" ? Green700 : Muted).LetterSpacing(0.08f);
                        }
                    }
                }

                // ── Totaalregel ───────────────────────────────────────────────────
                IContainer Foot() => t.Cell().Background(GreenSoft).BorderTop(1).BorderColor(GrpLine)
                                       .PaddingVertical(5).PaddingHorizontal(7);

                Foot().Text($"Totaal {_m.ProjectName}").FontSize(7.6f).Bold().FontColor(Green900);
                Foot().Text("");
                Foot().Text("");
                Foot().AlignRight().Text(t2 =>
                {
                    t2.Span(M2(_m.LivingSurfaceTotal)).FontSize(7.6f).SemiBold().FontColor(Green900);
                    t2.Span("  bewoonbaar").FontSize(6.6f).FontColor(Muted);
                });
                Foot().AlignRight().Text(Euro(_m.SalesValueTotal)).FontSize(7.6f).Bold().FontColor(Green900);
                if (_m.HasBasisakte)
                {
                    Foot().AlignRight().Text($"{Num(_m.LandshareAssigned)} / {Num(_m.LandshareTotal)}")
                        .FontSize(7.6f).Bold()
                        .FontColor(_m.LandshareAssigned == _m.LandshareTotal && _m.LandshareTotal > 0m ? Green900 : Sand);
                }
                else
                {
                    Foot().Text($"{_m.SoldMainCount} van {_m.MainCount} loten verkocht").FontSize(6.8f).FontColor(Muted);
                }
                Foot().Text("");
            });
        }
    }

    /// <summary>De paar projectgegevens die de eenhedenlijst-PDF bovenaan in zijn fiche zet en die niet
    /// uit <see cref="DetailUnitsV2Vm"/> komen — zelfde rol als
    /// <c>ClientListProjectInfo</c> voor de klantenlijst.</summary>
    public sealed class UnitListProjectInfo
    {
        public string AddressLine { get; set; }
        public string CityLine { get; set; }
        public string OpdrachtgeverName { get; set; }
        public string OpdrachtgeverAddress { get; set; }
    }
}
