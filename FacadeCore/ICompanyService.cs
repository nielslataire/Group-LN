using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface ICompanyService
    {
        GetResponse<CompanyBO> GetCompanyByID(int id);
        GetResponse<IdNameBO> GetCompanyForSelectByActivity(int actid);
        GetResponse<CompanyBO> GetCompanyBySearchfilter(CompanyFilter filter);
        string GetCompanyNameById(int id);
        string GetCompanyNameByContractId(int id);
        GetResponse<SelectBO> GetCompanyForSearchList(string searchterm);
        Response InsertUpdate(CompanyBO company);
        Response Delete(List<int> ids);
        Response Delete(List<CompanyBO> bos);

        // COMPANY ACTIVITIES
        GetResponse<ActivityBO> GetCompanyActivities(int companyid);
        Response AddCompanyActivity(int companyid, int activityid);
        Response DeleteCompanyActivity(int companyid, int activityid);
    }
}
