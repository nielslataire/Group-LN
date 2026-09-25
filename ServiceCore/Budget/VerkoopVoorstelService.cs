using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOCore;
using BOCore.Budget;
using FacadeCore;

namespace ServiceCore.Budget
{
    /// <summary>
    /// Bottom-up verkoopvoorstel: wat moet elke eenheid minstens opbrengen om de kostprijs
    /// plus de doelmarge te dekken, gesplitst in grondwaarde en bouwwaarde.
    ///
    /// Rekenregels:
    ///   GrondKost   = aankoopprijs grond + infrastructuurforfait + opmeting/sondering + straight loan grond
    ///   BouwKost    = totale kostprijs uit BudgetBerekeningService − de grondgebonden posten hierboven
    ///                 (dus: bouwkost activiteiten, erelonen, verzekeringen, publiciteit, liften,
    ///                  Wet Breyne, straight loan gebouw, onvoorzien)
    ///   Grondwaarde = GrondKost × (1 + grondmarge)      verdeeld op grondoppervlakte per eenheid
    ///   Bouwwaarde  = BouwKost  × (1 + doelmarge)       verdeeld op gereduceerde oppervlakte per eenheid
    ///                 (BudgetOppervlaktesBO.OppGereduceerd: de bestaande wegingsconventie van de wizard)
    ///   Minimumverkoopprijs per eenheid = grondwaarde + bouwwaarde
    ///
    /// Gemeenschappelijke delen en wegenis zitten in de gereduceerde oppervlakte van de rij waar ze
    /// ingevuld zijn; wie die als aparte rij ingeeft, ziet daar dus ook een waarde-aandeel op.
    /// </summary>
    public class VerkoopVoorstelService : IVerkoopVoorstelService
    {
        private readonly BudgetBerekeningService _berekening;
        private readonly IBudgetService _budgetService;
        private readonly DALCore.UnitOfWorkCore _uow;
        private readonly IMarktReferentieService _markt;
        private readonly IUnitService _unitService;

        /// <summary>Periode waarbinnen een verkoop meetelt als marktreferentie.</summary>
        public const int MarktPeriodeMaanden = 12;

        /// <summary>Oppervlakteband (±) waarbinnen een unit "vergelijkbaar" is.</summary>
        public const decimal MarktOppBand = 0.20m;

        public VerkoopVoorstelService(
            BudgetBerekeningService berekening,
            IBudgetService budgetService,
            DALCore.UnitOfWorkCore uow,
            IMarktReferentieService markt = null,
            IUnitService unitService = null)
        {
            _berekening    = berekening;
            _budgetService = budgetService;
            _uow           = uow;
            _markt         = markt;
            _unitService   = unitService;
        }

        // ── Samenvatting (stap 9, vergelijking, PDF/Excel) ────────────────────

        public async Task<BudgetVerkoopSamenvattingBO> SamenvattingAsync(int budgetVersieId)
        {
            var voorstel = await BerekenAsync(budgetVersieId);
            var lijnen = _uow.BudgetVerkoopLijn.GetNoTracking()
                .Where(l => l.BudgetVersieId == budgetVersieId)
                .ToList();
            return BouwSamenvatting(voorstel, lijnen);
        }

        /// <summary>Pure functie (unit-getest): voorstel + verkooplijnen → opbrengst en marge.</summary>
        public static BudgetVerkoopSamenvattingBO BouwSamenvatting(
            BudgetVerkoopVoorstelBO voorstel,
            IReadOnlyList<DALCore.Models.BudgetVerkoopLijn> lijnen)
        {
            var metPrijs = (lijnen ?? new List<DALCore.Models.BudgetVerkoopLijn>())
                .Where(l => l.Vraagprijs is > 0m)
                .ToList();

            return new BudgetVerkoopSamenvattingBO
            {
                TotaalKostprijsInclGrond   = voorstel.TotaalKostprijs,
                Grondwaarde                = voorstel.Grondwaarde,
                Bouwwaarde                 = voorstel.Bouwwaarde,
                MinimaleVerkoopwaarde      = voorstel.TotaalVerkoopwaarde,
                MarktTotaal                = voorstel.MarktTotaal,
                TotaalAanbevolenVraagprijs = voorstel.TotaalAanbevolenVraagprijs,
                AantalEenheden             = voorstel.AantalEenheden,
                VraagprijzenLijnen         = metPrijs.Count > 0 ? metPrijs.Sum(l => l.Vraagprijs.Value) : (decimal?)null,
                AantalLijnenMetVraagprijs  = metPrijs.Count
            };
        }

