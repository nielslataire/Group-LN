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

namespace ServiceCore
{
    public class AuthenticationService : IAuthenticationService
    {
        public GetResponse<bool> ValidateUser(string userName, string password)
        {
            var response = new GetResponse<bool>();
            response.Value = false;
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetUsersDAO();
            var _entity = dao.GetNoTracking().Where(s => s.UserId == userName).FirstOrDefault();
            if ((_entity != null))
            {
                if ((_entity.Password == _entity.Password))
                    response.Value = true;
            }
            return response;
        }
    }
}
