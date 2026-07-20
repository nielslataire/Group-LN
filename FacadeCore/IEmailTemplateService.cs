using BOCore;

namespace FacadeCore
{
    public interface IEmailTemplateService
    {
        GetResponse<EmailTemplateBO> GetAll(bool alleenActief = false);
        GetResponse<EmailTemplateBO> GetById(int id);
        Response InsertUpdate(EmailTemplateBO bo);
        Response Delete(int id);
    }
}
