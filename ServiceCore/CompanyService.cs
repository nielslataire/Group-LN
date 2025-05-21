using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class CompanyService : ICompanyService 
    {
        public GetResponse<CompanyBO> GetCompanyByID(int id)
        {
            GetResponse<CompanyBO> response = new GetResponse<CompanyBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyInfoDAO();
            var _entity = dao.GetNoTracking()
                .Where(m => m.CompanyId == id)
                .Include(m => m.PostCode)
                .ThenInclude(m => m.Country)
                .SingleOrDefault();
            CompanyBO company = new CompanyBO();

            var err = CompanyTranslator.TranslateEntityToBO(_entity, company);
            if (err == ErrorCode.Success)
                response.Value = company;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<IdNameBO> GetCompanyForSelectByActivity(int actid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyInfoDAO();
            var entities = dao.GetNoTracking().Where(m => m.Activity.Any(i => i.ActivityId == actid));
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            response.Values = response.Values.OrderBy(m => m.Display).ToList();
            return response;
        }

        public GetResponse<CompanyBO> GetCompanyBySearchfilter(CompanyFilter filter)
        {
            GetResponse<CompanyBO> response = new GetResponse<CompanyBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyInfoDAO();
            var entitys = dao.GetNoTracking().Where(CompanyQuery.GetFilterQeury(filter)).OrderBy(m => m.BedrijfsNaam);

            foreach (var _entity in entitys)
            {
                CompanyBO company = new CompanyBO();
                var err = CompanyTranslator.TranslateEntityToBO(_entity, company);
                if (err == ErrorCode.Success)
                    response.AddValue(company);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public string GetCompanyNameById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyInfoDAO();
            var _entity = dao.GetById(id);
            return _entity.BedrijfsNaam;
        }
        public string GetCompanyNameByContractId(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetContractDAO();
            var _entity = dao.GetNoTracking().Where(m => m.Id == id).Include(m => m.Company).SingleOrDefault();
            return _entity.Company.BedrijfsNaam;
        }
        public GetResponse<SelectBO> GetCompanyForSearchList(string searchterm)
        {
            GetResponse<SelectBO> response = new GetResponse<SelectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyInfoDAO();
            var entitys = dao.GetNoTracking().Where(CompanyQuery.GetNameQuery(searchterm)).OrderBy(m => m.BedrijfsNaam).Select(m => new SelectBO() { id = m.CompanyId, text = m.BedrijfsNaam, extra = "Company" });
            response.Values = entitys.ToList();
            // For Each _entity In entitys
            // response.AddValue(_entity.GetIdNameForSearch())
            // Next
            return response;
        }



        public Response InsertUpdate(CompanyBO company)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(company.Bedrijfsnaam)))
                response.AddError("companyname is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            CompanyInfo _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((company.CompanyId == 0))
                _entity = uow.GetCompanyInfoDAO().GetNew();
            else
                _entity = uow.GetCompanyInfoDAO().GetById(company.CompanyId);
            if ((_entity != null))
            {
                var err = CompanyTranslator.TranslateBOToEntity(_entity, company, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("company not found");

            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response Delete(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetCompanyInfoDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response Delete(List<CompanyBO> bos)
        {
            return Delete(bos.Select(s => s.CompanyId).ToList());
        }
        public GetResponse<ActivityBO> GetCompanyActivities(int companyid)
        {
            UnitOfWork uow = new UnitOfWork();
            GetResponse<ActivityBO> response = new GetResponse<ActivityBO>();
            var dao = uow.GetCompanyInfoDAO();
            var _entity = dao.GetNormal()
                .Include(m => m.Activity)
                .FirstOrDefault(m => m.CompanyId == companyid);
            List<ActivityBO> activities = new List<ActivityBO>();
            if (_entity == null)
            {
                response.AddError("company not found");
                return response;
            }   
            foreach (var act in _entity.Activity)
            {
                ActivityBO activity = new ActivityBO();
                var err = ActivityTranslator.TranslateEntityToBO(act, activity);
                if (err == ErrorCode.Success)
                    response.AddValue(activity);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }

        public Response AddCompanyActivity(int companyid, int activityid)
        {
            Response response = new Response();
            var uow = new UnitOfWork();
            var company = uow.GetCompanyInfoDAO().GetById(companyid);
            if ((company == null))
            {
                response.AddError("company not found");
                return response;
            }
            var acitvity = uow.GetActivityDAO().GetById(activityid);
            if ((acitvity == null))
            {
                response.AddError("activity not found");
                return response;
            }
            company.Activity.Add(acitvity);
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response DeleteCompanyActivity(int companyid, int activityid)
        {
            Response response = new Response();
            var uow = new UnitOfWork();
            var company = uow.GetCompanyInfoDAO().GetById(companyid);
            if ((company == null))
            {
                response.AddError("company not found");
                return response;
            }
            var acitvity = uow.GetActivityDAO().GetById(activityid);
            if ((acitvity == null))
            {
                response.AddError("activity not found");
                return response;
            }
            company.Activity.Remove(acitvity);
            response.AddError(uow.SaveChanges());
            return response;
        }
    }
}
