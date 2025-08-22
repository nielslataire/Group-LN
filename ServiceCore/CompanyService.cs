using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ServiceCore
{
    public class CompanyService : ICompanyService
    {
        private readonly UnitOfWorkCore _uow;

        public CompanyService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<CompanyBO> GetCompanyByID(int id)
        {
            var response = new GetResponse<CompanyBO>();

            var entity = _uow.CompanyInfo.GetNoTracking()
                .Where(m => m.CompanyId == id)
                .Include(m => m.PostCode).ThenInclude(m => m.Country)
                .SingleOrDefault();

            var bo = new CompanyBO();
            var err = CompanyTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<IdNameBO> GetCompanyForSelectByActivity(int actid)
        {
            var response = new GetResponse<IdNameBO>();

            var entities = _uow.CompanyInfo.GetNoTracking()
                .Where(m => m.Activity.Any(i => i.ActivityId == actid));

            foreach (var e in entities)
                response.AddValue(e.GetIdName());

            response.Values = response.Values.OrderBy(m => m.Display).ToList();
            return response;
        }

        public GetResponse<CompanyBO> GetCompanyBySearchfilter(CompanyFilter filter)
        {
            var response = new GetResponse<CompanyBO>();

            var entities = _uow.CompanyInfo.GetNoTracking()
                .Where(CompanyQuery.GetFilterQeury(filter))
                .OrderBy(m => m.BedrijfsNaam);

            foreach (var e in entities)
            {
                var bo = new CompanyBO();
                var err = CompanyTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public string GetCompanyNameById(int id)
        {
            var e = _uow.CompanyInfo.GetById(id);
            return e?.BedrijfsNaam ?? string.Empty;
        }

        public string GetCompanyNameByContractId(int id)
        {
            var e = _uow.Contracts.GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.Company)
                .SingleOrDefault();

            return e?.Company?.BedrijfsNaam ?? string.Empty;
        }

        public GetResponse<SelectBO> GetCompanyForSearchList(string searchterm)
        {
            var response = new GetResponse<SelectBO>();

            var query = _uow.CompanyInfo.GetNoTracking()
                .Where(CompanyQuery.GetNameQuery(searchterm))
                .OrderBy(m => m.BedrijfsNaam)
                .Select(m => new SelectBO { id = m.CompanyId, text = m.BedrijfsNaam, extra = "Company" });

            response.Values = query.ToList();
            return response;
        }

        public Response InsertUpdate(CompanyBO company)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(company.Bedrijfsnaam))
            {
                response.AddError("companyname is mandatory");
                return response;
            }

            CompanyInfo entity = company.CompanyId == 0
                ? _uow.CompanyInfo.GetNew()
                : _uow.CompanyInfo.GetById(company.CompanyId);

            if (entity == null)
            {
                response.AddError("company not found");
                return response;
            }

            // Let op: pas CompanyTranslator aan zodat hij UnitOfWorkCore accepteert.
            var err = CompanyTranslator.TranslateBOToEntity(entity, company, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Bedrijf aangepast of toegevoegd", "Bedrijf niet aangepast of toegevoegd");
            return response;
        }

        public Response Delete(List<int> ids)
        {
            var response = new Response();

            foreach (var id in ids)
                _uow.CompanyInfo.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response Delete(List<CompanyBO> bos)
            => Delete(bos.Select(s => s.CompanyId).ToList());

        public GetResponse<ActivityBO> GetCompanyActivities(int companyid)
        {
            var response = new GetResponse<ActivityBO>();

            var entity = _uow.CompanyInfo.GetNormal()
                .Include(m => m.Activity)
                .FirstOrDefault(m => m.CompanyId == companyid);

            if (entity == null)
            {
                response.AddError("company not found");
                return response;
            }

            foreach (var act in entity.Activity)
            {
                var bo = new ActivityBO();
                var err = ActivityTranslator.TranslateEntityToBO(act, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response AddCompanyActivity(int companyid, int activityid)
        {
            var response = new Response();

            var company = _uow.CompanyInfo.GetById(companyid);
            if (company == null)
            {
                response.AddError("company not found");
                return response;
            }

            var activity = _uow.Activities.GetById(activityid);
            if (activity == null)
            {
                response.AddError("activity not found");
                return response;
            }

            company.Activity.Add(activity);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Activiteit gekoppeld", "Activiteit niet gekoppeld");
            return response;
        }

        public Response DeleteCompanyActivity(int companyid, int activityid)
        {
            var response = new Response();

            var company = _uow.CompanyInfo.GetById(companyid);
            if (company == null)
            {
                response.AddError("company not found");
                return response;
            }

            var activity = _uow.Activities.GetById(activityid);
            if (activity == null)
            {
                response.AddError("activity not found");
                return response;
            }

            company.Activity.Remove(activity);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Activiteit ontkoppeld", "Activiteit niet ontkoppeld");
            return response;
        }
    }
}
