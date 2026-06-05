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

        public BudgetActivityService(UnitOfWorkCore uow, BouwIndexService bouwIndex)
        {
            _uow = uow;
            _bouwIndex = bouwIndex;
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

            // Eenheden en GBA-oppervlakte
            var aantalEenheden = await _uow.BudgetOppervlaktes.GetNoTracking()
                .CountAsync(o => o.BudgetVersieId == budgetVersieId);

            var totaalGBA = await _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == budgetVersieId)
                .SumAsync(o => (decimal?)o.BewoonbareOpp) ?? 0m;

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

                        if (lijnenByActivity.TryGetValue(activity.ActivityId, out var lijn))
                        {
                            bo.Id                          = lijn.Id;
                            bo.AlternatievePrijsPerEenheid = lijn.AlternatievePrijsPerEenheid ?? 0m;
                            bo.NacalcPrijsPerEenheid       = lijn.NacalcPrijsPerEenheid       ?? 0m;
                            bo.Correctiefactor             = lijn.Correctiefactor;
                            bo.IsManueel                   = lijn.IsManueel;
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
