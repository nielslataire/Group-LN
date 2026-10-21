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
    /// <summary>Bandbreedte op de perceeloppervlakte bij woningen (±30 %): de grond bepaalt mee de prijs, niet enkel de bewoonbare oppervlakte.</summary>
    public const decimal MarktPerceelBand = 0.30m;

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
            var lijnen = await HerberekendeLijnenAsync(budgetVersieId);
            return BouwSamenvatting(voorstel, lijnen);
        }

        /// <summary>
        /// Stap 8 herrekent bij het openen elke lijn met de huidige oppervlaktes (€/m² × gereduceerde of grondoppervlakte); in de database staat nog wat laatst
        /// bewaard werd. Resultaat, Excel en PDF moeten dezelfde getallen zien als stap 8, dus passen ze dezelfde rekenregels toe.
        /// </summary>
        public Task<List<DALCore.Models.BudgetVerkoopLijn>> HerberekendeLijnenAsync(int budgetVersieId)
        {
            var lijnen = _uow.BudgetVerkoopLijn.GetNoTracking().Where(l => l.BudgetVersieId == budgetVersieId).ToList();
            var oppResp = _budgetService.GetBudgetOppervlaktes(budgetVersieId);
            var opps = (oppResp.Success && oppResp.Values != null ? oppResp.Values : new List<BudgetOppervlaktesBO>())
                .Where(o => !string.IsNullOrWhiteSpace(o.EenheidNaam))
                .GroupBy(o => o.EenheidNaam.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var l in lijnen)
            {
                if (opps.TryGetValue((l.EenheidNaam ?? "").Trim(), out var o)) VerkoopLijnRekenregel.Herbereken(l, o.OppGereduceerd, o.Grondopp, o.GrondoppAgrarisch);
                else VerkoopLijnRekenregel.Herbereken(l, 0m, 0m);
            }
            return Task.FromResult(lijnen);
        }

        /// <summary>
        /// Grond- en bouwwaarde waarbij de ingevulde marges de effectieve NETTO marges zijn: de verkoopkosten (makelaarscommissie, met btw enkel op het grondaandeel,
        /// plus overige verkoopkosten) zitten al in de prijs. Per deel: waarde = (kost × (1 + marge) + aandeel overige) / (1 − commissie × btw-factor).
        /// Zonder commissie en overige is dit gewoon kost × (1 + marge), met de financiering (straight loan, Wet Breyne) tegen kostprijs: daarop komt geen marge.
        /// </summary>
        public static (decimal Grond, decimal Bouw) Waarden(decimal grondKost, decimal bouwKost, decimal grondMarge, decimal bouwMarge, decimal commissie, decimal overige, decimal grondFinanciering = 0m, decimal bouwFinanciering = 0m)
        {
            var kost = grondKost + bouwKost;
            var overigeGrond = kost > 0m ? overige * grondKost / kost : 0m;
            var overigeBouw = overige - overigeGrond;
            var noemerGrond = 1m - commissie * (1m + VerkoopResultaat.BtwCommissie);   // btw op het grondaandeel is niet aftrekbaar: kost
            var noemerBouw = 1m - commissie;                                           // btw op het bouwaandeel is aftrekbaar: geen kost
            if (noemerGrond <= 0m) noemerGrond = 1m;
            if (noemerBouw <= 0m) noemerBouw = 1m;
            // Op de financiering (straight loan, Wet Breyne) wordt geen marge genomen: ze zit tegen kostprijs in de waarde.
            var grondMetMarge = Math.Max(0m, grondKost - grondFinanciering);
            var bouwMetMarge = Math.Max(0m, bouwKost - bouwFinanciering);
            return (Math.Round((grondKost + grondMetMarge * grondMarge + overigeGrond) / noemerGrond, 2),
                    Math.Round((bouwKost + bouwMetMarge * bouwMarge + overigeBouw) / noemerBouw, 2));
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
                    Grondwaarde = (l.Grondwaarde.HasValue || l.GrondwaardeAgrarisch.HasValue) ? (l.Grondwaarde ?? 0m) + (l.GrondwaardeAgrarisch ?? 0m) : (decimal?)null,
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
        /// Per eenheid worden verkochte en te koop staande units samen genomen (minstens 3), van nauw naar breed:
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
                // Woningen: eerst ook op perceelgrootte vergelijken (en opp. + perceel samen), want de grond bepaalt mee de prijs
                var perceel = e.Grondopp + e.GrondoppAgrarisch;
                Func<MarktReferentieUnitBO, bool> perceelOk = u => u.LandArea.HasValue
                    && u.LandArea.Value >= perceel * (1m - MarktPerceelBand) && u.LandArea.Value <= perceel * (1m + MarktPerceelBand);
                Func<MarktReferentieUnitBO, bool> oppOk = u => u.LivingArea.HasValue
                    && u.LivingArea.Value >= opp * (1m - MarktOppBand) && u.LivingArea.Value <= opp * (1m + MarktOppBand);
                if (type == "Woning" && perceel > 0m)
                {
                    if (opp > 0m) filters.Add(("zelfde type, opp. ±20 %, perceel ±30 %", u => u.PropertyType == type && oppOk(u) && perceelOk(u)));
                    filters.Add(("zelfde type, perceel ±30 %", u => u.PropertyType == type && perceelOk(u)));
                }
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

                    // Verkochte én te koop staande units tellen samen mee (minstens 3 per stap): enkel verkochte units geven een te smalle,
                    // vertraagde set; het aanbod laat zien waar de markt nu staat.
                    if (v.Count + k.Count >= 3)
                    {
                        gekozen = v.Concat(k).ToList();
                        e.MarktBasis = v.Count == 0 ? "te koop" : k.Count == 0 ? "verkocht" : "te koop + verkocht";
                    }

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
            // Opmeting en sondering zijn sinds migratie 092 percentages (posten in KostenOpBouw) maar blijven grondgebonden kosten
            decimal grondForfaits = (p.InfrastructuurForfait ?? 0m)
                + resultaat.KostenOpBouw.Where(k => string.Equals(k.Omschrijving, "Opmeting", StringComparison.OrdinalIgnoreCase) || string.Equals(k.Omschrijving, "Sondering", StringComparison.OrdinalIgnoreCase)).Sum(k => k.Bedrag);
            decimal slGrond       = resultaat.Financiering
                .Where(f => (f.Omschrijving ?? "").StartsWith("Straight loan grond", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Bedrag);

            // De aankoopprijs zit sinds okt. 2026 ook in resultaat.TotaalKosten (groep Grond): daar weer uithalen, zodat
            // GrondKost + BouwKost = TotaalKosten blijft kloppen en de grond niet dubbel telt.
            decimal grondInResultaat = resultaat.Grond.Sum(g => g.Bedrag);
            // Registratierechten en notariskosten staan als grondposten in het resultaat (naast de aankoopprijs zelf)
            decimal grondExtra = resultaat.Grond.Where(g => !string.Equals(g.Omschrijving, "Aankoopprijs grond", StringComparison.OrdinalIgnoreCase)).Sum(g => g.Bedrag);
            bo.GrondKost = grondAankoop + grondExtra + grondForfaits + slGrond;
            bo.BouwKost  = resultaat.TotaalKosten - grondInResultaat - grondForfaits - slGrond;
            if (bo.BouwKost < 0m) bo.BouwKost = 0m;

            // De ingevulde marges zijn netto: de verkoopkosten (commissie + overige, zie Parameters › Verkoopkosten) zitten al in de waarden
            (bo.Grondwaarde, bo.Bouwwaarde) = Waarden(bo.GrondKost, bo.BouwKost, bo.GrondMargePerc, bo.DoelMargePerc, p.MakelaarsCommissiePerc ?? 0m, p.OverigeVerkoopkosten ?? 0m, slGrond, resultaat.Financiering.Sum(f => f.Bedrag) - slGrond);

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
            // Een tweede grondzone (bv. agrarisch) telt mee naar haar relatieve waarde (standaard 20 % van de eerste zone)
            var agrGewicht = p.GrondAgrarischPerc ?? 0.20m;
            bo.AgrarischGewicht = agrGewicht;
            var somGrond = rijen.Sum(r => r.Grondopp + r.GrondoppAgrarisch * agrGewicht);
            if (somGrond > 0m)
            {
                grondBasis = r => r.Grondopp + r.GrondoppAgrarisch * agrGewicht;
                bo.VerdeelsleutelGrond = rijen.Any(r => r.GrondoppAgrarisch > 0m)
                    ? "grondoppervlakte (tweede zone telt voor " + (agrGewicht * 100m).ToString("0.##") + " %)" : "grondoppervlakte";
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
                    GrondoppAgrarisch = r.GrondoppAgrarisch,
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
