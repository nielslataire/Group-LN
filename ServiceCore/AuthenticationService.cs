using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using System.Linq;

namespace ServiceCore
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UnitOfWorkCore _uow;

        public AuthenticationService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<bool> ValidateUser(string userName, string password)
        {
            var response = new GetResponse<bool> { Value = false };

            // Haal gebruiker op (no tracking)
            var user = _uow.Users.GetNoTracking()
                .SingleOrDefault(u => u.UserId == userName);

            if (user == null)
            {
                response.AddError("user not found");
                return response;
            }

            // TODO: vervang dit door een hash-verify (bv. BCrypt/ASP.NET Core Identity)
            var isOk = user.Password == password;

            if (!isOk)
            {
                response.AddError("invalid credentials");
            }

            response.Value = isOk;
            return response;
        }
    }
}
