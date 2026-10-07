using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BOCore;
using BOCore.Budget;
using ClosedXML.Excel;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    /// <summary>
    /// Referentieprojecten voor de nacalculatie (stap 6). Een referentieproject is een bevroren snapshot: werkelijke kost per
    /// activiteit op een peildatum, met de S- en I-index van dat moment. De vergelijking indexeert elk bedrag naar de huidige
    /// index van de budgetversie (zelfde gewogen formule als de materialen: 40 % I + 40 % S + 20 % vast) en deelt door de
    /// eenheden (of de GBA) van de referentieprojecten.
    /// </summary>
    public class BudgetReferentieProjectService : IBudgetReferentieProjectService
    {
        public const string BronExcel   = "Excel";
        public const string BronProject = "Project";

        private readonly UnitOfWorkCore _uow;
        private readonly BouwIndexService _bouwIndex;

        public BudgetReferentieProjectService(UnitOfWorkCore uow, BouwIndexService bouwIndex)
        {
            _uow      = uow;
            _bouwIndex = bouwIndex;
        }

        // ── Lezen ─────────────────────────────────────────────────────────────

        public async Task<List<BudgetReferentieProjectBO>> GetAlleAsync()
        {
            var list = await Query().OrderByDescending(r => r.Datum).ThenBy(r => r.Naam).ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<BudgetReferentieProjectBO> GetAsync(int id)
        {
            var e = await Query().FirstOrDefaultAsync(r => r.Id == id);
            return e is null ? null : Map(e);
        }

        private IQueryable<BudgetReferentieProject> Query() =>
            _uow.BudgetReferentieProjecten.GetNoTracking()
                .Include(r => r.Project)
                .Include(r => r.Lijnen).ThenInclude(l => l.Activity).ThenInclude(a => a.Group);

        // ── Import uit een project van de app ─────────────────────────────────

        public async Task<Response> ImportUitProjectAsync(int projectId, string naam, DateTime? datum, string opmerking)
        {
            var response = new Response();
            var project = await _uow.Projects.GetNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project is null) { response.AddError("Project niet gevonden."); return response; }

            // Werkelijke kost: inkomende facturen per activiteit (rechtstreeks op activiteit of via de contractactiviteit).
            var facturen = await _uow.IncommingInvoiceDetails.GetNoTracking()
                .Where(d => d.IncommingInvoice.ProjectId == projectId && d.Price != null
                            && (d.ActId != null || (d.ContractAct != null && d.ContractAct.Contract.ProjectId == projectId)))
                .Select(d => new { ActivityId = d.ActId ?? d.ContractAct.ActivityId, Bedrag = d.Price.Value })
                .ToListAsync();

            Dictionary<int, decimal> perActiviteit;
            string basis;
            if (facturen.Count > 0)
            {
                perActiviteit = facturen.GroupBy(f => f.ActivityId).ToDictionary(g => g.Key, g => g.Sum(f => f.Bedrag));
                basis = "inkomende facturen";
            }
            else
            {
                // Geen facturen (gekoppeld): val terug op de gecontracteerde bedragen.
                var contracten = await _uow.ContractActivities.GetNoTracking()
                    .Where(ca => ca.Contract.ProjectId == projectId && ca.Price != null)
                    .Select(ca => new { ca.ActivityId, Bedrag = ca.Price.Value })
                    .ToListAsync();
                perActiviteit = contracten.GroupBy(c => c.ActivityId).ToDictionary(g => g.Key, g => g.Sum(c => c.Bedrag));
                basis = "contracten";
            }

            perActiviteit = perActiviteit.Where(kv => kv.Value != 0m).ToDictionary(kv => kv.Key, kv => kv.Value);
            if (perActiviteit.Count == 0)
            {
                response.AddError("Dit project heeft geen inkomende facturen of contracten met een bedrag per activiteit.");
                return response;
            }

            // Eenheden: woon-/commerciële units van het project (zelfde regel als de wizard), anders alle units.
            var units = await _uow.Units.GetNoTracking().Include(u => u.Type).ThenInclude(t => t.Group)
                .Where(u => u.ProjectId == projectId).ToListAsync();
            var woonComm = units.Count(u => u.Type?.Group != null &&
                (u.Type.Group.Name.Contains("woon", StringComparison.OrdinalIgnoreCase) ||
                 u.Type.Group.Name.Contains("commerci", StringComparison.OrdinalIgnoreCase)));
            var aantalEenheden = woonComm > 0 ? woonComm : units.Count;

            // GBA: bewoonbare oppervlakte uit de recentste budgetversie van het project, als die er is.
            var laatsteVersieId = await _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.ProjectId == projectId).OrderByDescending(v => v.Versienummer).Select(v => (int?)v.Id).FirstOrDefaultAsync();
            decimal? gba = null;
            if (laatsteVersieId.HasValue)
            {
                var som = await _uow.BudgetOppervlaktes.GetNoTracking().Where(o => o.BudgetVersieId == laatsteVersieId.Value).SumAsync(o => (decimal?)o.BewoonbareOpp);
                if (som is > 0m) gba = som;
            }

            var peildatum = datum
                ?? (project.DeliveryDateDef?.ToDateTime(TimeOnly.MinValue))
                ?? (project.DeliveryDate?.ToDateTime(TimeOnly.MinValue))
                ?? DateTime.Today;

            var entity = await NieuwReferentieProjectAsync(
                string.IsNullOrWhiteSpace(naam) ? project.ProjectName : naam.Trim(), projectId, peildatum, aantalEenheden, gba, BronProject,
                string.IsNullOrWhiteSpace(opmerking) ? $"Basis: {basis}" : opmerking.Trim());
            foreach (var kv in perActiviteit)
                entity.Lijnen.Add(new BudgetReferentieProjectLijn { ActivityId = kv.Key, Bedrag = Math.Round(kv.Value, 2) });

            _uow.BudgetReferentieProjecten.Add(entity);
            await _uow.SaveChangesAsync();
            response.InsertedId = entity.Id;
            response.AddSuccess($"Referentieproject \"{entity.Naam}\" aangemaakt uit {basis}: {perActiviteit.Count} activiteiten, {aantalEenheden} eenheden.");
            if (!gba.HasValue) response.AddWarning("Geen bewoonbare oppervlakte gevonden (geen budget op dat project): vergelijking per m² niet mogelijk voor dit referentieproject.");
            return response;
        }

        // ── Import uit Excel ──────────────────────────────────────────────────

        public async Task<Response> ImportExcelAsync(Stream xlsx, string naam, DateTime? datum, int aantalEenheden, decimal? oppervlakteGBA, string opmerking)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(naam)) { response.AddError("Geef het referentieproject een naam."); return response; }
            if (aantalEenheden <= 0) { response.AddError("Aantal eenheden moet groter dan 0 zijn (deler voor de prijs per eenheid)."); return response; }
            if (xlsx is null || xlsx.Length == 0) { response.AddError("Geen Excel-bestand ontvangen."); return response; }

            var activiteiten = await _uow.Activities.GetNoTracking().ToListAsync();
            var opId   = activiteiten.ToDictionary(a => a.ActivityId, a => a);
            var opNaam = new Dictionary<string, Activity>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in activiteiten)
            {
                var key = Normaliseer(a.Omschrijving);
                if (!string.IsNullOrEmpty(key) && !opNaam.ContainsKey(key)) opNaam[key] = a;
            }

            List<(int ActivityId, decimal Bedrag)> gelezen;
            List<string> onbekend;
            try
            {
                (gelezen, onbekend) = LeesExcel(xlsx, opId, opNaam);
            }
            catch (Exception ex)
            {
                response.AddError("Excel kon niet gelezen worden: " + ex.Message);
                return response;
            }

            var perActiviteit = gelezen.GroupBy(g => g.ActivityId).ToDictionary(g => g.Key, g => g.Sum(x => x.Bedrag));
            if (perActiviteit.Count == 0)
            {
                response.AddError("Geen enkele rij kon aan een activiteit gekoppeld worden. Gebruik het sjabloon (kolommen ActivityId · Activiteit · Bedrag)."
                    + (onbekend.Count > 0 ? " Niet herkend: " + string.Join(", ", onbekend.Take(10)) + (onbekend.Count > 10 ? ", …" : "") : ""));
                return response;
            }

            var entity = await NieuwReferentieProjectAsync(naam.Trim(), null, datum, aantalEenheden, oppervlakteGBA, BronExcel, opmerking?.Trim());
            foreach (var kv in perActiviteit)
                entity.Lijnen.Add(new BudgetReferentieProjectLijn { ActivityId = kv.Key, Bedrag = Math.Round(kv.Value, 2) });

            _uow.BudgetReferentieProjecten.Add(entity);
            await _uow.SaveChangesAsync();
            response.InsertedId = entity.Id;
            response.AddSuccess($"Referentieproject \"{entity.Naam}\" ingelezen: {perActiviteit.Count} activiteiten, totaal € {entity.Lijnen.Sum(l => l.Bedrag):N0}.");
            if (onbekend.Count > 0)
                response.AddWarning($"{onbekend.Count} rij(en) niet herkend als activiteit en overgeslagen: " + string.Join(", ", onbekend.Take(10)) + (onbekend.Count > 10 ? ", …" : ""));
            return response;
        }

        /// <summary>Leest het eerste werkblad. Kopregel met "ActivityId"/"Activiteit"/"Bedrag" (hoofdletterongevoelig) wordt herkend;
        /// zonder kopregel: kolom A = activiteit, laatste gevulde numerieke kolom = bedrag. Lege of 0-bedragen worden overgeslagen.</summary>
        internal static (List<(int ActivityId, decimal Bedrag)> Gelezen, List<string> Onbekend) LeesExcel(
            Stream xlsx, IReadOnlyDictionary<int, Activity> opId, IReadOnlyDictionary<string, Activity> opNaam)
        {
            var gelezen  = new List<(int, decimal)>();
            var onbekend = new List<string>();

            using var wb = new XLWorkbook(xlsx);
            var ws = wb.Worksheets.First();
            var used = ws.RangeUsed();
            if (used is null) return (gelezen, onbekend);

            int colId = 0, colNaam = 1, colBedrag = 0, eersteRij = used.FirstRow().RowNumber();
            var kop = used.FirstRow();
            foreach (var cell in kop.Cells())
            {
                var h = Normaliseer(cell.GetString());
                if (h is "activityid" or "id")                       colId     = cell.Address.ColumnNumber;
                else if (h is "activiteit" or "activity" or "omschrijving") colNaam = cell.Address.ColumnNumber;
                else if (h.StartsWith("bedrag") || h.StartsWith("kost") || h.StartsWith("prijs") || h.StartsWith("totaal")) colBedrag = cell.Address.ColumnNumber;
            }
            bool heeftKop = colBedrag > 0 || colId > 0;
            if (heeftKop) eersteRij++;
            if (colBedrag == 0) colBedrag = used.LastColumn().ColumnNumber();

            for (int r = eersteRij; r <= used.LastRow().RowNumber(); r++)
            {
                var row = ws.Row(r);
                var bedrag = LeesDecimal(row.Cell(colBedrag));
                if (!bedrag.HasValue || bedrag.Value == 0m) continue;

                Activity act = null;
                if (colId > 0)
                {
                    var id = LeesDecimal(row.Cell(colId));
                    if (id.HasValue && opId.TryGetValue((int)id.Value, out var byId)) act = byId;
                }
                var naamCel = row.Cell(colNaam).GetString();
                if (act is null && opNaam.TryGetValue(Normaliseer(naamCel), out var byNaam)) act = byNaam;

                if (act is null) { onbekend.Add(string.IsNullOrWhiteSpace(naamCel) ? $"rij {r}" : naamCel.Trim()); continue; }
                gelezen.Add((act.ActivityId, bedrag.Value));
            }
            return (gelezen, onbekend);
        }

        private static decimal? LeesDecimal(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
            var s = cell.GetString().Trim().Replace("€", "").Replace(" ", "").Replace(" ", "");
            if (string.IsNullOrEmpty(s)) return null;
            // nl-BE "1.234,56" → "1234.56"; en "1234.56" blijft
            if (s.Contains(',') ) s = s.Replace(".", "").Replace(',', '.');
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        /// <summary>Kleine letters, zonder dubbele spaties en leestekens — zodat "Ruwbouw " en "ruwbouw" matchen.</summary>
        public static string Normaliseer(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"[^\p{L}\p{N}]+", " ");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        public byte[] MaakSjabloon()
        {
            var activiteiten = _uow.Activities.GetNoTracking().Include(a => a.Group)
                .Where(a => a.Group != null).OrderBy(a => a.Group.Lot).ThenBy(a => a.Omschrijving).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Referentieproject");
            ws.Cell(1, 1).Value = "ActivityId";
            ws.Cell(1, 2).Value = "Lot";
            ws.Cell(1, 3).Value = "Activiteit";
            ws.Cell(1, 4).Value = "Bedrag (excl. btw)";
            ws.Range(1, 1, 1, 4).Style.Font.Bold = true;
            int r = 2;
            foreach (var a in activiteiten)
            {
                ws.Cell(r, 1).Value = a.ActivityId;
                ws.Cell(r, 2).Value = a.Group.Lot.HasValue ? $"Lot {a.Group.Lot.Value:G} – {a.Group.Name}" : a.Group.Name;
                ws.Cell(r, 3).Value = a.Omschrijving;
                ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                r++;
            }
            ws.Column(1).Width = 11; ws.Column(2).Width = 34; ws.Column(3).Width = 44; ws.Column(4).Width = 18;
            ws.SheetView.FreezeRows(1);
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<Response> DeleteAsync(int id)
        {
            var response = new Response();
            var e = await _uow.BudgetReferentieProjecten.GetNormal().FirstOrDefaultAsync(r => r.Id == id);
            if (e is null) { response.AddError("Referentieproject niet gevonden."); return response; }
            _uow.BudgetReferentieProjecten.Remove(e);  // lijnen en versiekoppelingen: cascade
            await _uow.SaveChangesAsync();
            response.AddSuccess("Referentieproject verwijderd.");
            return response;
        }

        // ── Keuze per budgetversie ────────────────────────────────────────────

        public Task<List<int>> GetReferentieIdsVoorVersieAsync(int budgetVersieId) =>
            _uow.BudgetVersieNacalcReferenties.GetNoTracking()
                .Where(x => x.BudgetVersieId == budgetVersieId).Select(x => x.ReferentieProjectId).ToListAsync();

        public async Task<Response> SetReferentiesVoorVersieAsync(int budgetVersieId, IEnumerable<int> referentieProjectIds)
        {
            var response = new Response();
            var gewenst = (referentieProjectIds ?? Enumerable.Empty<int>()).Distinct().ToHashSet();
            var bestaand = await _uow.BudgetVersieNacalcReferenties.GetNormal().Where(x => x.BudgetVersieId == budgetVersieId).ToListAsync();
            foreach (var b in bestaand.Where(b => !gewenst.Contains(b.ReferentieProjectId)))
                _uow.BudgetVersieNacalcReferenties.Remove(b);
            foreach (var id in gewenst.Where(id => bestaand.All(b => b.ReferentieProjectId != id)))
                _uow.BudgetVersieNacalcReferenties.Add(new BudgetVersieNacalcReferentie { BudgetVersieId = budgetVersieId, ReferentieProjectId = id });
            await _uow.SaveChangesAsync();
            response.AddSuccess(gewenst.Count == 0 ? "Geen referentieprojecten meer gekozen." : $"{gewenst.Count} referentieproject(en) gekozen.");
            return response;
        }

        // ── Vergelijking ──────────────────────────────────────────────────────

        public async Task<Dictionary<int, NacalcReferentieBO>> BerekenReferentieAsync(int budgetVersieId)
        {
            var ids = await GetReferentieIdsVoorVersieAsync(budgetVersieId);
            if (ids.Count == 0) return new Dictionary<int, NacalcReferentieBO>();

            var refs = (await Query().Where(r => ids.Contains(r.Id)).ToListAsync()).Select(Map).ToList();

            var geg = await _uow.BudgetGegevens.GetNoTracking().FirstOrDefaultAsync(g => g.BudgetVersieId == budgetVersieId);
            var sHuidig = geg?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iHuidig = geg?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I2021");

            return Bereken(refs, sHuidig, iHuidig);
        }

        /// <summary>
        /// Pure functie (unit-getest). Per activiteit over de referentieprojecten die er een bedrag voor hebben:
        ///   factor_p        = gewogen indexfactor van (SIndex_p, IIndex_p) naar (sHuidig, iHuidig); 1 zonder indexen
        ///   prijs/eenheid   = Σ bedrag_p × factor_p ÷ Σ eenheden_p
        ///   prijs/m²        = Σ bedrag_p × factor_p ÷ Σ GBA_p   (enkel projecten mét GBA)
        /// </summary>
        public static Dictionary<int, NacalcReferentieBO> Bereken(IReadOnlyList<BudgetReferentieProjectBO> refs, decimal sHuidig, decimal iHuidig)
        {
            var result = new Dictionary<int, NacalcReferentieBO>();
            if (refs is null || refs.Count == 0) return result;

            var factor = refs.ToDictionary(r => r.Id, r => Factor(r, sHuidig, iHuidig));

            foreach (var groep in refs.SelectMany(r => r.Lijnen.Where(l => l.Bedrag != 0m).Select(l => (Ref: r, Lijn: l))).GroupBy(x => x.Lijn.ActivityId))
            {
                var items = groep.Where(x => x.Ref.AantalEenheden > 0).ToList();
                if (items.Count == 0) continue;

                var somGeind    = items.Sum(x => x.Lijn.Bedrag * factor[x.Ref.Id]);
                var somEenheden = items.Sum(x => x.Ref.AantalEenheden);
                var metGba      = items.Where(x => x.Ref.OppervlakteGBA is > 0m).ToList();
                var perEenheid  = items.Select(x => x.Lijn.Bedrag * factor[x.Ref.Id] / x.Ref.AantalEenheden).ToList();

                result[groep.Key] = new NacalcReferentieBO
                {
                    ActivityId      = groep.Key,
                    AantalProjecten = items.Count,
                    PrijsPerEenheid = Math.Round(somGeind / somEenheden, 2),
                    PrijsPerM2      = metGba.Count > 0 ? Math.Round(metGba.Sum(x => x.Lijn.Bedrag * factor[x.Ref.Id]) / metGba.Sum(x => x.Ref.OppervlakteGBA.Value), 2) : null,
                    MinPerEenheid   = Math.Round(perEenheid.Min(), 2),
                    MaxPerEenheid   = Math.Round(perEenheid.Max(), 2)
                };
            }
            return result;
        }

        /// <summary>Gewogen indexfactor referentiedatum → nu (40 % I + 40 % S + 20 % vast); 1 als het referentieproject geen indexen heeft.</summary>
        public static decimal Factor(BudgetReferentieProjectBO r, decimal sHuidig, decimal iHuidig)
        {
            if (!(r.SIndex is > 0m) || !(r.IIndex is > 0m) || sHuidig <= 0m || iHuidig <= 0m) return 1m;
            return (iHuidig / r.IIndex.Value) * 0.40m + (sHuidig / r.SIndex.Value) * 0.40m + 0.20m;
        }

        // ── Hulp ──────────────────────────────────────────────────────────────

        private async Task<BudgetReferentieProject> NieuwReferentieProjectAsync(string naam, int? projectId, DateTime? datum, int aantalEenheden, decimal? gba, string bron, string opmerking)
        {
            decimal? sIndex = null, iIndex = null;
            if (datum.HasValue)
            {
                sIndex = await _bouwIndex.GetIndexOpDatumAsync("S", datum.Value);
                iIndex = await _bouwIndex.GetIndexOpDatumAsync("I2021", datum.Value);
            }
            return new BudgetReferentieProject
            {
                Naam           = naam.Length > 200 ? naam.Substring(0, 200) : naam,
                ProjectId      = projectId,
                Datum          = datum?.Date,
                AantalEenheden = aantalEenheden,
                OppervlakteGBA = gba,
                SIndex         = sIndex,
                IIndex         = iIndex,
                Bron           = bron,
                Opmerking      = string.IsNullOrWhiteSpace(opmerking) ? null : (opmerking.Length > 500 ? opmerking.Substring(0, 500) : opmerking),
                CreatedAt      = DateTime.Now
            };
        }

        private static BudgetReferentieProjectBO Map(BudgetReferentieProject e) => new()
        {
            Id             = e.Id,
            Naam           = e.Naam,
            ProjectId      = e.ProjectId,
            ProjectNaam    = e.Project?.ProjectName,
            Datum          = e.Datum,
            AantalEenheden = e.AantalEenheden,
            OppervlakteGBA = e.OppervlakteGBA,
            SIndex         = e.SIndex,
            IIndex         = e.IIndex,
            Bron           = e.Bron,
            Opmerking      = e.Opmerking,
            CreatedAt      = e.CreatedAt,
            Lijnen         = e.Lijnen
                .OrderBy(l => l.Activity?.Group?.Lot ?? 0m).ThenBy(l => l.Activity?.Omschrijving)
                .Select(l => new BudgetReferentieLijnBO
                {
                    Id                   = l.Id,
                    ActivityId           = l.ActivityId,
                    ActivityOmschrijving = l.Activity?.Omschrijving,
                    LotNummer            = l.Activity?.Group?.Lot ?? 0m,
                    LotNaam              = l.Activity?.Group?.Name,
                    Bedrag               = l.Bedrag,
                    Opmerking            = l.Opmerking
                }).ToList()
        };
    }
}
