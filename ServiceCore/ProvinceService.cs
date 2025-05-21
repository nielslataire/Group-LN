using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacadeCore;
using BOCore;
using DALCore;

namespace ServiceCore
{
    public class ProvinceService : IProvinceService
    {
        public GetResponse<IdNameBO> GetProvinces()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetProvinceDAO();
            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
    }
}
