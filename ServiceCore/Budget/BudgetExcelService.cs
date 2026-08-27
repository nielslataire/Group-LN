using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using BOCore.Budget;
using DALCore.Models;

namespace ServiceCore.Budget
{
    public class BudgetExcelService
    {
        public byte[] GenereerBudgetExcel(
            BudgetResultaatBO resultaat,
            BudgetVersie versie,
            List<BudgetActivityLijnen> activiteitLijnen,
            List<Activity> activiteiten,
            List<ActivityGroup> groepen,
            List<BudgetOppervlaktes> oppervlaktes,
            decimal gewogenFactor)
        {
            using var wb = new XLWorkbook();

            // ===== TABBLAD 1: Kostenoverzicht =====
            var ws1 = wb.Worksheets.Add("Kostenoverzicht");

            ws1.Cell(1, 1).Value = versie.BudgetMaster?.Naam ?? "Budget";
            ws1.Cell(1, 1).Style.Font.Bold = true;
            ws1.Cell(1, 1).Style.Font.FontSize = 16;
            ws1.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");

            ws1.Cell(2, 1).Value = $"Versie {versie.Versienummer} — {versie.VersieNaam}";
            ws1.Cell(3, 1).Value = versie.BudgetGegevens?.Adres ?? "";
            ws1.Cell(4, 1).Value = $"Datum: {DateTime.Now:dd/MM/yyyy}";
            ws1.Cell(5, 1).Value = $"S-index: {versie.BudgetGegevens?.SIndexStart:F4} → {versie.BudgetGegevens?.SIndexHuidig:F4}";
            ws1.Cell(6, 1).Value = $"I-index: {versie.BudgetGegevens?.IIndexStart:F4} → {versie.BudgetGegevens?.IIndexHuidig:F4}";
            ws1.Cell(7, 1).Value = $"Gewogen factor: {gewogenFactor:F4}  ((I/I₀)×0.40 + (S/S₀)×0.40 + 0.20)";

            int rij = 9;

            ws1.Cell(rij, 1).Value = "Totale kostprijs";
            ws1.Cell(rij, 2).Value = (double)resultaat.TotaalKosten;
            ws1.Cell(rij, 2).Style.NumberFormat.Format = "€ #,##0";
            ws1.Cell(rij, 2).Style.Font.Bold = true;
            ws1.Cell(rij, 2).Style.Font.FontSize = 14;
            ws1.Cell(rij + 1, 1).Value = "Per eenheid";
            ws1.Cell(rij + 1, 2).Value = (double)resultaat.TotaalPerEenheid;
            ws1.Cell(rij + 1, 2).Style.NumberFormat.Format = "€ #,##0";
            ws1.Cell(rij + 2, 1).Value = "Per m² GBA";
            ws1.Cell(rij + 2, 2).Value = (double)resultaat.TotaalPerM2GBA;
            ws1.Cell(rij + 2, 2).Style.NumberFormat.Format = "€ #,##0";
            ws1.Cell(rij + 3, 1).Value = $"Aantal eenheden: {resultaat.AantalEenheden}   GBA: {resultaat.TotaalGBA:N0} m²";
            ws1.Cell(rij + 3, 1).Style.Font.FontColor = XLColor.Gray;

            rij += 6;

            void SetHeaderRij(int r)
            {
                ws1.Cell(r, 1).Value = "Kostenpost";
                ws1.Cell(r, 2).Value = "Bedrag";
                ws1.Cell(r, 3).Value = "% van totaal";
                ws1.Range(r, 1, r, 3).Style.Font.Bold = true;
                ws1.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#bdd7ff");
            }

            void SetGroepHeader(int r, string label)
            {
                ws1.Cell(r, 1).Value = label;
                ws1.Range(r, 1, r, 3).Merge();
                ws1.Range(r, 1, r, 3).Style.Font.Bold = true;
                ws1.Range(r, 1, r, 3).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");
                ws1.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f0fe");
            }

            void SetKostenRij(int r, string label, decimal bedrag, bool indent = true)
            {
                ws1.Cell(r, 1).Value = (indent ? "    " : "") + label;
                ws1.Cell(r, 2).Value = (double)bedrag;
                ws1.Cell(r, 2).Style.NumberFormat.Format = "€ #,##0";
                var perc = resultaat.TotaalKosten > 0 ? (double)(bedrag / resultaat.TotaalKosten) : 0.0;
                ws1.Cell(r, 3).Value = perc;
                ws1.Cell(r, 3).Style.NumberFormat.Format = "0.0%";
            }

            SetHeaderRij(rij++);
            SetGroepHeader(rij++, "Bouwkost (activiteiten)");
            SetKostenRij(rij++, "Totaal bouwkost", resultaat.TotaalBouw, false);

            if (resultaat.KostenOpBouw.Any())
            {
                SetGroepHeader(rij++, "Erelonen & Studies");
                foreach (var k in resultaat.KostenOpBouw) SetKostenRij(rij++, k.Omschrijving, k.Bedrag);
            }

            if (resultaat.Forfaits.Any(f => f.Bedrag > 0))
            {
                SetGroepHeader(rij++, "Forfaits");
                foreach (var k in resultaat.Forfaits.Where(f => f.Bedrag > 0)) SetKostenRij(rij++, k.Omschrijving, k.Bedrag);
            }

            if (resultaat.Financiering.Any(f => f.Bedrag > 0))
            {
                SetGroepHeader(rij++, "Financiering");
                foreach (var k in resultaat.Financiering.Where(f => f.Bedrag > 0)) SetKostenRij(rij++, k.Omschrijving, k.Bedrag);
            }

            if (resultaat.Onvoorzien > 0)
            {
                SetGroepHeader(rij++, "Onvoorzien");
                SetKostenRij(rij++, "Onvoorzien", resultaat.Onvoorzien);
            }

            ws1.Cell(rij, 1).Value = "TOTALE KOSTPRIJS";
            ws1.Cell(rij, 2).Value = (double)resultaat.TotaalKosten;
            ws1.Cell(rij, 2).Style.NumberFormat.Format = "€ #,##0";
            ws1.Cell(rij, 3).Value = 1.0;
            ws1.Cell(rij, 3).Style.NumberFormat.Format = "0.0%";
            ws1.Range(rij, 1, rij, 3).Style.Font.Bold = true;
            ws1.Range(rij, 1, rij, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#bdd7ff");
            ws1.Range(rij, 1, rij, 3).Style.Font.FontSize = 11;

            ws1.Column(1).Width = 45;
            ws1.Column(2).Width = 20;
            ws1.Column(3).Width = 15;

            // ===== TABBLAD 2: Activiteiten per lot =====
            var ws2 = wb.Worksheets.Add("Activiteiten VMSW");

            ws2.Cell(1, 1).Value = "Activiteiten per lot (VMSW-structuur)";
            ws2.Cell(1, 1).Style.Font.Bold = true;
            ws2.Cell(1, 1).Style.Font.FontSize = 14;
            ws2.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");

            int rij2 = 3;
            var gesorteerdeGroepen = groepen.OrderBy(g => g.Lot).ToList();

            foreach (var grp in gesorteerdeGroepen)
            {
                var lijnen = activiteitLijnen
                    .Where(l => activiteiten.Any(a => a.ActivityId == l.ActivityId && a.GroupId == grp.GroupId))
                    .ToList();
                if (!lijnen.Any()) continue;

                ws2.Cell(rij2, 1).Value = $"Lot {grp.Lot} — {grp.Name}";
                ws2.Range(rij2, 1, rij2, 5).Merge();
                ws2.Range(rij2, 1, rij2, 5).Style.Font.Bold = true;
                ws2.Range(rij2, 1, rij2, 5).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");
                ws2.Range(rij2, 1, rij2, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f0fe");
                rij2++;

                foreach (var (lbl, col) in new[] { ("Activiteit", 1), ("Alt. €/eenh.", 2), ("Alt. totaal", 3), ("Nacalc geïnd.", 4), ("Verschil", 5) })
                {
                    ws2.Cell(rij2, col).Value = lbl;
                    ws2.Cell(rij2, col).Style.Font.Bold = true;
                    ws2.Cell(rij2, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0f4ff");
                }
                rij2++;

                decimal totAlt = 0, totNac = 0;
                foreach (var lijn in lijnen)
                {
                    var act      = activiteiten.FirstOrDefault(a => a.ActivityId == lijn.ActivityId);
                    var altPpu   = lijn.AlternatievePrijsPerEenheid ?? 0m;
                    var nacPpu   = lijn.NacalcPrijsPerEenheid ?? 0m;
                    var nacGeind = nacPpu * lijn.Correctiefactor * gewogenFactor;
                    var altTot   = altPpu * resultaat.AantalWoonCommEenheden;
                    var nacTot   = nacGeind * resultaat.AantalWoonCommEenheden;
                    var verschil = altTot - nacTot;
                    totAlt += altTot;
                    totNac += nacTot;

                    ws2.Cell(rij2, 1).Value = act?.Omschrijving ?? "—";
                    ws2.Cell(rij2, 2).Value = (double)altPpu;
                    ws2.Cell(rij2, 2).Style.NumberFormat.Format = "€ #,##0.00";
                    ws2.Cell(rij2, 3).Value = (double)altTot;
                    ws2.Cell(rij2, 3).Style.NumberFormat.Format = "€ #,##0";
                    ws2.Cell(rij2, 4).Value = (double)nacGeind;
                    ws2.Cell(rij2, 4).Style.NumberFormat.Format = "€ #,##0.00";
                    ws2.Cell(rij2, 5).Value = (double)verschil;
                    ws2.Cell(rij2, 5).Style.NumberFormat.Format = "€ #,##0";
                    if (verschil > 0)      ws2.Cell(rij2, 5).Style.Font.FontColor = XLColor.Orange;
                    else if (verschil < 0) ws2.Cell(rij2, 5).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");
                    else                   ws2.Cell(rij2, 5).Style.Font.FontColor = XLColor.Green;
                    rij2++;
                }

                ws2.Cell(rij2, 1).Value = $"Subtotaal lot {grp.Lot}";
                ws2.Cell(rij2, 3).Value = (double)totAlt;
                ws2.Cell(rij2, 3).Style.NumberFormat.Format = "€ #,##0";
                ws2.Cell(rij2, 4).Value = (double)totNac;
                ws2.Cell(rij2, 4).Style.NumberFormat.Format = "€ #,##0";
                ws2.Cell(rij2, 5).Value = (double)(totAlt - totNac);
                ws2.Cell(rij2, 5).Style.NumberFormat.Format = "€ #,##0";
                ws2.Range(rij2, 1, rij2, 5).Style.Font.Bold = true;
                ws2.Range(rij2, 1, rij2, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#bdd7ff");
                rij2 += 2;
            }

            ws2.Column(1).Width = 45;
            for (int c = 2; c <= 5; c++) ws2.Column(c).Width = 18;

            // ===== TABBLAD 3: Oppervlaktes =====
            if (oppervlaktes.Any())
            {
                var ws3 = wb.Worksheets.Add("Oppervlaktes");
                ws3.Cell(1, 1).Value = "Oppervlaktes";
                ws3.Cell(1, 1).Style.Font.Bold = true;
                ws3.Cell(1, 1).Style.Font.FontSize = 14;
                ws3.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0d6efd");

                var headers3 = new[] { "Eenheid", "GBA (m²)", "Tuin", "Terras", "Dakterras", "Grondopp." };
                for (int c = 0; c < headers3.Length; c++)
                {
                    ws3.Cell(3, c + 1).Value = headers3[c];
                    ws3.Cell(3, c + 1).Style.Font.Bold = true;
                    ws3.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#bdd7ff");
                }

                int rij3 = 4;
                foreach (var o in oppervlaktes)
                {
                    ws3.Cell(rij3, 1).Value = o.EenheidNaam ?? "—";
                    ws3.Cell(rij3, 2).Value = (double)o.BewoonbareOpp;
                    ws3.Cell(rij3, 3).Value = (double)o.Tuin;
                    ws3.Cell(rij3, 4).Value = (double)(o.TerrasPrefab + o.TerrasGelijkvloers);
                    ws3.Cell(rij3, 5).Value = (double)o.Dakterras;
                    ws3.Cell(rij3, 6).Value = (double)o.Grondopp;
                    for (int c = 2; c <= 6; c++) ws3.Cell(rij3, c).Style.NumberFormat.Format = "#,##0.00";
                    rij3++;
                }

                ws3.Column(1).Width = 30;
                for (int c = 2; c <= 6; c++) ws3.Column(c).Width = 14;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