        // ── Doorzetten naar Units ─────────────────────────────────────────────

        public async Task<Response> DoorzettenNaarUnitsAsync(int budgetVersieId)
        {
            var response = new Response();
            if (_unitService is null)
            {
                response.AddError("Unit-service niet beschikbaar.");
                return response;
            }

            var versie = _uow.BudgetVersies.GetNoTracking().FirstOrDefault(v => v.Id == budgetVersieId);
            if (versie is null)
            {
                response.AddError("Budgetversie niet gevonden.");
                return response;
            }

            var lijnen = _uow.BudgetVerkoopLijn.GetNoTracking()
                .Where(l => l.BudgetVersieId == budgetVersieId && l.UnitId != null
                         && (l.Grondwaarde != null || l.Bouwwaarde != null))
                .ToList();

            if (lijnen.Count == 0)
            {
                response.AddError("Geen verkooplijnen met een gekoppelde eenheid én een grond- of bouwwaarde. Neem eerst het voorstel over en kies per lijn de eenheid.");
                return response;
            }

            var waarden = lijnen
                .GroupBy(l => l.UnitId!.Value)
                .Select(g => g.Last())
                .Select(l => new UnitBudgetWaardeBO
                {
                    UnitId      = l.UnitId!.Value,
                    EenheidNaam = l.EenheidNaam,
                    Grondwaarde = l.Grondwaarde,
                    Bouwwaarde  = l.Bouwwaarde
                })
                .ToList();

            return await Task.FromResult(_unitService.UpdateUnitBudgetWaarden(versie.ProjectId, waarden));
        }

        public async Task<BudgetVerkoopVoorstelBO> BerekenAsync(int budgetVersieId)
        {
            var resultaat = await _berekening.BerekenAsync(budgetVersieId);
            var p         = await _berekening.GetOrCreateParamsAsync(budgetVersieId);
            var oppResp   = _budgetService.GetBudgetOppervlaktes(budgetVersieId);
            var rijen     = oppResp.Success && oppResp.Values != null
                ? oppResp.Values.ToList()
                : new List<BudgetOppervlaktesBO>();

            var bo = Bereken(budgetVersieId, resultaat, p, rijen);

            // ── Marktreferentie (fail-safe: het voorstel blijft bruikbaar zonder markt) ──
            if (_markt is not null)
            {
                try
                {
                    var versie = _uow.BudgetVersies.GetNoTracking().FirstOrDefault(v => v.Id == budgetVersieId);
                    if (versie is not null)
                    {
                        var markt = await _markt.HaalOpAsync(versie.ProjectId, MarktPeriodeMaanden);
                        VerrijkMetMarkt(bo, markt);
                    }
                }
                catch (Exception ex)
                {
                    bo.Waarschuwingen.Add("Marktreferentie niet beschikbaar: " + ex.Message);
                }
            }

            return bo;
        }

        // ── Marktreferentie ────────────────────────────────────────────────────

