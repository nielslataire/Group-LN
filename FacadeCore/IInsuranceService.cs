using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IInsuranceService
    {
        GetResponse<InsuranceBO> GetInsurancesByProjectId(int projectid);
        GetResponse<InsuranceBO> GetInsuranceById(int id);
        GetResponse<WarningBO> CheckInsurances(string userid = "");
        Response InsertUpdate(InsuranceBO bo);
        //Response Delete(List<int> ids);
        //Response Delete(List<InsuranceBO> bos);
        GetResponse<InsuranceCompanyBO> GetInsuranceCompanies();
        GetResponse<IdNameBO> GetInsuranceCompaniesForSelect();
    }
}
