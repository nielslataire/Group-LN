using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOCore.Budget;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public class BudgetBerekeningService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly BudgetActivityService _activityService;

        public BudgetBerekeningService(UnitOfWorkCore uow, BudgetActivityService activityService)
        {
            _uow = uow;
            _activityService = activityService;
        }

        public async Task<BudgetResultaatBO> BerekenAsync(int budgetVersieId)
        {
            var versie = await _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetGegevens)
                .FirstOrDefaultAsync(v => v.Id == budgetVersieId);

            var opps = await _uow.BudgetOppervlaktes.GetNoTracking()
                .Include(o => o.UnitGroupType)
                .Where(o => o.BudgetVersieId == budgetVersieId)
                .ToListAsync();
            var aantalEenheden = opps.Count;
            var aantalWoonComm = BudgetActivityService.TelWoonCommEenheden(opps);
            var totaalGBA      = opps.Sum(o => o.BewoonbareOpp);

            // Bouwkost = de effectieve, gecorrigeerde activiteitentotalen zoals op stap 6
            // (opgeslagen alt.prijs óf het voorstel/formule-bedrag als er nog niets is bewaard).
            decimal totaalBouw = await _activityService.GetTotaalGecorrigeerdeBouwAsync(budgetVersieId);
            // Basis voor alle percentages van Parameters: zonder EPB, veiligheidscoordinatie en ventilatieverslaggeving (die staan wel in het totaal).
            decimal pctBasis = await _activityService.GetTotaalGecorrigeerdeBouwAsync(budgetVersieId, alsPercentageBasis: true);

            var p = await GetOrCreateParamsAsync(budgetVersieId);

            var result = new BudgetResultaatBO
            {
                BudgetVersieId         = budgetVersieId,
                AantalEenheden         = aantalEenheden,
                AantalWoonCommEenheden = aantalWoonComm,
                TotaalGBA              = totaalGBA,
                TotaalBouw             = totaalBouw
            };

            // B) Kosten op bouwkost
            void AddPerc(string omschrijving, decimal? perc, int? activityId = null)
            {
                decimal bedrag = pctBasis * (perc ?? 0m);
                if (perc.HasValue && perc.Value != 0m)
                    result.KostenOpBouw.Add(new BudgetKostenPostBO
                    {
                        Omschrijving = omschrijving,
                        Bedrag       = bedrag,
                        Percentage   = perc.Value,
                        IsPerc       = true,
                        BasisBedrag  = pctBasis,
                        ActivityId   = activityId
                    });
            }
            AddPerc("Projectcoördinatie", p.ProjectcoordinatiePerc, BudgetAlgemeneActiviteit.Projectcoordinatie);
            AddPerc("Architect",          p.ArchitectPerc, BudgetAlgemeneActiviteit.Architect);
            // Veiligheidscoördinatie + EPB en ventilatieverslaggeving zijn sinds migratie 091 activiteiten (lot 0 Algemeen) met een formule per eenheid:
            // niet meer als percentage/forfait tellen, anders staan ze dubbel in het budget.
            AddPerc("Ingenieur",          p.StudieIRPerc, BudgetAlgemeneActiviteit.Ingenieur);
            AddPerc("Decennale gesloten ruwbouw", p.DecennaleGeslRuwbouwPerc, BudgetAlgemeneActiviteit.Decennale);
            AddPerc("ABR + plaatsbeschrijving",   p.ABRPlaatsbeschrPerc, BudgetAlgemeneActiviteit.Abr);
            AddPerc("Opmeting",                   p.OpmetingPerc, BudgetAlgemeneActiviteit.Opmeting);
            AddPerc("Sondering",                  p.SonderingPerc, BudgetAlgemeneActiviteit.Sondering);

            // C) Forfaits
            void AddForfait(string omschrijving, decimal? bedrag)
            {
                if (bedrag.HasValue && bedrag.Value != 0m)
                    result.Forfaits.Add(new BudgetKostenPostBO
                    {
                        Omschrijving = omschrijving,
                        Bedrag       = bedrag.Value,
                        IsPerc       = false
                    });
            }
            AddForfait("Infrastructuur",         p.InfrastructuurForfait);

            int aantalLiften = versie?.BudgetGegevens?.AantalLiften ?? 0;
            if (aantalLiften > 0 && p.LiftPrijsPerStuk.HasValue && p.LiftPrijsPerStuk.Value != 0m)
                result.Forfaits.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Liften ({aantalLiften} st.)",
                    Bedrag       = aantalLiften * p.LiftPrijsPerStuk.Value,
                    IsPerc       = false
                });

            // D) Financiering
            decimal wetBreyne = pctBasis * p.WetBreynePerc * ((p.WetBreyneMaanden ?? 0) / 12m);
            if (wetBreyne != 0m)
                result.Financiering.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Wet Breyne ({p.WetBreyneMaanden ?? 0} mnd)",
                    Bedrag       = wetBreyne,
                    Percentage   = p.WetBreynePerc,
                    IsPerc       = true,
                    BasisBedrag  = pctBasis,
                    ActivityId   = BudgetAlgemeneActiviteit.FinancieringWetBreyne
                });

            decimal slGebouw = pctBasis * p.StraightloanGebouwPerc * ((p.StraightloanGebouwMaanden ?? 0) / 12m);
            if (slGebouw != 0m)
                result.Financiering.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Straight loan gebouw ({p.StraightloanGebouwMaanden ?? 0} mnd)",
                    Bedrag       = slGebouw,
                    Percentage   = p.StraightloanGebouwPerc,
                    IsPerc       = true,
                    BasisBedrag  = pctBasis,
                    ActivityId   = BudgetAlgemeneActiviteit.FinancieringStraightloan
                });

            // De aankoop kan uit meerdere delen (akten) bestaan; AankoopprijsGrond is het totaal ervan.
            var aankopen = NotarisTarief.Aankopen(p.GrondAankopenJson, p.AankoopprijsGrond ?? 0m);
            decimal grond = aankopen.Sum(a => a.P);
            decimal slGrond = grond * p.StraightloanGrondPerc * ((p.StraightloanGrondMaanden ?? 0) / 12m);
            if (slGrond != 0m)
                result.Financiering.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Straight loan grond ({p.StraightloanGrondMaanden ?? 0} mnd)",
                    Bedrag       = slGrond,
                    Percentage   = p.StraightloanGrondPerc,
                    IsPerc       = true,
                    BasisBedrag  = grond,
                    ActivityId   = BudgetAlgemeneActiviteit.FinancieringStraightloan
                });

            // E) Onvoorzien — enkel op de bouwkost van de activiteiten (niet op erelonen of grond).
            result.Onvoorzien = pctBasis * p.OnvoorzienPerc;

            // F) Grond: de aankoopprijs is een kost zoals elke andere. Stond voordien enkel in het verkoopvoorstel
            // ("kostprijs incl. grond"), waardoor stap 9 en het voorstel een andere kostprijs toonden.
            if (grond != 0m)
                result.Grond.Add(new BudgetKostenPostBO
                {
                    Omschrijving = "Aankoopprijs grond",
                    Bedrag       = grond,
                    IsPerc       = false
                });

            // Grondkost (kaart "Grondkost" op Parameters): registratierechten (vast percentage, per budget aanpasbaar) en ereloon van de notaris (schaaltarief)
            if (grond > 0m)
            {
                var regPerc = p.RegistratierechtenPerc ?? 0m;
                if (regPerc > 0m)
                    result.Grond.Add(new BudgetKostenPostBO { Omschrijving = "Registratierechten", Bedrag = Math.Round(grond * regPerc, 2), Percentage = regPerc, IsPerc = true, BasisBedrag = grond });
                var ereloon = aankopen.Sum(a => NotarisTarief.EerelonInclBtw(a.P));
                result.Grond.Add(new BudgetKostenPostBO { Omschrijving = aankopen.Count > 1 ? $"Notariskosten (ereloon, {aankopen.Count} akten)" : "Notariskosten (ereloon)", Bedrag = ereloon, IsPerc = false });
                var akteKost = Math.Round((p.AktekostenPerAkte ?? NotarisTarief.AktekostenStandaard) * aankopen.Count * (1m + NotarisTarief.Btw), 2);   // per akte excl. btw ingegeven, kost incl. 21 % btw
                if (akteKost > 0m)
                    result.Grond.Add(new BudgetKostenPostBO { Omschrijving = aankopen.Count > 1 ? $"Notaris: aktekosten en uitgaven incl. btw ({aankopen.Count} akten)" : "Notaris: aktekosten en uitgaven incl. btw", Bedrag = akteKost, IsPerc = false });
            }

            // Publiciteit: percentage van de totale verkoopprijs. De verkoopprijs hangt zelf van de kost af (publiciteit zit in de bouwkost),
            // dus oplossen: V = V0 + P*(1+doelmarge) met P = f*V  =>  P = f*V0 / (1 - f*(1+doelmarge)), V0 = verkoopprijs zonder publiciteit.
            var pubPerc = p.PubliciteitPerc ?? 0m;
            if (pubPerc > 0m)
            {
                decimal dm = p.DoelMargePerc ?? 0m, gm = p.GrondMargePerc ?? 0m;
                decimal grondExtra0 = result.Grond.Where(g => !string.Equals(g.Omschrijving, "Aankoopprijs grond", StringComparison.OrdinalIgnoreCase)).Sum(g => g.Bedrag);
                decimal grondForf0 = (p.InfrastructuurForfait ?? 0m)
                    + result.KostenOpBouw.Where(k => string.Equals(k.Omschrijving, "Opmeting", StringComparison.OrdinalIgnoreCase) || string.Equals(k.Omschrijving, "Sondering", StringComparison.OrdinalIgnoreCase)).Sum(k => k.Bedrag);
                decimal grondKost0 = grond + grondExtra0 + grondForf0 + slGrond;
                decimal bouwKost0 = Math.Max(0m, result.TotaalKosten - result.Grond.Sum(g => g.Bedrag) - grondForf0 - slGrond);
                decimal com = p.MakelaarsCommissiePerc ?? 0m;
                var w0 = VerkoopVoorstelService.Waarden(grondKost0, bouwKost0, gm, dm, com, p.OverigeVerkoopkosten ?? 0m);
                decimal v0 = w0.Grond + w0.Bouw;
                decimal bouwFactor = (1m + dm) / (com < 1m ? 1m - com : 1m);   // publiciteit zit in de bouwkost: elke euro erbij geeft zoveel verkoopwaarde erbij
                decimal noemer = 1m - pubPerc * bouwFactor;
                if (noemer > 0m && v0 > 0m)
                {
                    var bedrag = Math.Round(pubPerc * v0 / noemer, 2);
                    result.Forfaits.Add(new BudgetKostenPostBO { Omschrijving = "Publiciteit", Bedrag = bedrag, Percentage = pubPerc, IsPerc = true, BasisBedrag = Math.Round(v0 + bedrag * bouwFactor, 2) });
                }
            }

            return result;
        }

        public async Task<List<BudgetVersie>> GetVersiesVoorMasterAsync(int budgetMasterId)
        {
            return await _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetGegevens)
                .Where(v => v.BudgetMasterId == budgetMasterId)
                .OrderByDescending(v => v.Versienummer)
                .ToListAsync();
        }

        public async Task<List<BudgetResultaatBO>> GetVergelijkingAsync(List<int> versieIds)
        {
            var resultaten = new List<BudgetResultaatBO>();
            foreach (var id in versieIds)
            {
                var versie = await _uow.BudgetVersies.GetNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == id);
                var r = await BerekenAsync(id);
                r.VersieNaam   = versie?.VersieNaam   ?? string.Empty;
                r.Versienummer = versie?.Versienummer ?? 0;
                resultaten.Add(r);
            }
            return resultaten;
        }

        /// <summary>Zoekt een standaardpercentage in Instellingen › Bouwkost % op sleutel of naam. Fractie, of null.</summary>
        public static decimal? ZoekStandaard(IEnumerable<BouwkostPercentage> rijen, params string[] woorden)
        {
            bool Match(string t) => !string.IsNullOrWhiteSpace(t) && woorden.Any(w => t.Trim().StartsWith(w, StringComparison.OrdinalIgnoreCase) || (w.Length > 5 && t.Contains(w, StringComparison.OrdinalIgnoreCase)));
            var rij = rijen.FirstOrDefault(r => Match(r.Sleutel)) ?? rijen.FirstOrDefault(r => Match(r.Naam));
            return rij != null && rij.Percentage > 0m ? rij.Percentage / 100m : (decimal?)null;
        }

        /// <summary>Standaard decennale (tienjarige) verzekering: bv. de rij "Wet Peeters (10-jarige)".</summary>
        public static decimal? ZoekDecennaleStandaard(IEnumerable<BouwkostPercentage> rijen)
            => ZoekStandaard(rijen, "decennale", "tienjarige", "10-jarige", "10jarige", "peeters");

        public async Task<BudgetParams> GetOrCreateParamsAsync(int budgetVersieId)
        {
            // Standaard-erelonen komen uit Instellingen > Bouwkost % (vaste systeemrijen).
            // Daar staan ze in procentpunten (5,25) — BudgetParams bewaart een fractie.
            var std = await _uow.BouwkostPercentages.GetNoTracking()
                .Where(p => p.Sleutel != null)
                .ToDictionaryAsync(p => p.Sleutel, p => p.Percentage / 100m);

            decimal StdFractie(string sleutel) =>
                std.TryGetValue(sleutel, out var v) ? v : 0m;

            var alleStd = await _uow.BouwkostPercentages.GetNoTracking().ToListAsync();
            var decStd = ZoekDecennaleStandaard(alleStd);
            var makStd = ZoekStandaard(alleStd, "makelaarscommissie", "makelaar") ?? 0.03m;
            var pubStd = ZoekStandaard(alleStd, "publiciteit") ?? 0.0004m;
            var regStd = ZoekStandaard(alleStd, "registratierechten", "registratie") ?? 0.12m;
            var abrStd = ZoekStandaard(alleStd, "abr");
            var opmStd = ZoekStandaard(alleStd, "opmeting");
            var sonStd = ZoekStandaard(alleStd, "sondering");

            var bestaand = await _uow.BudgetParams.GetNoTracking()
                .FirstOrDefaultAsync(p => p.BudgetVersieId == budgetVersieId);

            if (bestaand != null)
            {
                // Effectieve standaard tonen voor velden die dit budget nog nooit
                // expliciet ingevuld heeft (null), of nog op de oude vaste default
                // stonden. Zodra de gebruiker op Parameters opslaat, blijft z'n waarde.
                var archStd = StdFractie("architect");
                var ingStd  = StdFractie("ingenieur");
                var pcStd    = StdFractie("projectcoordinatie");
                var doelStd  = StdFractie("doelmarge");
                var grondStd = StdFractie("grondmarge");

                if (bestaand.ArchitectPerc == null && archStd != 0m) bestaand.ArchitectPerc = archStd;
                // Decennale: leeg/0 = de standaard uit Instellingen › Bouwkost % (tienjarige verzekering)
                if ((bestaand.DecennaleGeslRuwbouwPerc ?? 0m) <= 0m && decStd.HasValue) bestaand.DecennaleGeslRuwbouwPerc = decStd;
                if ((bestaand.ABRPlaatsbeschrPerc ?? 0m) <= 0m && abrStd.HasValue) bestaand.ABRPlaatsbeschrPerc = abrStd;
                if ((bestaand.OpmetingPerc ?? 0m) <= 0m && opmStd.HasValue) bestaand.OpmetingPerc = opmStd;
                if ((bestaand.RegistratierechtenPerc ?? 0m) <= 0m) bestaand.RegistratierechtenPerc = regStd;
                if ((bestaand.PubliciteitPerc ?? 0m) <= 0m) bestaand.PubliciteitPerc = pubStd;
                if (bestaand.MakelaarsCommissiePerc is null) bestaand.MakelaarsCommissiePerc = makStd;
                if ((bestaand.SonderingPerc ?? 0m) <= 0m && sonStd.HasValue) bestaand.SonderingPerc = sonStd;
                if (bestaand.StudieIRPerc  == null && ingStd  != 0m) bestaand.StudieIRPerc  = ingStd;
                if (bestaand.DoelMargePerc  == null && doelStd  != 0m) bestaand.DoelMargePerc  = doelStd;
                if (bestaand.GrondMargePerc == null && grondStd != 0m) bestaand.GrondMargePerc = grondStd;
                // Enkel een nog nooit ingevulde waarde (0) krijgt de standaard; een bewust ingevulde 5,25 % blijft staan
                // (voordien werd 0,0525 als "oude default" gezien en telkens overschreven).
                if (bestaand.ProjectcoordinatiePerc == 0m && pcStd != 0m)
                    bestaand.ProjectcoordinatiePerc = pcStd;

                return bestaand;
            }

            var nieuw = new BudgetParams
            {
                BudgetVersieId         = budgetVersieId,
                ProjectcoordinatiePerc = StdFractie("projectcoordinatie") is var pc && pc != 0m ? pc : 0.0525m,
                ArchitectPerc          = StdFractie("architect") is var a && a != 0m ? a : (decimal?)null,
                StudieIRPerc           = StdFractie("ingenieur")  is var i && i != 0m ? i : (decimal?)null,
                DoelMargePerc          = StdFractie("doelmarge")  is var dm && dm != 0m ? dm : (decimal?)null,
                GrondMargePerc         = StdFractie("grondmarge") is var gm && gm != 0m ? gm : (decimal?)null,
                DecennaleGeslRuwbouwPerc = decStd,
                ABRPlaatsbeschrPerc    = abrStd,
                OpmetingPerc           = opmStd,
                SonderingPerc          = sonStd,
                RegistratierechtenPerc = regStd,
                PubliciteitPerc        = pubStd,
                MakelaarsCommissiePerc = makStd,
                WetBreynePerc          = 0.01m,
                StraightloanGebouwPerc = 0.0125m,
                StraightloanGrondPerc  = 0.0125m,
                OnvoorzienPerc         = 0.02m
            };
            _uow.BudgetParams.Add(nieuw);
            await _uow.SaveChangesAsync();
            return nieuw;
        }
    }
}
