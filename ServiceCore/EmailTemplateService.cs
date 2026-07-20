using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using System;
using System.Linq;

namespace ServiceCore
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly UnitOfWorkCore _uow;

        public EmailTemplateService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public GetResponse<EmailTemplateBO> GetAll(bool alleenActief = false)
        {
            var response = new GetResponse<EmailTemplateBO>();

            var query = _uow.EmailTemplates.GetNoTracking().AsQueryable();
            if (alleenActief)
                query = query.Where(t => t.IsActief);

            foreach (var e in query.OrderBy(t => t.Naam).ToList())
                response.AddValue(MapToBO(e));

            return response;
        }

        public GetResponse<EmailTemplateBO> GetById(int id)
        {
            var response = new GetResponse<EmailTemplateBO>();

            var entity = _uow.EmailTemplates.GetNoTracking().SingleOrDefault(t => t.Id == id);
            if (entity == null)
            {
                response.AddError("Template niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity));
            return response;
        }

        public Response InsertUpdate(EmailTemplateBO bo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(bo?.Naam))
            {
                response.AddError("Naam is verplicht.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(bo.Onderwerp))
            {
                response.AddError("Onderwerp is verplicht.");
                return response;
            }

            EmailTemplate entity;

            if (bo.ID == 0)
            {
                entity = _uow.EmailTemplates.GetNew();
                entity.AangemaaktOp = DateTime.Now;
            }
            else
            {
                entity = _uow.EmailTemplates.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Template niet gevonden.");
                    return response;
                }
            }

            entity.Naam = bo.Naam;
            entity.Onderwerp = bo.Onderwerp;
            entity.BodyHtml = bo.BodyHtml;
            entity.IsActief = bo.IsActief;
            entity.GewijzigdOp = DateTime.Now;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Template opgeslagen.", "Template niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response Delete(int id)
        {
            var response = new Response();

            _uow.EmailTemplates.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Template verwijderd.", "Template niet verwijderd.");

            return response;
        }

        private static EmailTemplateBO MapToBO(EmailTemplate e) => new EmailTemplateBO
        {
            ID = e.Id,
            Naam = e.Naam,
            Onderwerp = e.Onderwerp,
            BodyHtml = e.BodyHtml,
            IsActief = e.IsActief,
            AangemaaktOp = e.AangemaaktOp,
            GewijzigdOp = e.GewijzigdOp
        };
    }
}
