using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IDepartmentService
    {
        GetResponse<DepartmentBO> GetDepartments();
        GetResponse<IdNameBO> GetDepartmentsForSelect();
        GetResponse<DepartmentBO> GetDepartmentById(int id);
        GetResponse<DepartmentBO> GetDepartmentByIds(List<int> IdList);
        Response InsertUpdate(DepartmentBO bo);
        Response Delete(List<int> ids);
        Response Delete(List<DepartmentBO> bos);
    }
}
