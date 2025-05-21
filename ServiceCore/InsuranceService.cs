using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;

namespace ServiceCore
{
    public class InsuranceService : IInsuranceService
    {
        public GetResponse<InsuranceBO> GetInsurancesByProjectId(int projectid)
        {
            // Implementation needed here
            return new GetResponse<InsuranceBO>();
        }
        public GetResponse<InsuranceBO> GetInsuranceById(int id)
        {
            GetResponse<InsuranceBO> response = new GetResponse<InsuranceBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInsuranceDAO();
            var _entity = dao.GetById(id);
            InsuranceBO bo = new InsuranceBO();
            var err = InsuranceTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<WarningBO> CheckInsurances(string userid = "")
        {
            GetResponse<WarningBO> response = new GetResponse<WarningBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInsuranceDAO();
            if (userid == "")
            {
                var entities = dao.GetNoTracking().Where(m => m.Type == (int)InsuranceType.ABR && m.Startdate.Value < DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period-(int)m.ExtensionPeriod + 1)) && m.Startdate >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period-(int)m.ExtensionPeriod)) && m.ContractActivity.Contract.Project.DeliveryDate == null && m.Enddate == null);
                foreach (var _entity in entities)
                {
                    WarningBO bo = new WarningBO();
                    bo.ID = _entity.ContractActivityId;
                    bo.ProjectId = _entity.ContractActivity.Contract.ProjectId;
                    bo.Display = "De ABR polis van project " + _entity.ContractActivity.Contract.Project.ProjectName + " vervalt binnen één maand, gelieve deze te verlengen !";
                    bo.Type = "warning";
                    response.AddValue(bo);
                }
            }
            else
            {
                var entities = dao.GetNoTracking().Where(m => m.Type == (int)InsuranceType.ABR && m.Startdate.Value < DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod + 1)) && m.Startdate >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod)) && m.ContractActivity.Contract.Project.DeliveryDate == null && m.ContractActivity.Contract.Project.AspNetUserId == userid && m.Enddate == null);
                foreach (var _entity in entities)
                {
                    WarningBO bo = new WarningBO();
                    bo.ID = _entity.ContractActivityId;
                    bo.ProjectId = _entity.ContractActivity.Contract.ProjectId;
                    bo.Display = "De ABR polis van project " + _entity.ContractActivity.Contract.Project.ProjectName + " vervalt binnen één maand, gelieve deze te verlengen !";
                    bo.Type = "warning";
                    response.AddValue(bo);
                }
            }
            if (userid == "")
            {
                var entities = dao.GetNoTracking().Where(m => m.Type == (int)InsuranceType.ABR && m.Startdate >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod)) && m.ContractActivity.Contract.Project.DeliveryDate == null && m.Enddate == null);
                foreach (var _entity in entities)
                {
                    WarningBO bo = new WarningBO();
                    bo.ID = _entity.ContractActivityId;
                    bo.ProjectId = _entity.ContractActivity.Contract.ProjectId;
                    bo.Display = "De ABR polis van project " + _entity.ContractActivity.Contract.Project.ProjectName + " is vervallen, gelieve deze te verlengen !";
                    bo.Type = "danger";

                    response.AddValue(bo);
                }
            }
            else
            {
                var entities = dao.GetNoTracking().Where(m => m.Type == (int)InsuranceType.ABR && m.Startdate >= DateOnly.FromDateTime(DateTime.Now.AddMonths(-(int)m.Period - (int)m.ExtensionPeriod)) && m.ContractActivity.Contract.Project.DeliveryDate == null && m.ContractActivity.Contract.Project.AspNetUserId == userid && m.Enddate == null);
                foreach (var _entity in entities)
                {
                    WarningBO bo = new WarningBO();
                    bo.ID = _entity.ContractActivityId;
                    bo.ProjectId = _entity.ContractActivity.Contract.ProjectId;
                    bo.Display = "De ABR polis van project " + _entity.ContractActivity.Contract.Project.ProjectName + " is vervallen, gelieve deze te verlengen !";
                    bo.Type = "danger";
                    response.AddValue(bo);
                }
            }
            return response;
        }
        public Response InsertUpdate(InsuranceBO bo)
        {
            Response response = new Response();

            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInsuranceDAO();
            Insurances _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.ContractActivityID == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.ContractActivityID);
            if ((_entity != null))
            {
                var err = InsuranceTranslator.TranslateBOToEntity(_entity, bo, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("insurance not found");

            response.AddError(uow.SaveChanges());
            return response;
        }
        //public Response Delete(List<int> ids)
        //{
        //}
        //public Response Delete(List<InsuranceBO> bos)
        //{
        //}
        public GetResponse<InsuranceCompanyBO> GetInsuranceCompanies()
        {
            GetResponse<InsuranceCompanyBO> response = new GetResponse<InsuranceCompanyBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInsuranceCompaniesDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                InsuranceCompanyBO bo = new InsuranceCompanyBO();
                var err = InsuranceCompanyTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IdNameBO> GetInsuranceCompaniesForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetInsuranceCompaniesDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            response.Values = response.Values.OrderBy(m => m.Display).ToList();
            return response;
        }
    }
}
