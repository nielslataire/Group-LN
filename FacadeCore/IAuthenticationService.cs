using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IAuthenticationService
    {
        GetResponse<bool> ValidateUser(string userName, string password);
    }
}
