using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore
{
    public class ProjectVoortgangService : IProjectVoortgangService
    {
        private readonly UnitOfWorkCore _uow;

        // Type-waarde voor Meerwerk_Klant in IncommingInvoiceDetail
        private const decimal MEERWERK_KLANT = 3m;
        // Maximale facturatie-ratio per activiteitengroep (capping fysieke voortgang)
        private const decimal CAPPING = 0.95m;

        public ProjectVoortgangService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public Dictionary<int, ProjectVoortgangBO> GetForProjects(IEnumerable<int> projectIds)
        {
            var ids = projectIds.ToList();
            return _uow.ProjectVoortgangs.GetNoTracking()
                .Where(v => ids.Contains(v.ProjectId))
                .AsEnumerable()
                .ToDictionary(v => v.ProjectId, MapToBO);
        }

        public GetResponse<ProjectVoortgangBO> GetByProjectId(int projectId)
        {
            var response = new GetResponse<ProjectVoortgangBO>();

            var entity = _uow.ProjectVoortgangs.GetNoTracking()
                .FirstOrDefault(v => v.ProjectId == projectId);

            if (entity == null)
            {
                response.AddError("Geen voortgang gevonden voor dit project.");
                return response;
            }

            response.AddValue(MapToBO(entity));
            return response;
        }

        public GetResponse<ProjectVoortgangBO> Calculate(int projectId)
        {
            var response = new GetResponse<ProjectVoortgangBO>();
            var bo = CalculateInternal(projectId);
            response.AddValue(bo);
            return response;
        }

        public GetResponse<ProjectVoortgangBO> CalculateAndSave(int projectId)
        {
            var response = new GetResponse<ProjectVoortgangBO>();
            var bo = CalculateInternal(projectId);

            // Upsert: zoek bestaande rij of maak nieuwe aan
            var existing = _uow.ProjectVoortgangs.GetNoTracking()
                .FirstOrDefault(v => v.ProjectId == projectId);

            if (existing == null)
            {
                var entity = _uow.ProjectVoortgangs.GetNew();
                MapToEntity(entity, bo);
                _uow.ProjectVoortgangs.Add(entity);
                _uow.SaveChanges();
                bo.Id = entity.Id;
            }
            else
            {
                // Detach eventuele tracked versie
                var local = _uow.Context.Set<ProjectVoortgang>().Local
                    .FirstOrDefault(x => x.ProjectId == projectId);
                if (local != null)
                    _uow.Context.Entry(local).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                var entity = new ProjectVoortgang { Id = existing.Id };
                _uow.ProjectVoortgangs.Attach(entity);
                MapToEntity(entity, bo);
                entity.Id = existing.Id;

                var entry = _uow.Context.Entry(entity);
                entry.Property(e => e.FysiekeVoortgangPct).IsModified = true;
                entry.Property(e => e.FinancieleVoortgangPct).IsModified = true;
                entry.Property(e => e.ContractueleVolwassenheidPct).IsModified = true;
                entry.Property(e => e.TotaalBegroot).IsModified = true;
                entry.Property(e => e.TotaalGecontracteerd).IsModified = true;
                entry.Property(e => e.TotaalGefactureerd).IsModified = true;
                entry.Property(e => e.Fase).IsModified = true;
                entry.Property(e => e.Warnings).IsModified = true;
                entry.Property(e => e.BerekendOp).IsModified = true;

                _uow.SaveChanges();
                bo.Id = existing.Id;
            }

            response.AddValue(bo);
            return response;
        }

        public List<int> GetActiveProjectIds()
        {
            return _uow.Projects.GetNoTracking()
                .Where(p => p.StatusId != (int)ProjectStatusType.Opgeleverd)
                .Select(p => p.ProjectId)
                .ToList();
        }

        public void CalculateAllProjects()
        {
            foreach (var id in GetActiveProjectIds())
            {
                try
                {
                    CalculateAndSave(id);
                }
                catch
                {
                    // Eén project mag de rest niet blokkeren
                }
            }
        }

        // ─── Kern berekening ──────────────────────────────────────────────

        private ProjectVoortgangBO CalculateInternal(int projectId)
        {
            // 1. Begroot per activiteitengroep
            var budgets = _uow.Budgets.GetNoTracking()
                .Where(b => b.ProjectId == projectId)
                .Include(b => b.Activity)
                .ToList();

            var budgetPerGroep = budgets
                .GroupBy(b => b.Activity.GroupId)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Price));

            decimal totalBegroot = budgetPerGroep.Values.Sum();

            // 2. Gefactureerd per activiteitengroep (excl. Meerwerk_Klant)
            var factuurDetails = _uow.IncommingInvoiceDetails.GetNoTracking()
                .Where(d => d.IncommingInvoice.ProjectId == projectId && d.Type != MEERWERK_KLANT)
                .Include(d => d.ContractAct).ThenInclude(ca => ca.Activity)
                .Include(d => d.Act)
                .ToList();

            var factuurPerGroep = factuurDetails
                .GroupBy(d =>
                    d.ContractAct?.Activity?.GroupId
                    ?? d.Act?.GroupId
                    ?? 0)
                .Where(g => g.Key != 0)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Price ?? 0m));

            // 3. Gecontracteerd per activiteitengroep
            var contractActivities = _uow.ContractActivities.GetNoTracking()
                .Where(ca => ca.Contract.ProjectId == projectId)
                .Include(ca => ca.Activity)
                .Include(ca => ca.ContractAdditionalOrder)
                .ToList();

            // Gecontracteerd = basisprijs van het lot + de bijbestellingen erop.
            var gecontracterrdPerGroep = contractActivities
                .GroupBy(ca => ca.Activity.GroupId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(ca => (ca.Price ?? 0m) + ca.ContractAdditionalOrder.Sum(o => o.Price)));

            decimal totalGecontracteerd = gecontracterrdPerGroep.Values.Sum();
            decimal totalGefactureerd = factuurPerGroep.Values.Sum();

            // 3b. Taak-gebaseerde voortgang per activiteitengroep (overschrijft capping voor die groep)
            var planningSecties = _uow.PlanningSections.GetNoTracking()
                .Where(s => s.ProjectId == projectId && s.IsActief && s.ActivityGroupId != null)
                .Include(s => s.PlanningTaak)
                .ToList();

            // Gemiddelde taakvoortgang per gekoppelde activiteitengroep
            var taakVoortgangPerGroep = planningSecties
                .Where(s => s.PlanningTaak.Count > 0)
                .GroupBy(s => s.ActivityGroupId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(s => s.PlanningTaak).Average(t => (decimal)t.VoortgangPct) / 100m);

            // 4. Fysieke voortgang: gewogen, afgetopt per groep op CAPPING
            //    Als taken bestaan voor een groep: gebruik taakvoortgang i.p.v. capping
            decimal gewogenBijdrage = 0m;
            foreach (var (groepId, begroot) in budgetPerGroep)
            {
                if (begroot <= 0m) continue;
                decimal ratio;
                if (taakVoortgangPerGroep.TryGetValue(groepId, out var taakRatio))
                {
                    ratio = taakRatio;     // taak-gebaseerd: geen capping nodig
                }
                else
                {
                    var gefactureerd = factuurPerGroep.TryGetValue(groepId, out var gf) ? gf : 0m;
                    ratio = Math.Min(gefactureerd / begroot, CAPPING);
                }
                gewogenBijdrage += ratio * begroot;
            }
            decimal fysiekeVoortgangBudgetBased = totalBegroot > 0m
                ? Math.Round(gewogenBijdrage / totalBegroot * 100m, 2)
                : 0m;

            // 4b. Fysieke voortgang o.b.v. reeds gefactureerde betaalschijven aan
            //     kopers — voor eenheden met een akte in het verleden volgt de
            //     klantfacturatie schijven die zelf al aan werf-voortgang gekoppeld
            //     zijn, een directer signaal dan de budget/onderaannemersfactuur-
            //     verhouding hierboven. Enkel gebruikt wanneer er effectief
            //     verkochte eenheden met toepasbare schijven bestaan; anders blijft
            //     de budget-gebaseerde berekening hierboven gewoon gelden (het "oude
            //     systeem" als backup, zoals gevraagd).
            decimal fysiekeVoortgang = CalculateVerkoopVoortgang(projectId) ?? fysiekeVoortgangBudgetBased;

            // 5. Financiële voortgang: gefactureerd t.o.v. begroot — maar per lot
            //    (activiteitengroep) opgetrokken naar het gecontracteerde bedrag
            //    zodra dat hoger ligt dan de begroting voor dat lot. Zo weegt een
            //    lot dat duurder is aangenomen dan begroot mee aan zijn werkelijke
            //    (gecontracteerde) waarde i.p.v. aan de te lage begroting.
            var financieleReferentiePerGroep = budgetPerGroep.Keys
                .Union(gecontracterrdPerGroep.Keys)
                .Union(factuurPerGroep.Keys)
                .Select(groepId =>
                {
                    var begrootGroep = budgetPerGroep.TryGetValue(groepId, out var bg) ? bg : 0m;
                    var gecontracteerdGroep = gecontracterrdPerGroep.TryGetValue(groepId, out var gc) ? gc : 0m;
                    return Math.Max(begrootGroep, gecontracteerdGroep);
                })
                .Sum();

            decimal financieleVoortgang = financieleReferentiePerGroep > 0m
                ? Math.Round(totalGefactureerd / financieleReferentiePerGroep * 100m, 2)
                : 0m;

            // 6. Contractuele volwassenheid: gecontracteerd / begroot
            decimal contractueleVolwassenheid = totalBegroot > 0m
                ? Math.Round(totalGecontracteerd / totalBegroot * 100m, 2)
                : 0m;

            // 7. Fase bepalen: opleverdatum in verleden → Afgewerkt (hoogste prioriteit)
            var deliveryDate = _uow.Projects.GetNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.DeliveryDate)
                .FirstOrDefault();

            VoortgangFase fase;
            if (deliveryDate.HasValue && deliveryDate.Value < DateOnly.FromDateTime(DateTime.Today))
            {
                fase = VoortgangFase.Afgewerkt;
            }
            else
            {
                fase = fysiekeVoortgang switch
                {
                    <= 5m  => VoortgangFase.Opstart,
                    <= 25m => VoortgangFase.InVoorbereiding,
                    <= 80m => VoortgangFase.InUitvoering,
                    <= 95m => VoortgangFase.Eindfase,
                    _      => VoortgangFase.Afgewerkt,
                };
            }

            // 8. Warnings
            var warnings = new List<string>();

            // Overfacturatie: financieel loopt meer dan 15% voor op fysiek
            if (financieleVoortgang > fysiekeVoortgang + 15m)
                warnings.Add("OVERFACTURATIE");

            // Kostenoverschrijding: gefactureerd > 110% van gecontracteerd
            if (totalGecontracteerd > 0m && totalGefactureerd > totalGecontracteerd * 1.10m)
                warnings.Add("KOSTENOVERSCHRIJDING");

            // Grote afwijking: verschil fysiek/financieel > 25%
            if (Math.Abs(fysiekeVoortgang - financieleVoortgang) > 25m
                && !warnings.Contains("OVERFACTURATIE"))
                warnings.Add("GROTE_AFWIJKING");

            // Onderfacturatie: fysiek loopt meer dan 20% voor op financieel
            if (fysiekeVoortgang > financieleVoortgang + 20m)
                warnings.Add("ONDERFACTURATIE");

            return new ProjectVoortgangBO
            {
                ProjectId                   = projectId,
                FysiekeVoortgangPct         = fysiekeVoortgang,
                FinancieleVoortgangPct      = financieleVoortgang,
                ContractueleVolwassenheidPct = contractueleVolwassenheid,
                TotaalBegroot               = totalBegroot,
                TotaalGecontracteerd        = totalGecontracteerd,
                TotaalGefactureerd          = totalGefactureerd,
                Fase                        = fase,
                Warnings                    = warnings.Count > 0 ? string.Join("|", warnings) : null,
                BerekendOp                  = DateTime.UtcNow,
                ManueelAfgesloten           = false,
            };
        }

        // ─── Verkoop-gebaseerde fysieke voortgang ───────────────────────────

        /// <summary>Fysieke voortgang o.b.v. de betaalschijven die al aan kopers
        /// gefactureerd zijn — enkel voor eenheden met een akte in het verleden.
        /// Payment-groep-resolutie (eigen groep + groep van elke gekozen
        /// afwerkingsoptie) volgt exact hetzelfde patroon als
        /// ProjectService.GetProjectInvoicableUnits. Retourneert null wanneer er
        /// geen enkele verkochte eenheid met toepasbare, te factureren schijven
        /// bestaat — de aanroeper valt dan terug op de budget-gebaseerde
        /// berekening.</summary>
        private decimal? CalculateVerkoopVoortgang(int projectId)
        {
            var cutoffDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
            var units = _uow.Units.GetNoTracking()
                .Where(u => u.ProjectId == projectId
                            && u.ClientAccountId != null
                            && u.ClientAccount.DateDeedOfSale != null
                            && u.ClientAccount.DateDeedOfSale.Value <= cutoffDate)
                .Select(u => new { u.Id, u.PaymentGroupId })
                .ToList();

            if (units.Count == 0) return null;

            var unitIds = units.Select(u => u.Id).ToList();

            // Payment-groepen per eenheid: eigen groep + groep van elke gekozen
            // afwerkingsoptie.
            var groupMap = units.ToDictionary(u => u.Id, u => new HashSet<int>());
            foreach (var u in units)
                if (u.PaymentGroupId.HasValue)
                    groupMap[u.Id].Add(u.PaymentGroupId.Value);

            var constructionValues = _uow.UnitConstructionValues.GetNoTracking()
                .Where(cv => unitIds.Contains(cv.UnitId))
                .Select(cv => new { cv.UnitId, cv.PaymentGroupId, cv.Value, cv.ValueSold })
                .ToList();

            foreach (var cv in constructionValues.Where(cv => cv.PaymentGroupId.HasValue))
                groupMap[cv.UnitId].Add(cv.PaymentGroupId!.Value);

            var groupIds = groupMap.Values.SelectMany(g => g).Distinct().ToList();
            if (groupIds.Count == 0) return null;

            // ALLE schijven van het betaalschema van de groep (samen ~100% van de
            // bouw) — NIET enkel de al als "factureerbaar" gemarkeerde. Dat vinkje
            // wordt gaandeweg gezet: op dit moment zijn bv. enkel de eerste 3
            // milestones (40%) factureerbaar. Meten t.o.v. alleen die 40% maakt
            // "alle nu factureerbare schijven gefactureerd" ten onrechte 100%
            // fysieke voortgang terwijl de werf pas ~40% ver staat.
            var stages = _uow.PaymentStages.GetNoTracking()
                .Where(s => groupIds.Contains(s.GroupId))
                .Select(s => new { s.Id, s.GroupId, s.Percentage })
                .ToList();

            if (stages.Count == 0) return null;

            var stageIds = stages.Select(s => s.Id).ToList();

            // Enkel echt verzonden facturen tellen mee — een geannuleerde
            // factuurlijn mag een schijf niet als "gefactureerd" laten gelden.
            var invoicedPairs = _uow.InvoiceDetails.GetNoTracking()
                .Where(d => d.PaymentStageId != null && stageIds.Contains(d.PaymentStageId.Value)
                            && d.UnitId != null && unitIds.Contains(d.UnitId.Value)
                            && d.Invoice.CancelledAt == null)
                .Select(d => new { StageId = d.PaymentStageId!.Value, UnitId = d.UnitId!.Value })
                .ToList()
                .ToHashSet();

            decimal totalWeightedProgress = 0m;
            decimal totalWeight = 0m;

            foreach (var u in units)
            {
                if (!groupMap.TryGetValue(u.Id, out var unitGroupIds) || unitGroupIds.Count == 0)
                    continue;

                var unitStages = stages.Where(s => unitGroupIds.Contains(s.GroupId)).ToList();
                var totalPct = unitStages.Sum(s => s.Percentage);
                if (totalPct <= 0m) continue;

                var invoicedPct = unitStages
                    .Where(s => invoicedPairs.Contains(new { StageId = s.Id, UnitId = u.Id }))
                    .Sum(s => s.Percentage);

                // Afgetopt op 98% — de laatste 2% hangt af van opleveringspunten
                // die hier nog niet gemodelleerd zijn.
                var unitProgress = Math.Min(98m, invoicedPct / totalPct * 100m);

                // Gewicht = verkochte bouwwaarde (grondwaarde telt niet mee, die
                // zegt niets over werf-voortgang): basisconstructie + gekozen
                // afwerkingsopties, ValueSold met Value als fallback.
                var weight = constructionValues
                    .Where(cv => cv.UnitId == u.Id)
                    .Sum(cv => cv.ValueSold ?? cv.Value ?? 0m);
                if (weight <= 0m) continue;

                totalWeightedProgress += unitProgress * weight;
                totalWeight += weight;
            }

            return totalWeight > 0m
                ? Math.Round(totalWeightedProgress / totalWeight, 2)
                : null;
        }

        // ─── Hulpmethoden ─────────────────────────────────────────────────

        private static ProjectVoortgangBO MapToBO(ProjectVoortgang e) => new()
        {
            Id                          = e.Id,
            ProjectId                   = e.ProjectId,
            FysiekeVoortgangPct         = e.FysiekeVoortgangPct,
            FinancieleVoortgangPct      = e.FinancieleVoortgangPct,
            ContractueleVolwassenheidPct = e.ContractueleVolwassenheidPct,
            TotaalBegroot               = e.TotaalBegroot ?? 0m,
            TotaalGecontracteerd        = e.TotaalGecontracteerd ?? 0m,
            TotaalGefactureerd          = e.TotaalGefactureerd ?? 0m,
            Fase                        = (VoortgangFase)e.Fase,
            Warnings                    = e.Warnings,
            BerekendOp                  = e.BerekendOp,
            ManueelAfgesloten           = e.ManueelAfgesloten,
        };

        private static void MapToEntity(ProjectVoortgang entity, ProjectVoortgangBO bo)
        {
            entity.ProjectId                    = bo.ProjectId;
            entity.FysiekeVoortgangPct          = bo.FysiekeVoortgangPct;
            entity.FinancieleVoortgangPct       = bo.FinancieleVoortgangPct;
            entity.ContractueleVolwassenheidPct = bo.ContractueleVolwassenheidPct;
            entity.TotaalBegroot                = bo.TotaalBegroot;
            entity.TotaalGecontracteerd         = bo.TotaalGecontracteerd;
            entity.TotaalGefactureerd           = bo.TotaalGefactureerd;
            entity.Fase                         = (int)bo.Fase;
            entity.Warnings                     = bo.Warnings;
            entity.BerekendOp                   = bo.BerekendOp;
            entity.ManueelAfgesloten            = bo.ManueelAfgesloten;
        }
    }
}