        /// <summary>
        /// Vult gemeentecijfers en per eenheid de markt-mediaan in. Pure functie (unit-getest).
        /// Per eenheid wordt gezocht naar minstens 5 vergelijkbare units, in deze volgorde van
        /// voorkeur: verkocht in de periode → te koop → beide samen; en van nauw naar breed:
        /// zelfde type met oppervlakte ±20 % → zelfde type → alle types. Vanaf 3 units wordt
        /// een mediaan gegeven maar als "beperkt" gemarkeerd via MarktAantal.
        /// </summary>
        public static void VerrijkMetMarkt(BudgetVerkoopVoorstelBO bo, MarktReferentieBO markt)
        {
            bo.Markt = markt;
            if (markt is null) return;

            if (!markt.HeeftData)
            {
                bo.Waarschuwingen.Add("Geen marktreferentie: " + (markt.Bron ?? "geen marktdata voor deze gemeente."));
                return;
            }

            var units = markt.Units.Where(u => u.PrijsPerM2 is > 0).ToList();
            var verkocht = units.Where(u => u.IsVerkocht).ToList();
            var teKoop   = units.Where(u => !u.IsVerkocht).ToList();

            // Gemeentecijfers: enkel de marktdata (eigen verkopen hebben geen verkoopdatum en horen niet in absorptie)
            markt.AantalTeKoop      = markt.Units.Count(u => !u.IsVerkocht);
            markt.VerkochtInPeriode = markt.Units.Count(u => u.IsVerkocht && !u.IsEigenVerkoop);
            markt.AbsorptiePerMaand = markt.PeriodeMaanden > 0
                ? Math.Round((decimal)markt.VerkochtInPeriode / markt.PeriodeMaanden, 1)
                : (decimal?)null;
            markt.MediaanDoorlooptijdDagen = MediaanInt(markt.Units.Where(u => u.IsVerkocht && u.DoorlooptijdDagen.HasValue).Select(u => u.DoorlooptijdDagen.Value));
            markt.MediaanPrijsPerM2TeKoop   = Mediaan(teKoop.Select(u => u.PrijsPerM2.Value));
            markt.MediaanPrijsPerM2Verkocht = Mediaan(verkocht.Where(u => !u.IsEigenVerkoop).Select(u => u.PrijsPerM2.Value));

            // Eigen verkopen (werkelijke verkoopprijzen) apart zichtbaar; ze zitten ook in de "verkocht"-pool per eenheid.
            var eigen = units.Where(u => u.IsEigenVerkoop).ToList();
            markt.EigenVerkopen = eigen.Count;
            markt.MediaanPrijsPerM2EigenVerkoop = Mediaan(eigen.Select(u => u.PrijsPerM2.Value));

            foreach (var e in bo.Eenheden)
            {
                var type = ClassificeerType(e.TypeName, e.GroupTypeName);
                var opp  = e.BewoonbareOpp;

                // Kandidaatfilters van nauw naar breed
                var filters = new List<(string Label, Func<MarktReferentieUnitBO, bool> Pred)>();
                if (type is not null && opp > 0m)
                    filters.Add(("zelfde type, opp. ±20 %", u => u.PropertyType == type && u.LivingArea.HasValue
                        && u.LivingArea.Value >= opp * (1m - MarktOppBand) && u.LivingArea.Value <= opp * (1m + MarktOppBand)));
                if (type is not null)
                    filters.Add(("zelfde type", u => u.PropertyType == type));
                if (opp > 0m)
                    filters.Add(("alle types, opp. ±20 %", u => u.LivingArea.HasValue
                        && u.LivingArea.Value >= opp * (1m - MarktOppBand) && u.LivingArea.Value <= opp * (1m + MarktOppBand)));
                filters.Add(("alle types", _ => true));

                List<decimal> gekozen = null;
                foreach (var (label, pred) in filters)
                {
                    var v = verkocht.Where(pred).Select(u => u.PrijsPerM2.Value).ToList();
                    var k = teKoop.Where(pred).Select(u => u.PrijsPerM2.Value).ToList();

                    if (v.Count >= 5)      { gekozen = v; e.MarktBasis = "verkocht"; }
                    else if (k.Count >= 5) { gekozen = k; e.MarktBasis = "te koop"; }
                    else if (v.Count + k.Count >= 3) { gekozen = v.Concat(k).ToList(); e.MarktBasis = "te koop + verkocht"; }

                    if (gekozen is not null) { e.MarktVergelijking = label; break; }
                }

                if (gekozen is null)
                {
                    e.MarktAantal = 0;
                    continue;
                }

                e.MarktAantal      = gekozen.Count;
                e.MarktMediaanPerM2 = Mediaan(gekozen);
                e.MarktP25PerM2     = Percentiel(gekozen, 0.25m);
                e.MarktP75PerM2     = Percentiel(gekozen, 0.75m);
            }

            if (bo.Eenheden.Count > 0 && bo.AantalEenhedenMetMarkt == 0)
                bo.Waarschuwingen.Add("Te weinig vergelijkbare units in de marktdata om een marktprijs per eenheid te geven.");
        }

