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
                entry.Property(e => e.Fase).IsModified = true;
                entry.Property(e => e.Warnings).IsModified = true;
                entry.Property(e => e.BerekendOp).IsModified = true;

                _uow.SaveChanges();
                bo.Id = existing.Id;
            }

            response.AddValue(bo);
            return response;
        }

        public void CalculateAllProjects()
        {
            // Alle projecten die niet opgeleverd zijn (StatusId != 1)
            var projectIds = _uow.Projects.GetNoTracking()
                .Where(p => p.StatusId != (int)ProjectStatusType.Opgeleverd)
                .Select(p => p.ProjectId)
                .ToList();

            foreach (var id in projectIds)
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
                .ToList();

            var gecontracterrdPerGroep = contractActivities
                .GroupBy(ca => ca.Activity.GroupId)
                .ToDictionary(g => g.Key, g => g.Sum(ca => ca.Price ?? 0m));

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
            decimal fysiekeVoortgang = totalBegroot > 0m
                ? Math.Round(gewogenBijdrage / totalBegroot * 100m, 2)
                : 0m;

            // 5. Financiële voortgang: gefactureerd / gecontracteerd
            decimal financieleVoortgang = totalGecontracteerd > 0m
                ? Math.Round(totalGefactureerd / totalGecontracteerd * 100m, 2)
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
                Fase                        = fase,
                Warnings                    = warnings.Count > 0 ? string.Join("|", warnings) : null,
                BerekendOp                  = DateTime.UtcNow,
                ManueelAfgesloten           = false,
            };
        }

        // ─── Hulpmethoden ─────────────────────────────────────────────────

        private static ProjectVoortgangBO MapToBO(ProjectVoortgang e) => new()
        {
            Id                          = e.Id,
            ProjectId                   = e.ProjectId,
            FysiekeVoortgangPct         = e.FysiekeVoortgangPct,
            FinancieleVoortgangPct      = e.FinancieleVoortgangPct,
            ContractueleVolwassenheidPct = e.ContractueleVolwassenheidPct,
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
            entity.Fase                         = (int)bo.Fase;
            entity.Warnings                     = bo.Warnings;
            entity.BerekendOp                   = bo.BerekendOp;
            entity.ManueelAfgesloten            = bo.ManueelAfgesloten;
        }
    }
}
