using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using System.Linq;

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

            var entity = _uow.Insurances.GetById(id);
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
                    ID = e.ContractActivityId,
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
                    ID = e.ContractActivityId,
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

            // (Eventuele veldvalidaties hier toevoegen)
            if (!response.Success) return response;

            Insurances entity;

            if (bo.ContractActivityID == 0)
                entity = _uow.Insurances.GetNew();
            else
                entity = _uow.Insurances.GetById(bo.ContractActivityID);

            if (entity == null)
            {
                response.AddError("insurance not found");
                return response;
            }

            var err = InsuranceTranslator.TranslateBOToEntity(entity, bo, _uow); // let op: translator moet UnitOfWorkCore accepteren
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Verzekering aangepast of toegevoegd", "Verzekering niet aangepast of toegevoegd");

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
