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
            void AddPerc(string omschrijving, decimal? perc)
            {
                decimal bedrag = totaalBouw * (perc ?? 0m);
                if (perc.HasValue && perc.Value != 0m)
                    result.KostenOpBouw.Add(new BudgetKostenPostBO
                    {
                        Omschrijving = omschrijving,
                        Bedrag       = bedrag,
                        Percentage   = perc.Value,
                        IsPerc       = true,
                        BasisBedrag  = totaalBouw
                    });
            }
            AddPerc("Projectcoördinatie", p.ProjectcoordinatiePerc);
            AddPerc("Architect",          p.ArchitectPerc);
            AddPerc("Veiligheidscoörd. + EPB", p.VeiligheidscoordEPBPerc);
            AddPerc("Ingenieur",          p.StudieIRPerc);
            AddPerc("Decennale gesloten ruwbouw", p.DecennaleGeslRuwbouwPerc);
            AddPerc("ABR + plaatsbeschrijving",   p.ABRPlaatsbeschrPerc);

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
            AddForfait("Vent. verslaggever",    p.VentVerslaggeverForfait);
            AddForfait("Opmeting + sondering",  p.OpmetingSonderingForfait);
            AddForfait("Infrastructuur",         p.InfrastructuurForfait);
            AddForfait("Publiciteit",            p.PubliciteitForfait);

            int aantalLiften = versie?.BudgetGegevens?.AantalLiften ?? 0;
            if (aantalLiften > 0 && p.LiftPrijsPerStuk.HasValue && p.LiftPrijsPerStuk.Value != 0m)
                result.Forfaits.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Liften ({aantalLiften} st.)",
                    Bedrag       = aantalLiften * p.LiftPrijsPerStuk.Value,
                    IsPerc       = false
                });

            // D) Financiering
            decimal wetBreyne = totaalBouw * p.WetBreynePerc * ((p.WetBreyneMaanden ?? 0) / 12m);
            if (wetBreyne != 0m)
                result.Financiering.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Wet Breyne ({p.WetBreyneMaanden ?? 0} mnd)",
                    Bedrag       = wetBreyne,
                    Percentage   = p.WetBreynePerc,
                    IsPerc       = true,
                    BasisBedrag  = totaalBouw
                });

            decimal slGebouw = totaalBouw * p.StraightloanGebouwPerc * ((p.StraightloanGebouwMaanden ?? 0) / 12m);
            if (slGebouw != 0m)
                result.Financiering.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Straight loan gebouw ({p.StraightloanGebouwMaanden ?? 0} mnd)",
                    Bedrag       = slGebouw,
                    Percentage   = p.StraightloanGebouwPerc,
                    IsPerc       = true,
                    BasisBedrag  = totaalBouw
                });

            decimal grond = p.AankoopprijsGrond ?? 0m;
            decimal slGrond = grond * p.StraightloanGrondPerc * ((p.StraightloanGrondMaanden ?? 0) / 12m);
            if (slGrond != 0m)
                result.Financiering.Add(new BudgetKostenPostBO
                {
                    Omschrijving = $"Straight loan grond ({p.StraightloanGrondMaanden ?? 0} mnd)",
                    Bedrag       = slGrond,
                    Percentage   = p.StraightloanGrondPerc,
                    IsPerc       = true,
                    BasisBedrag  = grond
                });

            // E) Onvoorzien
            result.Onvoorzien = totaalBouw * p.OnvoorzienPerc;

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

        public async Task<BudgetParams> GetOrCreateParamsAsync(int budgetVersieId)
        {
            // Standaard-erelonen komen uit Instellingen > Bouwkost % (vaste systeemrijen).
            // Daar staan ze in procentpunten (5,25) — BudgetParams bewaart een fractie.
            var std = await _uow.BouwkostPercentages.GetNoTracking()
                .Where(p => p.Sleutel != null)
                .ToDictionaryAsync(p => p.Sleutel, p => p.Percentage / 100m);

            decimal StdFractie(string sleutel) =>
                std.TryGetValue(sleutel, out var v) ? v : 0m;

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

                if (bestaand.ArchitectPerc == null && archStd != 0m) bestaand.ArchitectPerc = archStd;
                if (bestaand.StudieIRPerc  == null && ingStd  != 0m) bestaand.StudieIRPerc  = ingStd;
                if ((bestaand.ProjectcoordinatiePerc == 0m || bestaand.ProjectcoordinatiePerc == 0.0525m)
                    && pcStd != 0m)
                    bestaand.ProjectcoordinatiePerc = pcStd;

                return bestaand;
            }

            var nieuw = new BudgetParams
            {
                BudgetVersieId         = budgetVersieId,
                ProjectcoordinatiePerc = StdFractie("projectcoordinatie") is var pc && pc != 0m ? pc : 0.0525m,
                ArchitectPerc          = StdFractie("architect") is var a && a != 0m ? a : (decimal?)null,
                StudieIRPerc           = StdFractie("ingenieur")  is var i && i != 0m ? i : (decimal?)null,
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
