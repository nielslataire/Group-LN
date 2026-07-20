using System.Collections.Generic;
using BOCore;

namespace FacadeCore
{
    public interface IEmailSendLogService
    {
        Response Log(EmailSendLogBO bo);

        /// <summary>Meest recente verzendlog per contact-e-mailadres (genormaliseerd, lowercase) binnen een project.</summary>
        Dictionary<string, EmailSendLogBO> GetLatestPerContact(int projectId);
    }
}
