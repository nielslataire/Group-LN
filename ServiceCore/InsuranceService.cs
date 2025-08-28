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
                .Where(m => m.Type == (int)InsuranceType.ABR
                            && m.ContractActivity.Contract.Project.DeliveryDate == null
                            && m.Enddate == null);

            if (!string.IsNullOrEmpty(userid))
                q = q.Where(m => m.ContractActivity.Contract.Project.AspNetUserId == userid);

            // Binnen 1 maand verlopen
            var warnSoon = q.Where(m =>
                m.Startdate.HasValue &&
                m.Startdate.Value < DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod + 1)) &&
                m.Startdate.Value >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod)));

            foreach (var e in warnSoon)
            {
                response.AddValue(new WarningBO
                {
                    ID = e.Id,
                    ProjectId = e.ContractActivity.Contract.ProjectId,
                    Display = $"De ABR polis van project {e.ContractActivity.Contract.Project.ProjectName} vervalt binnen één maand, gelieve deze te verlengen !",
                    Type = "warning"
                });
            }

            // Reeds vervallen
            var expired = q.Where(m =>
                m.Startdate >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod)));

            foreach (var e in expired)
            {
                response.AddValue(new WarningBO
                {
                    ID = e.Id,
                    ProjectId = e.ContractActivity.Contract.ProjectId,
                    Display = $"De ABR polis van project {e.ContractActivity.Contract.Project.ProjectName} is vervallen, gelieve deze te verlengen !",
                    Type = "danger"
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
                // Maak een stub met enkel de PK, laat de translator daarna de velden invullen.
                entity = new Insurances { Id = bo.Id };          // let op: bo.Id moet != 0
                _uow.Insurances.Attach(entity);

                var err = InsuranceTranslator.TranslateBOToEntity(entity, bo, _uow);
                if (err != ErrorCode.Success)
                {
                    response.AddError(err.ToString());
                    return response;
                }

                // Attach en markeer enkel de velden die je wil updaten
                _uow.Insurances.Attach(entity);
                var entry = _uow.Entry(entity);

                // Markeer gericht als modified (wijzig naar jouw model waar nodig)
                entry.Property("ContractActivityId").IsModified = true;
                entry.Property("InsuranceCompanyId").IsModified = true;
                entry.Property("Startdate").IsModified = true;
                entry.Property("Period").IsModified = true;
                entry.Property("ExtensionPeriod").IsModified = true;
                entry.Property("GuaranteePeriod").IsModified = true;
                entry.Property("Type").IsModified = true;
                entry.Property("Enddate").IsModified = true;

                // Belangrijk: navigaties NIET invullen/markeren (voorkomt principal-gedoe)
                // entry.Reference(x => x.ContractActivity).IsModified = false;
                // entry.Reference(x => x.InsuranceCompany).IsModified = false;

                var resultUpdate = _uow.SaveChanges();
                response.AddSaveChangesResult(resultUpdate, "Verzekering aangepast", "Verzekering niet aangepast");
                return response;
            }
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
