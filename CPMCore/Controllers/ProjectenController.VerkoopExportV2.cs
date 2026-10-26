using CPMCore.Services.Verkoop;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 45g): Te koop exporteren naar Excel. Tabbladen: Eenheden (altijd), Berekening per eenheid, Kosten koper. Bedragen staan als getallen, om verder te rekenen.
public partial class ProjectenController
{
    private const string XlsxGroen = "#00532D";
    private const string XlsxEuro = "€ #,##0.00;[Red]-€ #,##0.00";

    /// <summary>45g — Excel-export van de eenheden. <paramref name="ids"/> = enkel de gekozen eenheden (leeg = alle).</summary>
    [HttpGet]
    public async Task<IActionResult> SalesExportXlsxV2(int projectid, string? ids, bool prekad = true, bool koper = true, bool totaalKoper = true, bool berekening = true, bool kosten = true)
    {
        var vm = await Verkoop.GetIndexAsync(projectid);
        var gekozen = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToHashSet();
        var rijen = gekozen.Count == 0 ? vm.Rijen : vm.Rijen.Where(r => gekozen.Contains(r.UnitId)).ToList();
        if (rijen.Count == 0) { AddMessage("error", "Er zijn geen eenheden om te exporteren.", "Fout"); return RedirectToAction("DetailSales", new { projectid }); }

        using var wb = new XLWorkbook();

        // ── Eenheden ──
        var ws = wb.AddWorksheet("Eenheden");
        var kop = new List<(string Titel, bool Bedrag, Func<VerkoopRijVm, XLCellValue> Waarde)>
        {
            ("Eenheid", false, r => r.Naam), ("Type", false, r => r.TypeNaam), ("Verdieping", false, r => r.Verdieping), ("Status", false, r => r.Status),
            ("Perceel (m²)", false, r => Num(r.Perceel)), ("Wonen (m²)", false, r => Num(r.Wonen)), ("Slaapkamers", false, r => r.Slaapkamers),
            ("Grondwaarde", true, r => r.Grond), ("Bouwwaarde", true, r => r.Bouw), ("Totaalprijs", true, r => r.Totaal),
        };
        if (prekad) kop.Insert(4, ("Prekad", false, r => r.PreKad));
        if (totaalKoper) kop.Add(("Totaal voor de koper", true, r => r.Koper.Totaal));
        if (koper) { kop.Add(("Koper", false, r => r.KoperNaam ?? "")); kop.Add(("Compromis", false, r => Dat(r.CompromisDatum))); kop.Add(("Akte", false, r => Dat(r.AkteDatum))); }
        kop.Add(("Optie tot", false, r => Dat(r.OptionUntil)));
        kop.Add(("Optant", false, r => r.OptionHolder ?? ""));
        kop.Add(("Via", false, r => r.SaleVia ?? ""));
        kop.Add(("Online sinds", false, r => Dat(r.OnlineSince)));
        kop.Add(("Geïnteresseerden", false, r => r.Interessenten));
        for (var i = 0; i < kop.Count; i++) Kop(ws.Cell(1, i + 1), kop[i].Titel);
        var rij = 2;
        foreach (var r in rijen)
        {
            for (var i = 0; i < kop.Count; i++)
            {
                var c = ws.Cell(rij, i + 1); c.Value = kop[i].Waarde(r);
                if (kop[i].Bedrag) c.Style.NumberFormat.Format = XlsxEuro;
                else if (kop[i].Titel is "Compromis" or "Akte" or "Optie tot" or "Online sinds") c.Style.DateFormat.Format = "dd/MM/yyyy";
            }
            rij++;
        }
        // totaalrij met formules over de bedragkolommen
        ws.Cell(rij, 1).Value = "Totaal"; ws.Row(rij).Style.Font.Bold = true;
        for (var i = 0; i < kop.Count; i++)
            if (kop[i].Bedrag) { var c = ws.Cell(rij, i + 1); c.FormulaA1 = $"SUM({ws.Cell(2, i + 1).Address}:{ws.Cell(rij - 1, i + 1).Address})"; c.Style.NumberFormat.Format = XlsxEuro; }
        ws.Range(rij, 1, rij, kop.Count).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);