        /// <summary>"Appartement" / "Woning" op basis van unittype- en groepsnaam; null = onbekend (alle types).</summary>
        public static string ClassificeerType(string typeName, string groupTypeName)
        {
            var t = ((typeName ?? "") + " " + (groupTypeName ?? "")).ToLowerInvariant();
            if (t.Contains("appart") || t.Contains("studio") || t.Contains("penthouse") || t.Contains("duplex") || t.Contains("flat"))
                return "Appartement";
            if (t.Contains("woning") || t.Contains("huis") || t.Contains("villa") || t.Contains("bungalow"))
                return "Woning";
            return null;
        }

        private static decimal? Mediaan(IEnumerable<decimal> waarden) => Percentiel(waarden, 0.5m);

        private static int? MediaanInt(IEnumerable<int> waarden)
        {
            var m = Percentiel(waarden.Select(w => (decimal)w), 0.5m);
            return m.HasValue ? (int)Math.Round(m.Value) : (int?)null;
        }

        /// <summary>Percentiel met lineaire interpolatie; null bij lege reeks.</summary>
        private static decimal? Percentiel(IEnumerable<decimal> waarden, decimal p)
        {
            var lijst = waarden.OrderBy(v => v).ToList();
            if (lijst.Count == 0) return null;
            if (lijst.Count == 1) return lijst[0];
            var pos  = p * (lijst.Count - 1);
            var lo   = (int)Math.Floor(pos);
            var hi   = Math.Min(lo + 1, lijst.Count - 1);
            var frac = pos - lo;
            return Math.Round(lijst[lo] + (lijst[hi] - lijst[lo]) * frac, 2);
        }

