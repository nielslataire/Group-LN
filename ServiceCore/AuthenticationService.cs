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

            var user = _uow.Users.GetNoTracking()
                .SingleOrDefault(u => u.UserId == userName);

            if (user == null)
            {
                response.AddError("user not found");
                return response;
            }

            bool isOk;
            if (user.Password != null && user.Password.StartsWith("$2", StringComparison.Ordinal))
            {
                // BCrypt hash — nieuw formaat
                isOk = BCrypt.Net.BCrypt.Verify(password, user.Password);
            }
            else
            {
                // Transitie: bestaande plaintext-wachtwoorden worden nog aanvaard.
                // Migreer via een SQL-script of door wachtwoorden opnieuw in te stellen.
                isOk = user.Password == password;
            }

            if (!isOk)
                response.AddError("invalid credentials");

            response.Value = isOk;
            return response;
        }
    }
}
