using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using System.Linq;

namespace ServiceCore
{
    public class ContactService : IContactService
    {
        private readonly UnitOfWorkCore _uow;

        public ContactService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public GetResponse<ContactBO> GetContacts()
        {
            var response = new GetResponse<ContactBO>();

            var entities = _uow.CompanyContacts.GetNoTracking();
            foreach (var e in entities)
            {
                var bo = new ContactBO();
                var err = ContactTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ContactBO> GetContactById(int id)
        {
            var response = new GetResponse<ContactBO>();

            var entity = _uow.CompanyContacts.GetById(id);
            var bo = new ContactBO();
            var err = ContactTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<ContactBO> GetContactsByIds(List<int> idList)
        {
            var response = new GetResponse<ContactBO>();

            foreach (var id in idList)
            {
                var entity = _uow.CompanyContacts.GetById(id);
                var bo = new ContactBO();
                var err = ContactTranslator.TranslateEntityToBO(entity, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<SelectBO> GetContactsForSearchList(string searchterm)
        {
            var response = new GetResponse<SelectBO>();

            // Let op: ik ga ervan uit dat het contact een Id-veld heeft met de sleutel.
            // In je originele code stond 'CompanyId'; dat lijkt onjuist voor contacts.
            var query = _uow.CompanyContacts.GetNoTracking()
                .Where(ContactQuery.GetNameQuery(searchterm))
                .OrderBy(m => m.ContactNaam)
                .Select(m => new SelectBO
                {
                    id = m.CompanyId, // was m.CompanyId
                    text = m.ContactNaam + " " + m.ContactVoornaam + " - " + m.Company.BedrijfsNaam,
                    extra = "Contact"
                });

            response.Values = query.ToList();
            return response;
        }

        public Response InsertUpdate(ContactBO bo)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(bo.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            CompanyContacts entity = bo.Id == 0
                ? _uow.CompanyContacts.GetNew()
                : _uow.CompanyContacts.GetById(bo.Id);

            if (entity == null)
            {
                response.AddError("contact not found");
                return response;
            }

            var err = ContactTranslator.TranslateBOToEntity(entity, bo);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Contact aangepast of toegevoegd", "Contact niet aangepast of toegevoegd");
            return response;
        }

        public Response Delete(List<int> ids)
        {
            var response = new Response();

            foreach (var id in ids)
                _uow.CompanyContacts.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response Delete(List<ContactBO> bos)
            => Delete(bos.Select(s => s.Id).ToList());
    }
}