        // ── Berekening per eenheid / Kosten koper ──
        if (berekening || kosten)
        {
            var inst = await Verkoop.GetInstellingenAsync(projectid);
            if (berekening)
            {
                var wb2 = wb.AddWorksheet("Berekening per eenheid");
                string[] k2 = { "Eenheid", "Status", "Onderdeel", "Post", "Toelichting", "Bedrag" };
                for (var i = 0; i < k2.Length; i++) Kop(wb2.Cell(1, i + 1), k2[i]);
                var r2 = 2;
                void Post(VerkoopRijVm r, string onderdeel, string post, string? toelichting, decimal bedrag, bool vet = false)
                {
                    wb2.Cell(r2, 1).Value = r.Naam; wb2.Cell(r2, 2).Value = r.Status; wb2.Cell(r2, 3).Value = onderdeel; wb2.Cell(r2, 4).Value = post; wb2.Cell(r2, 5).Value = toelichting ?? "";
                    var c = wb2.Cell(r2, 6); c.Value = bedrag; c.Style.NumberFormat.Format = XlsxEuro;
                    if (vet) wb2.Row(r2).Style.Font.Bold = true;
                    r2++;
                }
                foreach (var r in rijen)
                {
                    var k = r.Koper;
                    Post(r, "Grond", "Grondwaarde", r.Perceel > 0m ? $"{r.Perceel:N0} m² × € {k.Grond / r.Perceel!.Value:N2}" : null, k.Grond);
                    var opWat = k.Regime == VerkoopRegime.Gemengd ? "op de grondwaarde" : "op grond en bouw";
                    if (k.Registratierechten > 0m) Post(r, "Grond", $"Registratierechten {k.RegistratiePercentage:0.##} %", opWat, k.Registratierechten);
                    Post(r, "Constructie", "Bouwwaarde excl. btw", null, k.Bouw);
                    if (k.Btw > 0m) Post(r, "Constructie", $"Btw {k.BtwPercentage:0.##} %", k.Regime == VerkoopRegime.Gemengd ? "op de bouwwaarde" : "op grond en bouw", k.Btw);
                    foreach (var p in KostenPosten(k)) Post(r, "Kosten koper", p.Post, p.Toelichting, p.Bedrag);
                    Post(r, "Totaal", "Vraagprijs (grond + bouw excl. btw)", null, k.Vraagprijs, true);
                    Post(r, "Totaal", "Totaal voor de koper", null, k.Totaal, true);
                }
                wb2.Columns().AdjustToContents(); wb2.SheetView.FreezeRows(1);
            }
            if (kosten)
            {
                var wk = wb.AddWorksheet("Kosten koper");
                string[] k3 = { "Eenheid", "Groep", "Post", "Toelichting", "Bedrag" };
                for (var i = 0; i < k3.Length; i++) Kop(wk.Cell(1, i + 1), k3[i]);
                var r3 = 2;
                foreach (var r in rijen)
                {
                    foreach (var p in KostenPosten(r.Koper))
                    {
                        wk.Cell(r3, 1).Value = r.Naam; wk.Cell(r3, 2).Value = p.Groep; wk.Cell(r3, 3).Value = p.Post; wk.Cell(r3, 4).Value = p.Toelichting ?? "";
                        var c = wk.Cell(r3, 5); c.Value = p.Bedrag; c.Style.NumberFormat.Format = XlsxEuro; r3++;
                    }
                }
                wk.Cell(r3, 1).Value = "Totaal"; wk.Row(r3).Style.Font.Bold = true;
                var tc = wk.Cell(r3, 5); tc.FormulaA1 = $"SUM({wk.Cell(2, 5).Address}:{wk.Cell(r3 - 1, 5).Address})"; tc.Style.NumberFormat.Format = XlsxEuro;
                wk.Range(r3, 1, r3, 5).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                wk.Columns().AdjustToContents(); wk.SheetView.FreezeRows(1);
            }
        }

        using var ms = new MemoryStream(); wb.SaveAs(ms);
        var proj = string.Concat(vm.ProjectName.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')).Trim().Replace(' ', '_');
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Verkoop_{proj}_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private static XLCellValue Num(decimal? v) => v.HasValue ? v.Value : Blank.Value;
    private static XLCellValue Dat(DateOnly? d) => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : Blank.Value;

    private static void Kop(IXLCell cell, string titel)
    {
        cell.Value = titel; cell.Style.Font.Bold = true; cell.Style.Font.FontColor = XLColor.White; cell.Style.Fill.BackgroundColor = XLColor.FromHtml(XlsxGroen);
    }

    /// <summary>De kosten koper van één eenheid als losse posten: de eigen kosten, de btw erop en de notaris opgesplitst (ereloon, administratieve kosten, enz.).</summary>
    private static List<(string Groep, string Post, string? Toelichting, decimal Bedrag)> KostenPosten(KoperBerekening k)
    {
        var l = new List<(string, string, string?, decimal)>();
        foreach (var p in k.Posten) l.Add(("Kosten koper", p.Naam, p.Toelichting, p.Bedrag));
        if (k.KostenBtw > 0m) l.Add(("Kosten koper", "Btw op de kosten", null, k.KostenBtw));
        var n = k.Notaris;
        if (n.Totaal > 0m)
        {
            l.Add(("Notaris", "Ereloon notaris", $"schaal {n.Schaal} op € {n.Grondslag:N0}", n.Ereloon));
            l.Add(("Notaris", "Administratieve kosten", "vacaties (art. 2 §2)", n.Vacaties));
            l.Add(("Notaris", "Uitgaven aan derden", null, n.Uitgaven));
            if (n.Overschrijving > 0m) l.Add(("Notaris", "Kosten overschrijving", "hypotheekkantoor", n.Overschrijving));
            l.Add(("Notaris", "Recht op geschriften", null, n.RechtOpGeschriften));
            l.Add(("Notaris", "Registratierecht op bijlagen", null, n.RegistratieBijlagen));
            l.Add(("Notaris", $"Btw {NotarisTarief.Btw:0.##} % op de notariskosten", "ereloon, administratieve kosten, uitgaven aan derden en recht op geschriften", n.Btw));
        }
        return l;
    }
}
