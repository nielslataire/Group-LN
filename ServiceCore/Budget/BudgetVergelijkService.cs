using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using BOCore;
using BOCore.Budget;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    /// <summary>Eén versie in de vergelijking (design-handoff 39n). Letter A is de basis.</summary>
    public class VergelijkVersie
    {
        public int Id { get; set; }
        public string Letter { get; set; } = "";
        public string BudgetNaam { get; set; } = "";
        public int Nummer { get; set; }
        public string VersieNaam { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsHuidig { get; set; }
        public int? LaatsteStap { get; set; }
        public DateTime Datum { get; set; }
        /// <summary>Alle stappen doorlopen (afgerond, definitief of concept dat stap 9 bereikte): enkel dan zijn verkoop en marge betrouwbaar.</summary>
        public bool Compleet { get; set; }
        public decimal Kostprijs { get; set; }
        public bool HeeftVerkoop { get; set; }
        public decimal Verkoopwaarde { get; set; }
        public decimal Resultaat { get; set; }
        public decimal MargeOpKost { get; set; }
        public decimal Verkoopkosten { get; set; }
        public decimal Gba { get; set; }
        public decimal KostprijsPerM2 => Gba > 0m ? Kostprijs / Gba : 0m;

        public string Label => $"{BudgetNaam} · v{Nummer}{(string.IsNullOrWhiteSpace(VersieNaam) ? "" : " " + VersieNaam)}";
        public string StatusTekst => Status == "Definitief" ? "definitief" : Status == "Afgerond" ? "afgerond" : IsHuidig ? "huidig · concept" : "concept";
    }

    public class VergelijkLijn
    {
        public string Naam { get; set; } = "";
        public Dictionary<int, decimal> Bedragen { get; } = new();
    }

    /// <summary>Hoofdgroep van de kostprijs (grond, een lot uit stap 6, erelonen, …) met haar activiteiten/posten.</summary>
    public class VergelijkGroep
    {
        public string Titel { get; set; } = "";
        public string Sub { get; set; } = "";
        public Dictionary<int, decimal> Bedragen { get; } = new();
        public List<VergelijkLijn> Lijnen { get; } = new();
    }

    public class VergelijkEenheid
    {
        public string Naam { get; set; } = "";
        public string Type { get; set; } = "";
        public Dictionary<int, decimal> Gba { get; } = new();
        public Dictionary<int, decimal> Vraagprijs { get; } = new();
    }

    public class VergelijkParameter
    {
        public string Label { get; set; } = "";
        public Dictionary<int, string> Waarden { get; } = new();
    }

    public class BudgetVergelijking
    {
        public List<VergelijkVersie> Versies { get; set; } = new();
        public List<VergelijkGroep> Groepen { get; set; } = new();
        /// <summary>Alleen kosten per activiteit: geen verkoop, marge of verkooptabel.</summary>
        public bool EnkelKosten { get; set; }
        public List<VergelijkEenheid> Eenheden { get; set; } = new();
        public List<VergelijkParameter> Parameters { get; set; } = new();
        public Dictionary<int, string> CommissieTekst { get; set; } = new();
        public bool HeeftVerkoop => !EnkelKosten && Versies.Any(v => v.HeeftVerkoop);
    }

    /// <summary>
    /// Vergelijkt 2 of 3 budgetversies van een project (design-handoff 39m/39n): kerncijfers, kostprijs per hoofdgroep met activiteiten, verkoop per eenheid
    /// en de parameters die verschillen. Eerste versie = basis. Gebruikt dezelfde berekeningen als de resultaatpagina, dus de cijfers lopen nooit uiteen.
    /// Een concept wordt vergeleken met wat al ingevuld is; verkoop en marge blijven leeg zolang niet alle stappen doorlopen zijn.
    /// </summary>
    public class BudgetVergelijkService
    {
        private static readonly CultureInfo NlBe = CultureInfo.GetCultureInfo("nl-BE");
        private readonly UnitOfWorkCore _uow;
        private readonly BudgetBerekeningService _berekening;
        private readonly BudgetActivityService _activiteiten;
        private readonly IVerkoopVoorstelService _voorstel;

        public BudgetVergelijkService(UnitOfWorkCore uow, BudgetBerekeningService berekening, BudgetActivityService activiteiten, IVerkoopVoorstelService voorstel)
        {
            _uow = uow; _berekening = berekening; _activiteiten = activiteiten; _voorstel = voorstel;
        }

        public static bool IsCompleet(string? status, int? laatsteStap)
            => status == BudgetVersie.StatusDefinitief || status == BudgetVersie.StatusAfgerond || laatsteStap >= 9;

        public async Task<BudgetVergelijking> VergelijkAsync(IReadOnlyList<int> versieIds, bool enkelKosten)
        {
            var uit = new BudgetVergelijking { EnkelKosten = enkelKosten };
            var groepen = new Dictionary<string, VergelijkGroep>();
            var volgorde = new List<string>();
            var eenhedenIdx = new Dictionary<string, VergelijkEenheid>(StringComparer.OrdinalIgnoreCase);
            var paramRijen = new List<(string Label, Dictionary<int, decimal?> Ruw, Func<decimal?, string> Fmt)>();
            var letters = "ABC";

            void Groep(int vid, string titel, string sub, decimal bedrag, IEnumerable<(string Naam, decimal Bedrag)> lijnen)
            {
                if (!groepen.TryGetValue(titel, out var g)) { g = new VergelijkGroep { Titel = titel, Sub = sub }; groepen[titel] = g; volgorde.Add(titel); }
                if (string.IsNullOrEmpty(g.Sub)) g.Sub = sub;
                g.Bedragen[vid] = bedrag;
                foreach (var (naam, b) in lijnen)
                {
                    var l = g.Lijnen.FirstOrDefault(x => x.Naam == naam);
                    if (l == null) { l = new VergelijkLijn { Naam = naam }; g.Lijnen.Add(l); }
                    l.Bedragen[vid] = (l.Bedragen.TryGetValue(vid, out var bestaand) ? bestaand : 0m) + b;
                }
            }
            void Param(string label, int vid, decimal? waarde, Func<decimal?, string> fmt)
            {
                var rij = paramRijen.FirstOrDefault(x => x.Label == label);
                if (rij.Label == null) { rij = (label, new Dictionary<int, decimal?>(), fmt); paramRijen.Add(rij); }
                rij.Ruw[vid] = waarde;
            }

            var i = 0;
            foreach (var id in versieIds)
            {
                var versie = await _uow.BudgetVersies.GetNoTracking().Include(v => v.BudgetMaster).Include(v => v.BudgetGegevens).FirstOrDefaultAsync(v => v.Id == id);
                if (versie == null) continue;
                var r = await _berekening.BerekenAsync(id);
                var p = await _berekening.GetOrCreateParamsAsync(id);
                var lots = await _activiteiten.GetLotGroepenAsync(id);
                var oppResp = _uow.BudgetOppervlaktes.GetNoTracking().Where(o => o.BudgetVersieId == id).ToList();
                var gba = r.TotaalGBA > 0m ? r.TotaalGBA : oppResp.Sum(o => o.BewoonbareOpp);

                var v = new VergelijkVersie
                {
                    Id = id, Letter = letters[Math.Min(i, 2)].ToString(), BudgetNaam = versie.BudgetMaster?.Naam ?? "", Nummer = versie.Versienummer, VersieNaam = versie.VersieNaam ?? "",
                    Status = versie.Status ?? "", IsHuidig = versie.IsHuidig, LaatsteStap = versie.LaatsteStap, Datum = versie.GewijzigdOp ?? versie.CreatedAt,
                    Compleet = IsCompleet(versie.Status, versie.LaatsteStap), Kostprijs = r.TotaalKosten, Gba = gba,
                };

                // ── Verkoop en marge (enkel als alle stappen doorlopen zijn) ──
                if (!enkelKosten && v.Compleet)
                {
                    try
                    {
                        var voorstel = await _voorstel.BerekenAsync(id);
                        var sam = await _voorstel.SamenvattingAsync(id);
                        var lijnen = await _voorstel.HerberekendeLijnenAsync(id);
                        var vr = VerkoopResultaat.Bereken(sam, voorstel, lijnen, p.MakelaarsCommissiePerc ?? 0m, p.OverigeVerkoopkosten ?? 0m, r.TotaalKosten);
                        if (vr.HeeftVerkoop)
                        {
                            v.HeeftVerkoop = true; v.Verkoopwaarde = vr.Verkoopwaarde; v.Resultaat = vr.Resultaat; v.MargeOpKost = vr.MargeOpKostprijs; v.Verkoopkosten = vr.Verkoopkosten;
                            uit.CommissieTekst[id] = (vr.CommissiePerc * 100m).ToString("0.##", NlBe) + " %";
                            var eenh = voorstel.Eenheden.ToDictionary(e => (e.EenheidNaam ?? "").Trim(), e => e, StringComparer.OrdinalIgnoreCase);
                            foreach (var l in lijnen.Where(l => l.Vraagprijs is > 0m))
                            {
                                var naam = (l.EenheidNaam ?? "").Trim();
                                if (!eenhedenIdx.TryGetValue(naam, out var e)) { e = new VergelijkEenheid { Naam = naam }; eenhedenIdx[naam] = e; }
                                eenh.TryGetValue(naam, out var vo);
                                if (vo != null && string.IsNullOrEmpty(e.Type)) e.Type = string.IsNullOrWhiteSpace(vo.TypeName) ? (vo.GroupTypeName ?? "") : vo.TypeName;
                                e.Vraagprijs[id] = l.Vraagprijs!.Value;
                                e.Gba[id] = vo?.BewoonbareOpp ?? 0m;
                            }
                        }
                    }
                    catch { /* verkoop is aanvullend: bij een fout blijft die leeg */ }
                }
                uit.Versies.Add(v);

                // ── Kostprijs per hoofdgroep ──
                var grond = r.Grond.Where(k => k.Bedrag != 0m).ToList();
                Groep(id, "Grondkost", "aankoop + registratie + notaris", grond.Sum(k => k.Bedrag), grond.Select(k => (k.Omschrijving ?? "", k.Bedrag)));
                foreach (var lot in lots.OrderBy(l => l.LotNummer))
                {
                    var lijnen = lot.Lijnen.Select(l => (Naam: l.ActivityOmschrijving ?? "", Bedrag: l.TotaalAlternatief * (l.Correctiefactor < 0m ? 1m : l.Correctiefactor))).Where(x => x.Bedrag > 0m).ToList();
                    if (lijnen.Count == 0) continue;
                    Groep(id, lot.LotNaam ?? $"Lot {lot.LotNummer}", "", lijnen.Sum(x => x.Bedrag), lijnen);
                }
                bool IsVerz(BudgetKostenPostBO k) => (k.Omschrijving ?? "").StartsWith("Decennale", StringComparison.OrdinalIgnoreCase) || (k.Omschrijving ?? "").StartsWith("ABR", StringComparison.OrdinalIgnoreCase);
                var erelonen = r.KostenOpBouw.Where(k => !IsVerz(k) && k.Bedrag != 0m).ToList();
                var verz = r.KostenOpBouw.Where(k => IsVerz(k) && k.Bedrag != 0m).ToList();
                var forfaits = r.Forfaits.Where(f => f.Bedrag != 0m && !string.Equals(f.Omschrijving, "Publiciteit", StringComparison.OrdinalIgnoreCase)).ToList();
                var publ = r.Forfaits.Where(f => string.Equals(f.Omschrijving, "Publiciteit", StringComparison.OrdinalIgnoreCase)).Sum(f => f.Bedrag);
                Groep(id, "Erelonen & studies", "", erelonen.Sum(k => k.Bedrag), erelonen.Select(k => (k.Omschrijving ?? "", k.Bedrag)));
                Groep(id, "Verzekeringen", "decennale + ABR", verz.Sum(k => k.Bedrag), verz.Select(k => (k.Omschrijving ?? "", k.Bedrag)));
                Groep(id, "Aansluitingen & liften", "", forfaits.Sum(f => f.Bedrag), forfaits.Select(f => (f.Omschrijving ?? "", f.Bedrag)));
                var fin = r.Financiering.Where(f => f.Bedrag != 0m).ToList();
                Groep(id, "Financiering", "Wet Breyne, straight loan", fin.Sum(f => f.Bedrag), fin.Select(f => (f.Omschrijving ?? "", f.Bedrag)));
                var reserves = new List<(string, decimal)>(); if (r.Onvoorzien != 0m) reserves.Add(("Onvoorzien", r.Onvoorzien)); if (publ != 0m) reserves.Add(("Publiciteit", publ));
                Groep(id, "Reserves", "onvoorzien, publiciteit", reserves.Sum(x => x.Item2), reserves);

                // ── Parameters ──
                var g0 = versie.BudgetGegevens;
                string Idx(decimal? x) => x.HasValue ? x.Value.ToString("0.0000", NlBe) : "—";
                string Pct(decimal? x) => x.HasValue ? (x.Value * 100m).ToString("0.##", NlBe) + " %" : "—";
                string Eur(decimal? x) => x.HasValue ? "€ " + x.Value.ToString("N0", NlBe) : "—";
                Param("S-index", id, g0?.SIndexHuidig, Idx); Param("I-index", id, g0?.IIndexHuidig, Idx);
                Param("Wet Breyne", id, p.WetBreynePerc, Pct); Param("Straight loan gebouw", id, p.StraightloanGebouwPerc, Pct); Param("Straight loan grond", id, p.StraightloanGrondPerc, Pct);
                Param("Onvoorzien", id, p.OnvoorzienPerc, Pct); Param("Projectcoördinatie", id, p.ProjectcoordinatiePerc, Pct); Param("Architect", id, p.ArchitectPerc, Pct);
                Param("Ingenieur", id, p.StudieIRPerc, Pct); Param("Decennale", id, p.DecennaleGeslRuwbouwPerc, Pct); Param("Registratierechten", id, p.RegistratierechtenPerc, Pct);
                Param("Publiciteit", id, p.PubliciteitPerc, Pct); Param("Doelmarge", id, p.DoelMargePerc, Pct); Param("Marge op grond", id, p.GrondMargePerc, Pct);
                Param("Makelaarscommissie", id, p.MakelaarsCommissiePerc, Pct); Param("Overige verkoopkosten", id, p.OverigeVerkoopkosten, Eur); Param("Aankoopprijs grond", id, p.AankoopprijsGrond, Eur);
                i++;
            }

            uit.Groepen = volgorde.Select(t => groepen[t]).Where(g => g.Bedragen.Values.Any(b => b != 0m) || g.Lijnen.Count > 0).ToList();
            uit.Eenheden = eenhedenIdx.Values.OrderBy(e => e.Naam, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var (label, ruw, fmt) in paramRijen)
            {
                var waarden = uit.Versies.Select(v => ruw.TryGetValue(v.Id, out var x) ? x : null).ToList();
                if (waarden.Select(x => x ?? -1m).Distinct().Count() <= 1) continue;   // enkel wat verschilt
                var pr = new VergelijkParameter { Label = label };
                foreach (var v in uit.Versies) pr.Waarden[v.Id] = fmt(ruw.TryGetValue(v.Id, out var x) ? x : null);
                uit.Parameters.Add(pr);
            }
            return uit;
        }

        /// <summary>Excel-export van de vergelijking (knop "Exporteren"): kerncijfers, kostprijs per hoofdgroep met activiteiten, verkoop per eenheid en gewijzigde parameters.</summary>
        public static byte[] NaarExcel(BudgetVergelijking v, string projectNaam)
        {
            using var wb = new XLWorkbook();
            var ids = v.Versies.Select(x => x.Id).ToList();
            var basis = ids.FirstOrDefault();
            string Koptekst(VergelijkVersie x) => $"{x.Letter} · {x.Label} ({x.StatusTekst})";

            void Kop(IXLWorksheet ws, int rij, params string[] titels)
            {
                for (var c = 0; c < titels.Length; c++)
                {
                    var cel = ws.Cell(rij, c + 1); cel.Value = titels[c];
                    cel.Style.Font.Bold = true; cel.Style.Font.FontColor = XLColor.White; cel.Style.Fill.BackgroundColor = XLColor.FromHtml("#00532D");
                    if (c > 0) cel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
            }
            void Euro(IXLCell c, decimal? w) { if (w.HasValue) { c.Value = w.Value; c.Style.NumberFormat.Format = "#,##0;-#,##0"; } else c.Value = "—"; c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; }
            void Verschil(IXLWorksheet ws, int rij, int kol, decimal? a, decimal? b)
            {
                if (a.HasValue && b.HasValue) { ws.Cell(rij, kol).Value = b.Value - a.Value; ws.Cell(rij, kol).Style.NumberFormat.Format = "+#,##0;-#,##0;0"; if (a.Value != 0m) { ws.Cell(rij, kol + 1).Value = (b.Value - a.Value) / a.Value; ws.Cell(rij, kol + 1).Style.NumberFormat.Format = "+0.0%;-0.0%;0.0%"; } }
            }

            // Tabblad 1: kerncijfers
            var k = wb.Worksheets.Add("Kerncijfers");
            k.Cell(1, 1).Value = $"Budgetten vergelijken · {projectNaam}"; k.Cell(1, 1).Style.Font.Bold = true; k.Cell(1, 1).Style.Font.FontSize = 14;
            Kop(k, 3, new[] { "" }.Concat(v.Versies.Select(Koptekst)).ToArray());
            var rij = 4;
            void KernRij(string label, Func<VergelijkVersie, decimal?> f, string format = "#,##0")
            {
                k.Cell(rij, 1).Value = label;
                for (var c = 0; c < v.Versies.Count; c++) { var w = f(v.Versies[c]); var cel = k.Cell(rij, c + 2); if (w.HasValue) { cel.Value = w.Value; cel.Style.NumberFormat.Format = format; } else cel.Value = "—"; cel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; }
                rij++;
            }
            KernRij("Totale kostprijs", x => x.Kostprijs);
            if (!v.EnkelKosten)
            {
                KernRij("Verkoopprijs", x => x.HeeftVerkoop ? x.Verkoopwaarde : null);
                KernRij("Verkoopkosten", x => x.HeeftVerkoop ? x.Verkoopkosten : null);
                KernRij("Resultaat", x => x.HeeftVerkoop ? x.Resultaat : null);
                KernRij("Marge op kostprijs", x => x.HeeftVerkoop ? x.MargeOpKost : null, "0.0%");
            }
            KernRij("Kostprijs per m² GBA", x => x.Gba > 0m ? x.KostprijsPerM2 : null);
            k.Columns().AdjustToContents();

            // Tabblad 2: kostprijs per hoofdgroep
            var g = wb.Worksheets.Add("Kostprijs per hoofdgroep");
            var kolommen = new List<string> { "Hoofdgroep · activiteit" }; kolommen.AddRange(v.Versies.Select(x => $"{x.Letter} · v{x.Nummer}"));
            for (var c = 1; c < v.Versies.Count; c++) { kolommen.Add($"Verschil {v.Versies[c].Letter}"); kolommen.Add($"% {v.Versies[c].Letter}"); }
            Kop(g, 1, kolommen.ToArray());
            rij = 2;
            void Rij(string naam, Func<int, decimal?> w, bool vet, bool inspring)
            {
                g.Cell(rij, 1).Value = (inspring ? "    " : "") + naam; g.Cell(rij, 1).Style.Font.Bold = vet;
                for (var c = 0; c < v.Versies.Count; c++) { Euro(g.Cell(rij, c + 2), w(v.Versies[c].Id)); g.Cell(rij, c + 2).Style.Font.Bold = vet; }
                for (var c = 1; c < v.Versies.Count; c++) Verschil(g, rij, v.Versies.Count + 2 + (c - 1) * 2, w(basis), w(v.Versies[c].Id));
                rij++;
            }
            foreach (var gr in v.Groepen)
            {
                Rij(gr.Titel, id => gr.Bedragen.TryGetValue(id, out var b) ? b : null, true, false);
                foreach (var l in gr.Lijnen) Rij(l.Naam, id => l.Bedragen.TryGetValue(id, out var b) ? b : 0m, false, true);
            }
            Rij("Totale kostprijs", id => v.Versies.First(x => x.Id == id).Kostprijs, true, false);
            g.Columns().AdjustToContents();

            // Tabblad 3: verkoop per eenheid
            if (v.HeeftVerkoop)
            {
                var e = wb.Worksheets.Add("Verkoop per eenheid");
                var kk = new List<string> { "Eenheid", "Type", "GBA m²" }; kk.AddRange(v.Versies.Select(x => $"{x.Letter} · v{x.Nummer}"));
                for (var c = 1; c < v.Versies.Count; c++) { kk.Add($"Verschil {v.Versies[c].Letter}"); kk.Add($"% {v.Versies[c].Letter}"); }
                Kop(e, 1, kk.ToArray());
                rij = 2;
                foreach (var u in v.Eenheden)
                {
                    e.Cell(rij, 1).Value = u.Naam; e.Cell(rij, 2).Value = u.Type;
                    var gba = u.Gba.Values.FirstOrDefault(); if (gba > 0m) { e.Cell(rij, 3).Value = gba; e.Cell(rij, 3).Style.NumberFormat.Format = "#,##0"; }
                    for (var c = 0; c < v.Versies.Count; c++) Euro(e.Cell(rij, c + 4), u.Vraagprijs.TryGetValue(v.Versies[c].Id, out var w) ? w : null);
                    for (var c = 1; c < v.Versies.Count; c++) Verschil(e, rij, v.Versies.Count + 4 + (c - 1) * 2, u.Vraagprijs.TryGetValue(basis, out var a) ? a : null, u.Vraagprijs.TryGetValue(v.Versies[c].Id, out var b) ? b : null);
                    rij++;
                }
                e.Cell(rij, 1).Value = "Totale verkoop"; e.Cell(rij, 1).Style.Font.Bold = true;
                for (var c = 0; c < v.Versies.Count; c++) { Euro(e.Cell(rij, c + 4), v.Versies[c].HeeftVerkoop ? v.Versies[c].Verkoopwaarde : null); e.Cell(rij, c + 4).Style.Font.Bold = true; }
                e.Columns().AdjustToContents();
            }

            // Tabblad 4: gewijzigde parameters
            if (v.Parameters.Count > 0)
            {
                var p = wb.Worksheets.Add("Gewijzigde parameters");
                Kop(p, 1, new[] { "Parameter" }.Concat(v.Versies.Select(x => $"{x.Letter} · v{x.Nummer}")).ToArray());
                rij = 2;
                foreach (var pr in v.Parameters)
                {
                    p.Cell(rij, 1).Value = pr.Label;
                    for (var c = 0; c < v.Versies.Count; c++) { p.Cell(rij, c + 2).Value = pr.Waarden.TryGetValue(v.Versies[c].Id, out var t) ? t : "—"; p.Cell(rij, c + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; }
                    rij++;
                }
                p.Columns().AdjustToContents();
            }

            using var ms = new System.IO.MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
