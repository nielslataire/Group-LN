using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public enum MeldingType { Info = 0, Waarschuwing = 1, Fout = 2 }

    /// <summary>Eén melding van de budgetflow (design-handoff 39k: type, voorwaarde en plaats).
    /// <see cref="Plaats"/>: "veld" · "rij" · "kader" · "label". <see cref="Sleutel"/> koppelt de melding aan een veldnaam of rij-id op de stap.</summary>
    public class BudgetMelding
    {
        public int Stap { get; set; }
        public MeldingType Type { get; set; }
        public string Code { get; set; }
        public string Tekst { get; set; }
        public string Plaats { get; set; }
        public string Sleutel { get; set; }
        /// <summary>false = enkel een weergave (bv. het kader bij een veldwaarschuwing); telt niet mee in het Stappenplan.</summary>
        public bool Telt { get; set; } = true;
        public bool Genegeerd { get; set; }
        public string GenegeerdDoor { get; set; }
        public DateTime? GenegeerdOp { get; set; }
        /// <summary>Telt mee als openstaande waarschuwing/fout.</summary>
        public bool IsOpen => Telt && !Genegeerd && Type != MeldingType.Info;
    }

    /// <summary>
    /// Alle waarschuwingen en fouten van de budgetflow op één plaats (39k). Fouten blokkeren "Afronden", waarschuwingen niet;
    /// een waarschuwing kan genegeerd worden (naam + datum bewaard in <c>BudgetVersie.WaarschuwingenBevestigd</c>, per code).
    /// </summary>
    public class BudgetControleService
    {
        private static readonly string[] DakTypes = { "PlatDak", "HellendDak", "GroenDak" };
        private readonly UnitOfWorkCore _uow;
        private readonly BudgetActivityService _activity;
        private readonly IBudgetReferentieProjectService _referentie;
        private readonly IVerkoopVoorstelService _voorstel;
        private readonly BudgetBerekeningService _berekening;

        public BudgetControleService(UnitOfWorkCore uow, BudgetActivityService activity, IBudgetReferentieProjectService referentie, IVerkoopVoorstelService voorstel, BudgetBerekeningService berekening)
        {
            _uow = uow; _activity = activity; _referentie = referentie; _voorstel = voorstel; _berekening = berekening;
        }

        // ── Genegeerde waarschuwingen: "code~naam~yyyy-MM-dd,code2" (oud formaat: enkel de code) ───────
        public static Dictionary<string, (string Door, DateTime? Op)> ParseGenegeerd(string raw)
        {
            var d = new Dictionary<string, (string, DateTime?)>(StringComparer.OrdinalIgnoreCase);
            foreach (var deel in (raw ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = deel.Trim().Split('~');
                DateTime? op = p.Length > 2 && DateTime.TryParseExact(p[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
                d[p[0]] = (p.Length > 1 ? p[1] : null, op);
            }
            return d;
        }

        public static string ZetGenegeerd(string raw, string code, bool negeren, string door)
        {
            var d = ParseGenegeerd(raw);
            if (negeren) d[code] = (door, DateTime.Today); else d.Remove(code);
            if (d.Count == 0) return null;
            string Schoon(string s) => (s ?? "").Replace(",", "").Replace("~", "");
            return string.Join(",", d.Select(kv => kv.Value.Op.HasValue ? $"{kv.Key}~{Schoon(kv.Value.Door)}~{kv.Value.Op:yyyy-MM-dd}" : kv.Key));
        }

        /// <summary>Zoekt de indexrij bij een waarde: exact, anders binnen 0,005 (de modelbinder rondt af op 2 decimalen); dichtstbij en meest recent eerst.</summary>
        public static BouwIndex ZoekIndexRij(IEnumerable<BouwIndex> rijen, decimal waarde)
            => rijen.Where(x => Math.Abs(x.IndexWaarde - waarde) < 0.005m)
                    .OrderBy(x => Math.Abs(x.IndexWaarde - waarde)).ThenByDescending(x => x.GeldigVanaf ?? DateTime.MinValue).ThenByDescending(x => x.Jaar).ThenByDescending(x => x.Maand)
                    .FirstOrDefault();

        // ── Hoofdberekening ────────────────────────────────────────────────────────────────────────────
        public async Task<List<BudgetMelding>> BerekenAsync(int versieId)
        {
            var res = new List<BudgetMelding>();
            var versie = await _uow.BudgetVersies.GetNoTracking().Include(v => v.BudgetGegevens).FirstOrDefaultAsync(v => v.Id == versieId);
            if (versie == null) return res;
            var g = versie.BudgetGegevens;

            void Add(int stap, MeldingType type, string code, string tekst, string plaats, string sleutel = null, bool telt = true)
                => res.Add(new BudgetMelding { Stap = stap, Type = type, Code = code, Tekst = tekst, Plaats = plaats, Sleutel = sleutel, Telt = telt });

            var opps = await _uow.BudgetOppervlaktes.GetNoTracking().Include(o => o.UnitGroupType).Where(o => o.BudgetVersieId == versieId).OrderBy(o => o.SortOrder).ToListAsync();
            var elementen = await _uow.BudgetGevelElementen.GetNoTracking().Where(e => e.BudgetVersieId == versieId).ToListAsync();
            bool Woning(BudgetOppervlaktes o) => o.UnitGroupType != null && o.UnitGroupType.Name != null && o.UnitGroupType.Name.Contains("woon", StringComparison.OrdinalIgnoreCase);
            decimal M2(BudgetGevelElementen e)
            {
                if (e.Hoogte.HasValue && e.Hoogte.Value != 0m) return e.Aantal * (e.Breedte ?? 0m) * e.Hoogte.Value;
                if (e.Breedte.HasValue && e.Breedte.Value != 0m && e.Lengte.HasValue && e.Lengte.Value != 0m) return e.Aantal * e.Breedte.Value * e.Lengte.Value;
                return 0m;
            }

            // ── Stap 1 · Gegevens ──────────────────────────────────────────────────────────────────────
            if (g != null)
            {
                bool indexProbleem = false;
                foreach (var (type, waarde, sleutel, naam) in new[] { ("S", g.SIndexHuidig, "Gegevens.SIndexHuidig", "S-index"), ("I2021", g.IIndexHuidig, "Gegevens.IIndexHuidig", "I2021-index") })
                {
                    if (!waarde.HasValue || waarde.Value == 0m) continue;
                    var rij = ZoekIndexRij(await _uow.BouwIndex.GetNoTracking().Where(x => x.IndexType == type).ToListAsync(), waarde.Value);
                    DateTime? peil = rij?.GeldigVanaf ?? (rij?.Jaar != null ? new DateTime(rij.Jaar.Value, rij.Maand ?? 1, 1) : (DateTime?)null);
                    if (peil == null) { Add(1, MeldingType.Waarschuwing, "index-peildatum", $"{naam} zonder peildatum — controleer de indexwaarde.", "veld", sleutel); indexProbleem = true; }
                    else if (peil.Value < DateTime.Today.AddMonths(-6)) { Add(1, MeldingType.Waarschuwing, "index-peildatum", $"{naam} van {peil.Value:MM/yyyy} is ouder dan 6 maanden — klopt dit nog?", "veld", sleutel); indexProbleem = true; }
                }
                if (indexProbleem) Add(1, MeldingType.Waarschuwing, "index-peildatum", "Een index heeft geen peildatum of is ouder dan 6 maanden. Kies de actuele indexwaarde via Historiek.", "kader", null, telt: false);

                if (opps.Count > 0)
                {
                    if ((g.M3Grondwerk ?? 0m) == 0m) Add(1, MeldingType.Waarschuwing, "grondwerk-leeg", "Grondwerk is leeg — grondwerken worden niet berekend.", "veld", "Gegevens.M3Grondwerk");
                    if ((g.OppFunderingen ?? 0m) == 0m) Add(1, MeldingType.Waarschuwing, "funderingen-leeg", "Oppervlakte funderingen is leeg.", "veld", "Gegevens.OppFunderingen");
                }
                var dakRijen = elementen.Where(e => DakTypes.Contains(e.ElementType)).ToList();
                if (string.IsNullOrWhiteSpace(g.TypeDak) && dakRijen.Any(e => M2(e) > 0m))
                    Add(1, MeldingType.Fout, "geen-daktype", "Geen daktype gekozen, terwijl er in stap 5 dakoppervlakte staat.", "veld", "Gegevens.TypeDak");
                if (await PoortenOntbreektAsync(versieId)) Add(1, MeldingType.Waarschuwing, "poorten", "De poorten moeten geselecteerd worden.", "veld", "Gegevens.TypePoorten");
            }

            // ── Stap 2 · Oppervlaktes ──────────────────────────────────────────────────────────────────
            foreach (var o in opps.Where(Woning).Where(o => o.Grondopp <= 0m || o.BewoonbareOpp <= 0m))
                Add(2, MeldingType.Waarschuwing, "woning-zonder-opp", o.Grondopp <= 0m && o.BewoonbareOpp <= 0m ? "Woning zonder grond en zonder bewoonbare oppervlakte." : o.Grondopp <= 0m ? "Woning zonder grondoppervlakte." : "Woning zonder bewoonbare oppervlakte.", "rij", o.Id.ToString());
            foreach (var grp in opps.Where(o => !string.IsNullOrWhiteSpace(o.EenheidNaam)).GroupBy(o => o.EenheidNaam.Trim().ToLowerInvariant()).Where(x => x.Count() > 1))
                foreach (var o in grp) Add(2, MeldingType.Fout, "dubbele-naam", $"De naam “{o.EenheidNaam.Trim()}” komt twee keer voor.", "rij", o.Id.ToString());
            var projectEenheden = await _uow.Units.GetNoTracking().CountAsync(u => u.ProjectId == versie.ProjectId);
            if (projectEenheden > 0 && projectEenheden != opps.Count)
                Add(2, MeldingType.Waarschuwing, "aantal-eenheden", $"Dit budget telt {opps.Count} eenhe{(opps.Count == 1 ? "id" : "den")}, het project {projectEenheden}.", "kader");

            // ── Stap 3 · Sanitair ──────────────────────────────────────────────────────────────────────
            foreach (var s in await _uow.BudgetSanitair.GetNoTracking().Where(x => x.BudgetVersieId == versieId).OrderBy(x => x.SortOrder).ToListAsync())
            {
                if (s.Badkamer <= 0 && s.ToiletInBadkamer + s.AfzonderlijkToilet <= 0) Add(3, MeldingType.Waarschuwing, "sanitair-ontbreekt", "Woning zonder badkamer en zonder toilet.", "rij", s.Id.ToString());
                else if (s.Badkamer <= 0) Add(3, MeldingType.Waarschuwing, "sanitair-ontbreekt", "Woning zonder badkamer.", "rij", s.Id.ToString());
                else if (s.ToiletInBadkamer + s.AfzonderlijkToilet <= 0) Add(3, MeldingType.Waarschuwing, "sanitair-ontbreekt", "Woning zonder toilet.", "rij", s.Id.ToString());
            }

            // ── Stap 4 · Gevels en ramen ───────────────────────────────────────────────────────────────
            foreach (var e in elementen.Where(e => e.ElementType is "GevelNieuwbouw" or "GevelBestaand" or "RaamNieuwbouw" or "RaamBestaand"))
                if (M2(e) < 0m || e.Aantal < 0m || (e.Breedte ?? 0m) < 0m || (e.Hoogte ?? 0m) < 0m || (e.Lengte ?? 0m) < 0m)
                    Add(4, MeldingType.Fout, "negatieve-regel", "Negatieve regel — gebruik tabblad Ramen om ramen af te trekken.", "veld", e.Id.ToString());
            foreach (var (gevel, raam, label) in new[] { ("GevelNieuwbouw", "RaamNieuwbouw", "nieuwbouw"), ("GevelBestaand", "RaamBestaand", "bestaand") })
            {
                var gm = elementen.Where(e => e.ElementType == gevel).Sum(M2); var rm = elementen.Where(e => e.ElementType == raam).Sum(M2);
                if (rm > 0m && rm > gm) Add(4, MeldingType.Fout, "ramen-groter-dan-gevel", $"De ramen {label} ({rm:N2} m²) zijn groter dan de geveloppervlakte {label} ({gm:N2} m²).".Replace('.', ','), "kader", null);
            }

            // ── Stap 5 · Dak & afbraak ─────────────────────────────────────────────────────────────────
            var daken = elementen.Where(e => DakTypes.Contains(e.ElementType)).ToList();
            if (daken.Count == 0) Add(5, MeldingType.Fout, "geen-daktype-aan", "Geen enkel daktype aangezet — zet plat, hellend of groen dak aan en vul de regels in.", "kader");
            if ((g?.AantalVeluxen ?? 0) > 0 && daken.Any(e => e.ElementType == "PlatDak") && !daken.Any(e => e.ElementType == "HellendDak"))
                Add(5, MeldingType.Waarschuwing, "velux-plat-dak", "Veluxen bij een plat dak — klopt dit?", "veld", "bw-velux");

            // ── Stap 6 · Activiteiten ──────────────────────────────────────────────────────────────────
            var groepen = await _activity.GetLotGroepenAsync(versieId);
            var referentie = await _referentie.BerekenReferentieAsync(versieId);
            foreach (var lijn in groepen.SelectMany(x => x.Lijnen))
            {
                if (referentie.TryGetValue(lijn.ActivityId, out var r)) { lijn.ReferentiePrijsPerEenheid = r.PrijsPerEenheid; lijn.ReferentiePrijsPerM2 = r.PrijsPerM2; lijn.ReferentieAantalProjecten = r.AantalProjecten; lijn.ReferentieMinPerEenheid = r.MinPerEenheid; lijn.ReferentieMaxPerEenheid = r.MaxPerEenheid; }
                // Alleen activiteiten met een bedrag: dat zijn de rijen die stap 6 toont (een activiteit op € 0 met referentie telde voordien als -100 % afwijking)
                if (lijn.TotaalAlternatief == 0m) { Add(6, MeldingType.Info, "activiteit-nul", "Activiteit op € 0.", "rij", lijn.ActivityId.ToString()); continue; }
                var afw = lijn.ReferentieVerschilPerc;
                if (afw.HasValue && Math.Abs(afw.Value) > 0.5m) Add(6, MeldingType.Waarschuwing, "afwijking-referentie", $"Afwijking van {(afw.Value * 100m):+0;-0} % tegenover de referentie.", "rij", lijn.ActivityId.ToString());
                var corr = lijn.Correctiefactor <= 0m ? 1m : lijn.Correctiefactor;
                if (corr != 1m && string.IsNullOrWhiteSpace(lijn.Opmerking)) Add(6, MeldingType.Waarschuwing, "correctie-zonder-opmerking", "Correctie ≠ 100 % zonder opmerking.", "veld", lijn.ActivityId.ToString());
            }

            // ── Stap 7 · Parameters ────────────────────────────────────────────────────────────────────
            var p = await _uow.BudgetParams.GetNoTracking().FirstOrDefaultAsync(x => x.BudgetVersieId == versieId);
            if (p != null)
            {
                if ((p.DecennaleGeslRuwbouwPerc ?? 0m) <= 0m)
                {
                    Add(7, MeldingType.Waarschuwing, "decennale", "0 % — verplicht voor gesloten ruwbouw. Vul een percentage in of bevestig dat ze elders gedekt is.", "veld", "pctDecennale");
                }
                var std = await _uow.BouwkostPercentages.GetNoTracking().Where(x => x.Sleutel != null).ToDictionaryAsync(x => x.Sleutel, x => x.Percentage / 100m);
                foreach (var (sleutel, waarde, veld, naam) in new[] { ("projectcoordinatie", (decimal?)p.ProjectcoordinatiePerc, "pctProjectcoord", "Projectcoördinatie"), ("architect", p.ArchitectPerc, "pctArchitect", "Architect"), ("ingenieur", p.StudieIRPerc, "pctIngenieur", "Ingenieur"), ("doelmarge", p.DoelMargePerc, "pctDoelmarge", "Doelmarge"), ("grondmarge", p.GrondMargePerc, "pctGrondmarge", "Grondmarge") })
                    if (waarde.HasValue && std.TryGetValue(sleutel, out var s) && s > 0m && Math.Abs(waarde.Value - s) / s > 0.5m)
                        Add(7, MeldingType.Waarschuwing, "afwijking-instellingen", $"{naam} wijkt meer dan 50 % af van de standaard in Instellingen ({(s * 100m):0.##} %).".Replace('.', ','), "veld", veld);
                if (p.WetBreynePerc > 0m && (p.WetBreyneMaanden ?? 0) <= 0) Add(7, MeldingType.Fout, "maanden-ontbreken", "Wet Breyne zonder aantal maanden.", "veld", "Params.WetBreyneMaanden");
                if (p.StraightloanGebouwPerc > 0m && (p.StraightloanGebouwMaanden ?? 0) <= 0) Add(7, MeldingType.Fout, "maanden-ontbreken", "Straight loan gebouw zonder aantal maanden.", "veld", "Params.StraightloanGebouwMaanden");
                if (p.StraightloanGrondPerc > 0m && (p.StraightloanGrondMaanden ?? 0) <= 0) Add(7, MeldingType.Fout, "maanden-ontbreken", "Straight loan grond zonder aantal maanden.", "veld", "Params.StraightloanGrondMaanden");
                if ((p.AankoopprijsGrond ?? 0m) == 0m) Add(7, MeldingType.Waarschuwing, "grondprijs-leeg", "Aankoopprijs grond is leeg.", "veld", "Params.AankoopprijsGrond");
            }

            // ── Stap 8 · Verkoop ───────────────────────────────────────────────────────────────────────
            var vlijnen = await _uow.BudgetVerkoopLijn.GetNoTracking().Where(l => l.BudgetVersieId == versieId).OrderBy(l => l.SortOrder).ToListAsync();
            var voorstel = await _voorstel.BerekenAsync(versieId);
            var minima = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in voorstel?.Eenheden ?? new List<BOCore.Budget.BudgetVerkoopVoorstelEenheidBO>()) minima[(e.EenheidNaam ?? "").Trim()] = Math.Round(e.MinimumVerkoopprijs, 0);
            var grens = DateTime.Today.AddMonths(-12);
            var refs = await _uow.BudgetPrijsReferentie.GetNoTracking().Where(r => !r.Gearchiveerd && (r.ProjectId == null || r.ProjectId == versie.ProjectId)).ToListAsync();
            foreach (var l in vlijnen)
            {
                var naam = (l.EenheidNaam ?? "").Trim();
                if ((l.Vraagprijs ?? 0m) <= 0m) Add(8, MeldingType.Waarschuwing, "zonder-vraagprijs", "Eenheid zonder vraagprijs.", "rij", l.Id.ToString());
                else if (minima.TryGetValue(naam, out var min) && min > 0m && l.Vraagprijs < min) Add(8, MeldingType.Waarschuwing, "onder-minimum", $"Vraagprijs onder de minimumprijs van € {min:N0}.".Replace(',', '.'), "veld", l.Id.ToString());
                foreach (var (code, type) in new[] { (l.CodeBouw, "Bouw"), (l.CodeGrond, "Grond") })
                {
                    if (!code.HasValue) continue;
                    var r = refs.Where(x => x.PrijsType == type && x.Code == code.Value).OrderByDescending(x => x.ProjectId.HasValue).FirstOrDefault();
                    if (r?.Datum != null && r.Datum.Value < grens) Add(8, MeldingType.Waarschuwing, "prijscode-oud", $"Prijscode {(type == "Bouw" ? "B" : "G")}-{code.Value:00} is ouder dan 12 maanden ({r.Datum.Value:dd/MM/yyyy}).", "label", l.Id.ToString());
                }
            }

            // ── Stap 9 · Resultaat ─────────────────────────────────────────────────────────────────────
            var kost = (await _berekening.BerekenAsync(versieId)).TotaalKosten;
            var opbrengst = vlijnen.Where(l => (l.Vraagprijs ?? 0m) > 0m).Sum(l => l.Vraagprijs.Value);
            if (opbrengst > 0m)
            {
                var marge = opbrengst - kost;
                if (marge < 0m) Add(9, MeldingType.Fout, "marge-negatief", $"De marge is negatief: de vraagprijzen dekken de kostprijs niet (€ {marge:N0}).".Replace(',', '.'), "kader");
                else
                {
                    var def = await _uow.BudgetVersies.GetNoTracking().FirstOrDefaultAsync(v => v.ProjectId == versie.ProjectId && v.Status == BudgetVersie.StatusDefinitief && v.Id != versieId);
                    if (def != null)
                    {
                        var defKost = (await _berekening.BerekenAsync(def.Id)).TotaalKosten;
                        var defOpbrengst = (await _uow.BudgetVerkoopLijn.GetNoTracking().Where(l => l.BudgetVersieId == def.Id && l.Vraagprijs > 0m).Select(l => l.Vraagprijs).ToListAsync()).Sum(x => x ?? 0m);
                        if (defOpbrengst > 0m)
                        {
                            var perc = marge / opbrengst; var defPerc = (defOpbrengst - defKost) / defOpbrengst;
                            if (defPerc - perc > 0.05m) Add(9, MeldingType.Waarschuwing, "marge-vs-definitief", $"De marge ({perc * 100m:0.#} %) ligt meer dan 5 procentpunt onder die van het definitieve budget ({defPerc * 100m:0.#} %).".Replace('.', ','), "kader");
                        }
                    }
                }
            }

            // Genegeerde waarschuwingen (per code) markeren
            var genegeerd = ParseGenegeerd(versie.WaarschuwingenBevestigd);
            foreach (var m in res.Where(m => m.Type == MeldingType.Waarschuwing && m.Code != null && genegeerd.ContainsKey(m.Code)))
            { var (door, op) = genegeerd[m.Code]; m.Genegeerd = true; m.GenegeerdDoor = door; m.GenegeerdOp = op; }
            return res;
        }

        private async Task<bool> PoortenOntbreektAsync(int versieId)
        {
            var typePoorten = await _uow.BudgetGegevens.GetNoTracking().Where(g => g.BudgetVersieId == versieId).Select(g => g.TypePoorten).SingleOrDefaultAsync();
            var heeftGarage = await _uow.BudgetOppervlaktes.GetNoTracking().AnyAsync(o => o.BudgetVersieId == versieId && o.UnitType != null && o.UnitType.Name.Contains("garage"));
            return heeftGarage && (string.IsNullOrWhiteSpace(typePoorten) || typePoorten == "Geen");
        }
    }
}
