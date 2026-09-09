using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ServiceCore
{
    public class InsuranceService : IInsuranceService
    {
        private readonly UnitOfWorkCore _uow;

        public InsuranceService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<InsuranceBO> GetInsurancesByProjectId(int projectid)
        {
            var response = new GetResponse<InsuranceBO>();

            // Alle verzekeringen die aan dit project hangen (via ContractActivity -> Contract -> ProjectId)
            var entities = _uow.Insurances
                .GetNoTracking()
                .Where(m => m.ContractActivity.Contract.ProjectId == projectid);

            foreach (var e in entities)
            {
                var bo = new InsuranceBO();
                var err = InsuranceTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }

        public GetResponse<InsuranceBO> GetInsuranceById(int id)
        {
            var response = new GetResponse<InsuranceBO>();

            var entity = _uow.Insurances.GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.ContractActivity)
                .ThenInclude(m => m.Contract)
                .ThenInclude(m => m.Company)
                .Include(m => m.InsuranceCompany)
                .FirstOrDefault();

            if (entity == null)
            {
                response.AddError("insurance not found");
                return response;
            }

            var bo = new InsuranceBO();
            var err = InsuranceTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());

            return response;
        }

        public GetResponse<WarningBO> CheckInsurances(string userid = "")
        {
            var response = new GetResponse<WarningBO>();
            var q = _uow.Insurances.GetNoTracking()
                .Include(m => m.ContractActivity)
                    .ThenInclude(m => m.Contract)
                        .ThenInclude(m => m.Project)
                .Where(m => m.Type == (int)InsuranceType.ABR
                            && m.ContractActivity.Contract.Project.DeliveryDate == null
                            && m.Enddate == null);

            if (!string.IsNullOrEmpty(userid))
                q = q.Where(m => m.ContractActivity.Contract.Project.AspNetUserId == userid);

            // Binnen 1 maand verlopen
            var warnSoon = q.Where(m =>
                m.Startdate.HasValue &&
                m.Startdate.Value < DateOnly.FromDateTime(DateTime.Now.AddMonths(-(m.Period ?? 0) - (m.ExtensionPeriod ?? 0) + 1)) &&
                m.Startdate.Value >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(m.Period ?? 0) - (m.ExtensionPeriod ?? 0))));

            foreach (var e in warnSoon)
            {
                var project = e.ContractActivity?.Contract?.Project;
                if (project == null) continue;
                response.AddValue(new WarningBO
                {
                    ID = e.Id,
                    ProjectId = project.ProjectId,
                    Display = $"De ABR polis van project {project.ProjectName} vervalt binnen één maand, gelieve deze te verlengen !",
                    Type = "warning",
                    Category = "verzekering"
                });
            }

            // Reeds vervallen
            var expired = q.Where(m =>
                m.Startdate.HasValue &&
                m.Startdate.Value >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(m.Period ?? 0) - (m.ExtensionPeriod ?? 0))));

            foreach (var e in expired)
            {
                var project = e.ContractActivity?.Contract?.Project;
                if (project == null) continue;
                response.AddValue(new WarningBO
                {
                    ID = e.Id,
                    ProjectId = project.ProjectId,
                    Display = $"De ABR polis van project {project.ProjectName} is vervallen, gelieve deze te verlengen !",
                    Type = "danger",
                    Category = "verzekering"
                });
            }

            return response;
        }

        public Response InsertUpdate(InsuranceBO bo)
        {
            var response = new Response();

            // --- basisvalidatie: pas aan wat verplicht is ---
            if (bo == null)
            {
                response.AddError("Ongeldig verzoek.");
                return response;
            }
            if (bo.ContractActivityID == 0)
            {
                response.AddError("Contractactiviteit is verplicht.");
                return response;
            }
            // Als InsuranceCompany verplicht is:
            // if (bo.InsuranceCompany?.Id is null or 0) { response.AddError("Verzekeringsmaatschappij is verplicht."); return response; }

            Insurances entity;

            if (bo.Id == 0)
            {
                // ===== CREATE =====
                entity = _uow.Insurances.GetNew();

                // Laat de translator ALLE scalar/FK velden mappen.
                // Belangrijk: de translator mag GEEN navigaties zetten (ContractActivity/InsuranceCompany).
                var err = InsuranceTranslator.TranslateBOToEntity(entity, bo, _uow);
                if (err != ErrorCode.Success)
                {
                    response.AddError(err.ToString());
                    return response;
                }

                _uow.Insurances.Add(entity);

                var resultCreate = _uow.SaveChanges();
                response.AddSaveChangesResult(resultCreate, "Verzekering toegevoegd", "Verzekering niet toegevoegd");
                return response;
            }
            else
            {
                // ===== UPDATE (detached) =====
                if (bo.Id == 0)
                {
                    response.AddError("Ongeldige Id voor update.");
                    return response;
                }

                // 1) Kijk of er al een tracked instance bestaat en detach die
                var local = _uow.Context.Set<Insurances>().Local.FirstOrDefault(x => x.Id == bo.Id);
                if (local != null)
                {
                    _uow.Context.Entry(local).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

                entity = new Insurances { Id = bo.Id };   // permanente PK
                _uow.Insurances.Attach(entity);               // Attach slechts 1 keer

                // 2) Vertaal enkel scalar/FK’s (geen navigations!)
                var err = InsuranceTranslator.TranslateBOToEntity(entity, bo, _uow);
                if (err != ErrorCode.Success)
                {
                    response.AddError(err.ToString());
                    return response;
                }

                // 3) Gerichte updates markeren
                var entry = _uow.Context.Entry(entity);
                entry.Property("ContractActivityId").IsModified = true;
                entry.Property("InsuranceCompanyId").IsModified = true;
                entry.Property("Startdate").IsModified = true;
                entry.Property("Period").IsModified = true;
                entry.Property("ExtensionPeriod").IsModified = true;
                entry.Property("GuaranteePeriod").IsModified = true;
                entry.Property("Type").IsModified = true;
                entry.Property("Enddate").IsModified = true;
                entry.Property("Polisnummer").IsModified = true;

                var resultUpdate = _uow.SaveChanges();
                response.AddSaveChangesResult(resultUpdate, "Verzekering aangepast", "Verzekering niet aangepast");
                return response;
            }
        }

        public Response Delete(int id)
        {
            var response = new Response();
            if (id <= 0) { response.AddError("Ongeldige verzekering."); return response; }

            var entity = _uow.Insurances.GetNormal().FirstOrDefault(m => m.Id == id);
            if (entity == null) { response.AddError("Verzekering niet gevonden."); return response; }

            var contractActivityId = entity.ContractActivityId;

            _uow.Insurances.DeleteObject(entity);
            var saved = _uow.SaveChanges();

            // De 1-op-1 ContractActivity (activiteit "Verzekeringen") heeft geen bestaansreden
            // zonder verzekering — best-effort opruimen zodat er geen weesregels achterblijven.
            if (saved > 0 && contractActivityId > 0)
            {
                try
                {
                    var ca = _uow.ContractActivities.GetById(contractActivityId);
                    if (ca != null)
                    {
                        _uow.ContractActivities.DeleteObject(ca);
                        _uow.SaveChanges();
                    }
                }
                catch { /* mag de verzekering-verwijdering niet blokkeren */ }
            }

            response.AddSaveChangesResult(saved, "Verzekering verwijderd", "Verzekering niet verwijderd");
            return response;
        }

        public GetResponse<InsuranceCompanyBO> GetInsuranceCompanies()
        {
            var response = new GetResponse<InsuranceCompanyBO>();

            var entities = _uow.InsuranceCompanies.GetNoTracking();
            foreach (var e in entities)
            {
                var bo = new InsuranceCompanyBO();
                var err = InsuranceCompanyTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }

        public GetResponse<IdNameBO> GetInsuranceCompaniesForSelect()
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.InsuranceCompanies.GetNoTracking();
            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            response.Values = response.Values.OrderBy(m => m.Display).ToList();
            return response;
        }
    }
}
