using BOCore;

namespace FacadeCore
{
    public interface IUserSignatureService
    {
        GetResponse<UserEmailSignatureBO> GetByUserId(int userId);
        Response Save(int userId, string signatureHtml, string signatureFormat);
    }
}