        /// <summary>
        /// Pure berekening (geen I/O) — apart gehouden zodat de rekenregels unit-testbaar zijn.
        /// </summary>
        public static BudgetVerkoopVoorstelBO Bereken(
            int budgetVersieId,
            BudgetResultaatBO resultaat,
            DALCore.Models.BudgetParams p,
            IReadOnlyList<BudgetOppervlaktesBO> rijen)
        {
            var bo = new BudgetVerkoopVoorstelBO
            {
                BudgetVersieId = budgetVersieId,
                DoelMargePerc  = p.DoelMargePerc  ?? 0m,
                GrondMargePerc = p.GrondMargePerc ?? 0m
            };

            if (p.DoelMargePerc is null)
                bo.Waarschuwingen.Add("Geen doelmarge ingesteld (Parameters of Instellingen > Bouwkost %): bouwwaarde = bouwkost zonder marge.");
            if (!p.AankoopprijsGrond.HasValue || p.AankoopprijsGrond.Value == 0m)
                bo.Waarschuwingen.Add("Aankoopprijs grond is 0: de grondwaarde bevat enkel de grondgebonden kosten.");

            // ── Kostprijs splitsen in grond en bouw ──────────────────────────
            decimal grondAankoop  = p.AankoopprijsGrond ?? 0m;
            decimal grondForfaits = (p.InfrastructuurForfait ?? 0m) + (p.OpmetingSonderingForfait ?? 0m);
            decimal slGrond       = resultaat.Financiering
                .Where(f => (f.Omschrijving ?? "").StartsWith("Straight loan grond", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Bedrag);

            bo.GrondKost = grondAankoop + grondForfaits + slGrond;
            bo.BouwKost  = resultaat.TotaalKosten - grondForfaits - slGrond;
            if (bo.BouwKost < 0m) bo.BouwKost = 0m;

            bo.Grondwaarde = Math.Round(bo.GrondKost * (1m + bo.GrondMargePerc), 2);
            bo.Bouwwaarde  = Math.Round(bo.BouwKost  * (1m + bo.DoelMargePerc), 2);

            if (rijen.Count == 0)
            {
                bo.Waarschuwingen.Add("Geen eenheden op het tabblad Oppervlaktes: geen verdeling per eenheid mogelijk.");
                bo.VerdeelsleutelBouw  = "—";
                bo.VerdeelsleutelGrond = "—";
                return bo;
            }

            // ── Verdeelsleutels ─────────────────────────────────────────────
            Func<BudgetOppervlaktesBO, decimal> bouwBasis;
            var somGereduceerd = rijen.Sum(r => r.OppGereduceerd);
            var somBewoonbaar  = rijen.Sum(r => r.BewoonbareOpp);
            if (somGereduceerd > 0m)
            {
                bouwBasis = r => r.OppGereduceerd;
                bo.VerdeelsleutelBouw = "gereduceerde oppervlakte";
            }
            else if (somBewoonbaar > 0m)
            {
                bouwBasis = r => r.BewoonbareOpp;
                bo.VerdeelsleutelBouw = "bewoonbare oppervlakte";
            }
            else
            {
                bouwBasis = _ => 1m;
                bo.VerdeelsleutelBouw = "gelijk per eenheid";
                bo.Waarschuwingen.Add("Geen oppervlaktes ingevuld: bouwwaarde gelijk verdeeld over de eenheden.");
            }

            Func<BudgetOppervlaktesBO, decimal> grondBasis;
            var somGrond = rijen.Sum(r => r.Grondopp);
            if (somGrond > 0m)
            {
                grondBasis = r => r.Grondopp;
                bo.VerdeelsleutelGrond = "grondoppervlakte";
            }
            else
            {
                grondBasis = bouwBasis;
                bo.VerdeelsleutelGrond = bo.VerdeelsleutelBouw;
                if (bo.GrondKost > 0m)
                    bo.Waarschuwingen.Add("Geen grondoppervlakte per eenheid ingevuld: grondwaarde verdeeld volgens de " + bo.VerdeelsleutelBouw + ".");
            }

            var totBouwBasis  = rijen.Sum(bouwBasis);
            var totGrondBasis = rijen.Sum(grondBasis);

            foreach (var r in rijen)
            {
                var aandeelBouw  = totBouwBasis  > 0m ? bouwBasis(r)  / totBouwBasis  : 0m;
                var aandeelGrond = totGrondBasis > 0m ? grondBasis(r) / totGrondBasis : 0m;

                bo.Eenheden.Add(new BudgetVerkoopVoorstelEenheidBO
                {
                    EenheidNaam    = r.EenheidNaam,
                    GroupTypeName  = r.GroupTypeName,
                    TypeName       = r.TypeName,
                    BewoonbareOpp  = r.BewoonbareOpp,
                    OppGereduceerd = r.OppGereduceerd,
                    Grondopp       = r.Grondopp,
                    AandeelBouw    = aandeelBouw,
                    AandeelGrond   = aandeelGrond,
                    KostprijsBouw  = Math.Round(bo.BouwKost   * aandeelBouw,  2),
                    KostprijsGrond = Math.Round(bo.GrondKost  * aandeelGrond, 2),
                    Bouwwaarde     = Math.Round(bo.Bouwwaarde * aandeelBouw,  2),
                    Grondwaarde    = Math.Round(bo.Grondwaarde * aandeelGrond, 2)
                });
            }

            return bo;
        }
    }
}
