using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCore
{
    public class EmailSendLogService : IEmailSendLogService
    {
        private readonly UnitOfWorkCore _uow;

        public EmailSendLogService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public Response Log(EmailSendLogBO bo)
        {
            var response = new Response();

            var entity = _uow.EmailSendLogs.GetNew();
            entity.ProjectId = bo.ProjectId;
            entity.ContactEmail = bo.ContactEmail;
            entity.ContactNaam = bo.ContactNaam;
            entity.EmailTemplateId = bo.EmailTemplateId;
            entity.TemplateNaam = bo.TemplateNaam;
            entity.Onderwerp = bo.Onderwerp;
            entity.VerzondenDoorUserId = bo.VerzondenDoorUserId;
            entity.VerzondenDoorNaam = bo.VerzondenDoorNaam;
            entity.VerzondenOp = DateTime.Now;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Verzending gelogd.", "Verzending niet gelogd.");

            return response;
        }

        public Dictionary<string, EmailSendLogBO> GetLatestPerContact(int projectId)
        {
            var logs = _uow.EmailSendLogs.GetNoTracking()
                .Where(l => l.ProjectId == projectId)
                .ToList();

            return logs
                .GroupBy(l => (l.ContactEmail ?? string.Empty).Trim().ToLowerInvariant())
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(
                    g => g.Key,
                    g => MapToBO(g.OrderByDescending(l => l.VerzondenOp).First()));
        }

        private static EmailSendLogBO MapToBO(EmailSendLog e) => new EmailSendLogBO
        {
            ID = e.Id,
            ProjectId = e.ProjectId,
            ContactEmail = e.ContactEmail,
            ContactNaam = e.ContactNaam,
            EmailTemplateId = e.EmailTemplateId,
            TemplateNaam = e.TemplateNaam,
            Onderwerp = e.Onderwerp,
            VerzondenDoorUserId = e.VerzondenDoorUserId,
            VerzondenDoorNaam = e.VerzondenDoorNaam,
            VerzondenOp = e.VerzondenOp
        };
    }
}
