using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOCore;
using BOCore.Budget;
using DALCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public class BudgetActivityService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly BouwIndexService _bouwIndex;
        private readonly BudgetFormulaService _formulaService;

        public BudgetActivityService(UnitOfWorkCore uow, BouwIndexService bouwIndex, BudgetFormulaService formulaService)
        {
            _uow = uow;
            _bouwIndex = bouwIndex;
            _formulaService = formulaService;
        }

        private static decimal GevelM2(DALCore.Models.BudgetGevelElementen e)
        {
            if (e.Hoogte.HasValue && e.Hoogte.Value > 0)
                return e.Aantal * (e.Breedte ?? 0m) * e.Hoogte.Value;
            if (e.Breedte.HasValue && e.Breedte.Value > 0 && e.Lengte.HasValue && e.Lengte.Value > 0)
                return e.Aantal * e.Breedte.Value * e.Lengte.Value;
            return 0m;
        }

        public async Task<List<BudgetLotGroepBO>> GetLotGroepenAsync(int budgetVersieId)
        {
            var versie = await _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetGegevens)
                .SingleOrDefaultAsync(v => v.Id == budgetVersieId);

            var sStart  = versie?.BudgetGegevens?.SIndexStart  ?? 0m;
            var sHuidig = versie?.BudgetGegevens?.SIndexHuidig ?? 0m;
            var iStart  = versie?.BudgetGegevens?.IIndexStart  ?? 0m;
            var iHuidig = versie?.BudgetGegevens?.IIndexHuidig ?? 0m;
            var gewogenFactor = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            // Oppervlaktes (met UnitGroupType voor eenhedentelling)
            var opps = await _uow.BudgetOppervlaktes.GetNoTracking()
                .Include(o => o.UnitGroupType)
                .Where(o => o.BudgetVersieId == budgetVersieId)
                .ToListAsync();

            var aantalEenheden = opps.Count;
            var totaalGBA = opps.Sum(o => o.BewoonbareOpp);

            // ── Ruwbouw-voorstel berekening ────────────────────────────────────────
            // Gebruik de opgeslagen basisprijzen uit BudgetGegevens (zelfde als de live sidebar),
            // en haal enkel de referentie-indexwaarden op via de formualkoppeling voor de juiste noemer.
            var gegevensBO = new BudgetGegevensBO();
            gegevensBO.IIndexHuidig = versie?.BudgetGegevens?.IIndexHuidig;
            gegevensBO.SIndexHuidig = versie?.BudgetGegevens?.SIndexHuidig;
            var formulaCtx = await _formulaService.BuildContextAsync(budgetVersieId, gegevensBO);
            var formulaResultaten = _formulaService.BerekenAlle(formulaCtx);

            var fIHuidig = versie?.BudgetGegevens?.IIndexHuidig ?? 100m;
            var fSHuidig = versie?.BudgetGegevens?.SIndexHuidig ?? 100m;

            decimal MatFactor(decimal iRef, decimal sRef) =>
                fIHuidig / (iRef > 0m ? iRef : 100m) * 0.4m +
                fSHuidig / (sRef > 0m ? sRef : 100m) * 0.4m + 0.2m;

            var ruwbouwIRef = formulaResultaten.GetValueOrDefault(FormulaSleutels.NacalcRuwbouwBasis)?.MateriaalIRef ?? 100m;
            var ruwbouwSRef = formulaResultaten.GetValueOrDefault(FormulaSleutels.NacalcRuwbouwBasis)?.MateriaalSRef ?? 100m;
            var gevelIRef   = formulaResultaten.GetValueOrDefault(FormulaSleutels.BovenbouwGevelmetselwerk)?.MateriaalIRef ?? 100m;
            var gevelSRef   = formulaResultaten.GetValueOrDefault(FormulaSleutels.BovenbouwGevelmetselwerk)?.MateriaalSRef ?? 100m;
            var terrasIRef  = formulaResultaten.GetValueOrDefault(FormulaSleutels.BovenbouwTerras)?.MateriaalIRef ?? 100m;
            var terrasSRef  = formulaResultaten.GetValueOrDefault(FormulaSleutels.BovenbouwTerras)?.MateriaalSRef ?? 100m;

            // Basisprijzen uit BudgetGegevens — zelfde bron als de live berekening op tabblad Gegevens
            var ruwbouwPrijsGeind = (versie?.BudgetGegevens?.NacalcBasisprijs       ?? 0m) * MatFactor(ruwbouwIRef, ruwbouwSRef);
            var gevelPrijsGeind   = (versie?.BudgetGegevens?.GevelMetselwerkPrijsPerM2 ?? 0m) * MatFactor(gevelIRef,   gevelSRef);
            var terrasPrijsGeind  = (versie?.BudgetGegevens?.TerrasPrijsPerM2        ?? 0m) * MatFactor(terrasIRef,  terrasSRef);

            var totOppRuwbouw = opps.Sum(o =>
                o.BewoonbareOpp + o.GaragesParkingsBovenGr + o.GarBergOndergronds +
                o.BergGelijkvloers + o.DoorritGVL + o.GemeenschappelijkeDelen + o.Zolder * 0.30m);

            var totTerrasPrefab = opps.Sum(o => o.TerrasPrefab);

            var gevelRijen = await _uow.BudgetGevelElementen.GetNoTracking()
                .Where(g => g.BudgetVersieId == budgetVersieId)
                .ToListAsync();
            var totaalGevels = gevelRijen
                .Where(g => g.ElementType == "GevelNieuwbouw" || g.ElementType == "GevelBestaand")
                .Sum(g => GevelM2(g));

            var aantalWoonComm = opps.Count(o =>
                o.UnitGroupType != null && (
                    o.UnitGroupType.Name.Contains("woon", StringComparison.OrdinalIgnoreCase) ||
                    o.UnitGroupType.Name.Contains("commerci", StringComparison.OrdinalIgnoreCase)));
            if (aantalWoonComm == 0) aantalWoonComm = aantalEenheden;

            var ruwbouwVoorstelTotaal = ruwbouwPrijsGeind * totOppRuwbouw
                                      + terrasPrijsGeind  * totTerrasPrefab
                                      + gevelPrijsGeind   * totaalGevels;
            var ruwbouwVoorstelPerEenheid = aantalWoonComm > 0
                ? Math.Round(ruwbouwVoorstelTotaal / aantalWoonComm, 2)
                : 0m;
            // ── Einde ruwbouw-voorstel ─────────────────────────────────────────────

            // Alle activiteiten inclusief lot-groep
            var activities = await _uow.Activities.GetNoTracking()
                .Include(a => a.Group)
                .Where(a => a.Group != null)
                .OrderBy(a => a.Group.Lot)
                .ThenBy(a => a.Omschrijving)
                .ToListAsync();

            // Bestaande lijnen voor deze versie
            var bestaandeLijnen = await _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == budgetVersieId)
                .ToListAsync();

            var lijnenByActivity = bestaandeLijnen.ToDictionary(l => l.ActivityId, l => l);

            // Groepeer per ActivityGroup
            var groepen = activities
                .GroupBy(a => new
                {
                    a.GroupId,
                    a.Group.Name,
                    Lot = a.Group.Lot ?? 0m
                })
                .OrderBy(g => g.Key.Lot)
                .Select(g =>
                {
                    var groep = new BudgetLotGroepBO
                    {
                        LotNummer = g.Key.Lot,
                        LotNaam   = g.Key.Name
                    };

                    foreach (var activity in g)
                    {
                        bool isRuwbouw = g.Key.Name.Contains("ruwbouw", StringComparison.OrdinalIgnoreCase) ||
                                         activity.Omschrijving.Contains("ruwbouw", StringComparison.OrdinalIgnoreCase);

                        var bo = new BudgetActivityLijnBO
                        {
                            BudgetVersieId        = budgetVersieId,
                            ActivityId            = activity.ActivityId,
                            ActivityOmschrijving  = activity.Omschrijving,
                            LotNummer             = g.Key.Lot,
                            LotNaam               = g.Key.Name,
                            GroupId               = activity.GroupId,
                            AantalEenheden        = aantalEenheden,
                            TotaalOppervlakte     = totaalGBA,
                            Correctiefactor       = 1m,
                            SIndexStart           = sStart,
                            SIndexHuidig          = sHuidig,
                            IIndexStart           = iStart,
                            IIndexHuidig          = iHuidig,
                            GewogenIndexFactor    = gewogenFactor
                        };

                        bool heeftBestaandeLijn = lijnenByActivity.TryGetValue(activity.ActivityId, out var lijn);
                        if (heeftBestaandeLijn)
                        {
                            bo.Id                          = lijn.Id;
                            bo.AlternatievePrijsPerEenheid = lijn.AlternatievePrijsPerEenheid ?? 0m;
                            bo.NacalcPrijsPerEenheid       = lijn.NacalcPrijsPerEenheid       ?? 0m;
                            bo.Correctiefactor             = lijn.Correctiefactor;
                            bo.IsManueel                   = lijn.IsManueel;
                        }

                        if (isRuwbouw && ruwbouwVoorstelPerEenheid > 0)
                        {
                            bo.VoorgesteldePrijsPerEenheid  = ruwbouwVoorstelPerEenheid;
                            bo.VoorstelRuwbouwPrijs         = ruwbouwPrijsGeind;
                            bo.VoorstelRuwbouwOpp           = totOppRuwbouw;
                            bo.VoorstelTerrasPrijs          = terrasPrijsGeind;
                            bo.VoorstelTerrasOpp            = totTerrasPrefab;
                            bo.VoorstelGevelPrijs           = gevelPrijsGeind;
                            bo.VoorstelGevelOpp             = totaalGevels;
                            bo.VoorstelAantalEenheden       = aantalWoonComm;
                            if (!heeftBestaandeLijn)
                                bo.AlternatievePrijsPerEenheid = ruwbouwVoorstelPerEenheid;
                        }

                        groep.Lijnen.Add(bo);
                    }

                    return groep;
                })
                .ToList();

            return groepen;
        }

        public async Task<Response> SaveLijnenAsync(int budgetVersieId, List<BudgetActivityLijnBO> lijnen)
        {
            var response = new Response();

            var bestaandeLijnen = await _uow.BudgetActivityLijnen.GetNormal()
                .Where(l => l.BudgetVersieId == budgetVersieId)
                .ToListAsync();

            var bestaandeByActivity = bestaandeLijnen.ToDictionary(l => l.ActivityId, l => l);

            foreach (var bo in lijnen)
            {
                if (bestaandeByActivity.TryGetValue(bo.ActivityId, out var bestaande))
                {
                    bestaande.AlternatievePrijsPerEenheid = bo.AlternatievePrijsPerEenheid;
                    bestaande.NacalcPrijsPerEenheid       = bo.NacalcPrijsPerEenheid;
                    bestaande.Correctiefactor             = bo.Correctiefactor;
                    bestaande.IsManueel                   = bo.IsManueel;
                }
                else
                {
                    _uow.BudgetActivityLijnen.Add(new BudgetActivityLijnen
                    {
                        BudgetVersieId             = budgetVersieId,
                        ActivityId                 = bo.ActivityId,
                        AlternatievePrijsPerEenheid = bo.AlternatievePrijsPerEenheid,
                        NacalcPrijsPerEenheid       = bo.NacalcPrijsPerEenheid,
                        Correctiefactor             = bo.Correctiefactor,
                        IsManueel                  = bo.IsManueel,
                        VerhogingsPerc             = 0m
                    });
                }
            }

            int affected = await _uow.SaveChangesAsync();
            response.AddSaveChangesResult(affected,
                "Activiteitenlijnen opgeslagen.",
                "Geen wijzigingen opgeslagen.");
            return response;
        }

        public async Task<IEnumerable<SelectListItem>> GetProjectenVoorNacalcAsync()
        {
            // Geeft alle unieke projecten die budget masters hebben
            var projecten = await _uow.BudgetMasters.GetNoTracking()
                .Include(m => m.Project)
                .Where(m => m.Project != null && !m.IsGearchiveerd)
                .Select(m => new { m.ProjectId, m.Project })
                .Distinct()
                .OrderBy(x => x.ProjectId)
                .ToListAsync();

            return projecten
                .DistinctBy(x => x.ProjectId)
                .Select(x => new SelectListItem
                {
                    Value = x.ProjectId.ToString(),
                    Text  = x.Project.ProjectName ?? $"Project {x.ProjectId}"
                })
                .ToList();
        }
    }
}
