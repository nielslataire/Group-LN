using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;

namespace ServiceCore
{
    public class ContactService : IContactService
    {
        public GetResponse<ContactBO> GetContacts()
        {
            GetResponse<ContactBO> response = new GetResponse<ContactBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyContactsDAO();

            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                ContactBO bo = new ContactBO();
                var err = ContactTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ContactBO> GetContactById(int id)
        {
            GetResponse<ContactBO> response = new GetResponse<ContactBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyContactsDAO();
            var _entity = dao.GetById(id);
            ContactBO bo = new ContactBO();
            var err = ContactTranslator.TranslateEntityToBO(_entity, bo);
            if (err == ErrorCode.Success)
                response.AddValue(bo);
            else
                response.AddError(err.ToString());

            return response;
        }
        public GetResponse<ContactBO> GetContactsByIds(List<int> IdList)
        {
            GetResponse<ContactBO> response = new GetResponse<ContactBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyContactsDAO();
            foreach (var id in IdList)
            {
                var _entity = dao.GetById(id);
                ContactBO bo = new ContactBO();
                var err = ContactTranslator.TranslateEntityToBO(_entity, bo);
                if (err == ErrorCode.Success)
                    response.AddValue(bo);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<SelectBO> GetContactsForSearchList(string searchterm)
        {
            GetResponse<SelectBO> response = new GetResponse<SelectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyContactsDAO();
            var entitys = dao.GetNoTracking().Where(ContactQuery.GetNameQuery(searchterm)).OrderBy(m => m.ContactNaam).Select(m => new SelectBO() { id = m.CompanyId, text = m.ContactNaam + " " + m.ContactVoornaam + " - " + m.Company.BedrijfsNaam, extra = "Contact" });
            response.Values = entitys.ToList();
            // For Each _entity In entitys
            // response.AddValue(_entity.GetIdNameForSearch())
            // Next
            return response;
        }

        public Response InsertUpdate(ContactBO bo)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(bo.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetCompanyContactsDAO();
            CompanyContacts _entity = null/* TODO Change to default(_) if this is not a reference type */;

            if ((bo.Id == 0))
                _entity = dao.GetNew();
            else
                _entity = dao.GetById(bo.Id);
            if ((_entity != null))
            {
                var err = ContactTranslator.TranslateBOToEntity(_entity, bo);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("department not found");

            response.AddError(uow.SaveChanges());
            return response;
        }

        public Response Delete(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetCompanyContactsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response Delete(List<ContactBO> bos)
        {
            return Delete(bos.Select(s => s.Id).ToList());
        }
    }
}
