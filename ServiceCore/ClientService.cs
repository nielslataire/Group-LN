using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class ClientService : IClientService
    {
        // ClIENT ACCOUNTS
        public GetResponse<ClientAccountBO> GetClientAccountById(int id)
        {
            GetResponse<ClientAccountBO> response = new GetResponse<ClientAccountBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();

            var _entity = dao.GetNormal(m => m.Id == id)
                .Include(m => m.PostalCode)
                .ThenInclude(m => m.Country)
                .Include(m => m.InvoicePostalCode)
                .ThenInclude(m => m.Country)
                .Include(m => m.OwnerType)
                .Include(m => m.ClientContacts)
                .ThenInclude(m => m.PostalCode)
                .ThenInclude(m => m.Country)
                .Include(m => m.ClientContacts)
                .ThenInclude(m => m.InvoicePostalCode)
                .ThenInclude(m => m.Country)
                .Include(m => m.ClientContacts)
                .ThenInclude(m => m.CoOwnerType)
                .FirstOrDefault();
            ClientAccountBO clientaccount = new ClientAccountBO();

            var err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount);
            if (err == ErrorCode.Success)
                response.Value = clientaccount;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ClientAccountBO> GetClientAccountByIds(List<int> ids)
        {
            GetResponse<ClientAccountBO> response = new GetResponse<ClientAccountBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            var entities = dao.GetNoTracking().Where(m => ids.Contains(m.Id));
            foreach (var _entity in entities)
            {
                ClientAccountBO clientaccount = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount);
                if (err == ErrorCode.Success)
                    response.AddValue(clientaccount);
                else
                    response.AddError(err.ToString());
            }

            return response;
        }
        public GetResponse<ClientAccountWithUnitsBO> GetClientAccountByIdWithUnits(int id)
        {
            GetResponse<ClientAccountWithUnitsBO> response = new GetResponse<ClientAccountWithUnitsBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();

            var _entity = dao.GetById(id);

            ClientAccountWithUnitsBO clientaccount = new ClientAccountWithUnitsBO();
            var err = ClientAccountTranslator.TranslateEntityToBO_WithUnits(_entity, clientaccount);
            if (err == ErrorCode.Success)
                response.AddValue(clientaccount);
            else
                response.AddError(err.ToString());

            return response;
        }
        public GetResponse<SelectBO> GetClientAccountsForSearchList(string searchterm)
        {
            GetResponse<SelectBO> response = new GetResponse<SelectBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();   
            var entitys = dao.GetNoTracking().Where(ClientAccountQuery.GetNameQuery(searchterm)).OrderBy(m => m.Name).Select(m => new SelectBO() { id = m.Id, text = m.Name + m.CompanyName, extra = "Client" });
            response.Values = entitys.ToList();
            // For Each _entity In entitys
            // response.AddValue(_entity.GetIdNameForSearch())
            // Next
            return response;
        }
        public string GetClientAccountNameById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            string companyname = dao.GetById(id)?.CompanyName ?? string.Empty;
            if (string.IsNullOrEmpty(companyname))
                return dao.GetById(id)?.Name ?? string.Empty;
            else
                return companyname;
        }
        public string GetClientAccountUnitsNameById(int id)
        {
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            string name = "";
            int i = 0;
            var entities = dao.GetById(id).Units.Where(m => m.Type.Selectable == true).OrderBy(m => m.Type.GroupId);
            foreach (var _entity in entities)
            {
                if (_entity.Id == entities.First().Id)
                    name = _entity.Type.Name + " " + _entity.Name;
                else
                    name = name + " - " + _entity.Type.Name + " " + _entity.Name;
            }
            return name;
        }
        public GetResponse<ClientAccountBO> GetClientAccountsByProjectId(int id)
        {
            GetResponse<ClientAccountBO> response = new GetResponse<ClientAccountBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();

            var entities = dao.GetNoTracking().Where(m => m.Units.Any(i => i.ProjectId == id));
            foreach (var _entity in entities)
            {
                ClientAccountBO clientaccount = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount);
                if (err == ErrorCode.Success)
                    response.AddValue(clientaccount);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ClientAccountWithUnitsBO> GetClientAccountsByProjectIdWithUnits(int id)
        {
            GetResponse<ClientAccountWithUnitsBO> response = new GetResponse<ClientAccountWithUnitsBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();

            var entities = dao.GetNoTracking().Where(m => m.Units.Any(i => i.ProjectId == id))
                .Include(m => m.Units)
                .ThenInclude(unit => unit.Project)
                .Include(m => m.Units)
                .ThenInclude(unit => unit.Type)
                .Include(m => m.PostalCode)
                .Include(m => m.PostalCode.Country)
                .Include(m => m.PostalCode.Provincie);

            foreach (var _entity in entities)
            {
                ClientAccountWithUnitsBO clientaccount = new ClientAccountWithUnitsBO();
                var err = ClientAccountTranslator.TranslateEntityToBO_WithUnits(_entity, clientaccount);
                if (err == ErrorCode.Success)
                    response.AddValue(clientaccount);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<IdNameBO> GetClientAccountsByProjectIdForSelect(int projectid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            var entities = dao.GetNoTracking().Where(m => m.Units.First().ProjectId == projectid);
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<IdNameBO> GetClientAccountsByProjectIdLast5(int projectid)
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            var entities = dao.GetNoTracking().Where(m => m.Units.First().ProjectId == projectid);
            foreach (var _entity in entities.OrderByDescending(m => m.DateSalesAgreement).Take(5))
                response.AddValue(_entity.GetIdName());
            return response;
        }
        public GetResponse<ClientAccountBO> GetClientAccountsByUnitIds(List<int> Unitids)
        {
            GetResponse<ClientAccountBO> response = new GetResponse<ClientAccountBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            var entities = dao.GetNoTracking().Where(m => m.Units.Any(i => Unitids.Contains(i.Id))).Distinct();
            foreach (var _entity in entities)
            {
                ClientAccountBO clientaccount = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount);
                if (err == ErrorCode.Success)
                    response.AddValue(clientaccount);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ClientAccountBO> GetClientAccountsByDateDeedofSale()
        {
            GetResponse<ClientAccountBO> response = new GetResponse<ClientAccountBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientAccountDAO();
            var entities = dao.GetNoTracking().Where(m => DateOnly.FromDateTime(DateTime.Now.AddMonths(-3)) > m.DateSalesAgreement  && m.DateDeedOfSale == null);

            foreach (var _entity in entities)
            {
                ClientAccountBO clientaccount = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount);
                if (err == ErrorCode.Success)
                    response.AddValue(clientaccount);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
      

        public Response InsertUpdate(ClientAccountBO clientaccount)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(clientaccount.Name) && string.IsNullOrWhiteSpace(clientaccount.CompanyName)))
                response.AddError("name or companyname is mandatory");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ClientAccount _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((clientaccount.Id == 0))
                _entity = uow.GetClientAccountDAO().GetNew();
            else
                _entity = uow.GetClientAccountDAO().GetById(clientaccount.Id);
            if ((_entity != null))
            {
                var err = ClientAccountTranslator.TranslateBOToEntity(_entity, clientaccount, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("clientaccount not found");
            response.AddError(uow.SaveChanges());
            Message msg = new Message();
            msg.Type = MessageType.Value;
            msg.Message = _entity.Id.ToString();
            response.Messages.Add(msg);
            return response;
        }
        public Response AddClientAccountToUnit(int unitId, int accountid)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            Units _entity = null/* TODO Change to default(_) if this is not a reference type */;
            _entity = uow.GetUnitsDAO().GetById(unitId);
            if ((_entity != null))
                _entity.ClientAccountId = accountid;
            else
                response.AddError("unit not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response Delete(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)

                uow.GetClientAccountDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response Delete(List<ClientAccountBO> bos)
        {
            return Delete(bos.Select(s => s.Id).ToList());
        }

        // CLIENT CONTACTS
        public GetResponse<ClientContactBO> GetClientContactById(int id)
        {
            GetResponse<ClientContactBO> response = new GetResponse<ClientContactBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientContactsDAO();

            var _entity = dao.GetById(id);
            ClientContactBO clientcontact = new ClientContactBO();

            var err = ClientContactTranslator.TranslateEntityToBO(_entity, clientcontact);
            if (err == ErrorCode.Success)
                response.Value = clientcontact;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<decimal> GetMaxOwnerPercentage(int accountid, int ownerid)
        {
            GetResponse<decimal> response = new GetResponse<decimal>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientContactsDAO();
            decimal max = 99.99m;
            max = max - dao.GetNoTracking().Where(m => m.ClientAccountId == accountid && m.IsCoOwner == true && m.Id != ownerid).Sum(m => m.CoOwnerPercentage) ?? 0;
            response.Value = max;
            return response;
        }
        public Response InsertUpdateClientContact(ClientContactBO contact)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(contact.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ClientContacts _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((contact.Id == 0))
                _entity = uow.GetClientContactsDAO().GetNew();
            else
                _entity = uow.GetClientContactsDAO().GetById(contact.Id);
            if ((_entity != null))
            {
                var err = ClientContactTranslator.TranslateBOToEntity(_entity, contact, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("contact not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response DeleteClientContact(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetClientContactsDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response DeleteClientContact(List<ClientContactBO> bos)
        {
            return DeleteClientContact(bos.Select(s => s.Id).ToList());
        }

        // CLIENT OWNER TYPE
        public GetResponse<ClientOwnerTypeBO> GetClientOwnerTypeById(int id)
        {
            GetResponse<ClientOwnerTypeBO> response = new GetResponse<ClientOwnerTypeBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientOwnerTypeDAO();

            var _entity = dao.GetById(id);
            ClientOwnerTypeBO clientownertype = new ClientOwnerTypeBO();

            var err = ClientOwnerTypeTranslator.TranslateEntityToBO(_entity, clientownertype);
            if (err == ErrorCode.Success)
                response.Value = clientownertype;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ClientOwnerTypeBO> GetOwnerTypes()
        {
            GetResponse<ClientOwnerTypeBO> response = new GetResponse<ClientOwnerTypeBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientOwnerTypeDAO();
            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
            {
                ClientOwnerTypeBO bo = new ClientOwnerTypeBO();
                bo.Id = _entity.Id;
                bo.Name = _entity.Name;
                response.AddValue(bo);
            }
            return response;
        }
        public GetResponse<IdNameBO> GetOwnerTypesForSelect()
        {
            GetResponse<IdNameBO> response = new GetResponse<IdNameBO>();
            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientOwnerTypeDAO();
            var entities = dao.GetNoTracking();
            foreach (var _entity in entities)
                response.AddValue(_entity.GetIdName());
            return response;
        }

        public Response InsertUpdateClientOwnerType(ClientOwnerTypeBO ownertype)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(ownertype.Name)))
                response.AddError("name is mandatory");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ClientOwnerType _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((ownertype.Id == 0))
                _entity = uow.GetClientOwnerTypeDAO().GetNew();
            else
                _entity = uow.GetClientOwnerTypeDAO().GetById(ownertype.Id);
            if ((_entity != null))
            {
                var err = ClientOwnerTypeTranslator.TranslateBOToEntity(_entity, ownertype, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("ownertype not found");
            response.AddError(uow.SaveChanges());
            return response;
        }
        public Response DeleteClientOwnerType(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();
            foreach (var id in ids)
                uow.GetClientOwnerTypeDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());
            return response;
        }
        public Response DeleteClientOwnerType(List<ClientOwnerTypeBO> bos)
        {
            return DeleteClientOwnerType(bos.Select(s => s.Id).ToList());
        }

        // CLIENT GIFTS
        public GetResponse<ClientGiftBO> GetClientGiftById(int id)
        {
            GetResponse<ClientGiftBO> response = new GetResponse<ClientGiftBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientGiftDAO();

            var _entity = dao.GetById(id);
            ClientGiftBO clientgift = new ClientGiftBO();

            var err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift);
            if (err == ErrorCode.Success)
                response.Value = clientgift;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ClientGiftBO> GetClientGiftByAccountId(int id)
        {
            GetResponse<ClientGiftBO> response = new GetResponse<ClientGiftBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientGiftDAO();

            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == id);
            foreach (var _entity in entities)
            {
                ClientGiftBO clientgift = new ClientGiftBO();
                var err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift);
                if (err == ErrorCode.Success)
                    response.AddValue(clientgift);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ClientGiftWithAccountDetailsBO> GetClientsGifts(int projectid)
        {
            GetResponse<ClientGiftWithAccountDetailsBO> response = new GetResponse<ClientGiftWithAccountDetailsBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientGiftDAO();
            // And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
            // m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
            var entities = dao.GetNoTracking().Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectid));
            foreach (var _entity in entities)
            {
                ClientGiftWithAccountDetailsBO clientgift = new ClientGiftWithAccountDetailsBO();
                var err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift);
                if (_entity.ClientAccount.Name is not null)
                {
                    Salutation i = (Salutation)Enum.Parse(typeof(Salutation), _entity.ClientAccount.Salutation);
                    clientgift.AccountName = i.GetDisplayName() + " " + _entity.ClientAccount.Name;
                }
                else
                    clientgift.AccountName = _entity.ClientAccount.CompanyName;
                foreach (var unit in _entity.ClientAccount.Units.Where(m => m.TypeId == 1))
                    clientgift.LivingUnit = clientgift.LivingUnit + " " + unit.Type.Name + " " + unit.Name;
                if (err == ErrorCode.Success)
                    response.AddValue(clientgift);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ClientGiftWithAccountDetailsBO> GetClientsGifts(int projectid, List<int> activities)
        {
            GetResponse<ClientGiftWithAccountDetailsBO> response = new GetResponse<ClientGiftWithAccountDetailsBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientGiftDAO();
            // And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
            // m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
            var entities = dao.GetNoTracking().Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectid) && m.Activity.Any(s => activities.Contains(s.ActivityId)));
            foreach (var _entity in entities)
            {
                ClientGiftWithAccountDetailsBO clientgift = new ClientGiftWithAccountDetailsBO();
                var err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift);
                if (_entity.ClientAccount.Name is not null)
                {
                    Salutation i = (Salutation)Enum.Parse(typeof(Salutation), _entity.ClientAccount.Salutation);
                    clientgift.AccountName = i.GetDisplayName() + " " + _entity.ClientAccount.Name;
                }
                else
                    clientgift.AccountName = _entity.ClientAccount.CompanyName;
                foreach (var unit in _entity.ClientAccount.Units.Where(m => m.TypeId == 1))
                    clientgift.LivingUnit = clientgift.LivingUnit + " " + unit.Type.Name + " " + unit.Name;
                if (err == ErrorCode.Success)
                    response.AddValue(clientgift);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdateClientGift(ClientGiftBO gift)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(gift.Description)))
                response.AddError("description is mandatory");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ClientGift _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((gift.Id == 0))
                _entity = uow.GetClientGiftDAO().GetNew();
            else
                _entity = uow.GetClientGiftDAO().GetById(gift.Id);
            if ((_entity != null))
            {
                var err = ClientGiftTranslator.TranslateBOToEntity(_entity, gift, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("clientgift not found");
            response.AddError(uow.SaveChanges());
            return response;
        }

        public Response DeleteClientGift(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetClientGiftDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response DeleteClientGift(List<ClientGiftBO> bos)
        {
            return DeleteClientGift(bos.Select(s => s.Id).ToList());
        }

        // ClIENT POINT OF ATTENTION (POA)
        public GetResponse<ClientPoaBO> GetClientPoaById(int id)
        {
            GetResponse<ClientPoaBO> response = new GetResponse<ClientPoaBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientPoaDAO();

            var _entity = dao.GetById(id);
            ClientPoaBO clientpoa = new ClientPoaBO();

            var err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa);
            if (err == ErrorCode.Success)
                response.Value = clientpoa;
            else
                response.AddError(err.ToString());
            return response;
        }
        public GetResponse<ClientPoaBO> GetClientPoaByAccountId(int id)
        {
            GetResponse<ClientPoaBO> response = new GetResponse<ClientPoaBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientPoaDAO();

            var entities = dao.GetNoTracking().Where(m => m.ClientAccountId == id);
            foreach (var _entity in entities)
            {
                ClientPoaBO clientpoa = new ClientPoaBO();
                var err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa);
                if (err == ErrorCode.Success)
                    response.AddValue(clientpoa);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ClientPoaWithAccountDetailsBO> GetClientsPoas(int projectid)
        {
            GetResponse<ClientPoaWithAccountDetailsBO> response = new GetResponse<ClientPoaWithAccountDetailsBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientPoaDAO();
            // And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
            // m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
            var entities = dao.GetNoTracking().Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectid));
            foreach (var _entity in entities)
            {
                ClientPoaWithAccountDetailsBO clientpoa = new ClientPoaWithAccountDetailsBO();
                var err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa);
                if (_entity.ClientAccount.Name is not null)
                {
                    Salutation i = (Salutation)Enum.Parse(typeof(Salutation), _entity.ClientAccount.Salutation);
                    clientpoa.AccountName = i.GetDisplayName() + " " + _entity.ClientAccount.Name;
                }
                else
                    clientpoa.AccountName = _entity.ClientAccount.CompanyName;
                foreach (var unit in _entity.ClientAccount.Units.Where(m => m.TypeId == 1))
                    clientpoa.LivingUnit = clientpoa.LivingUnit + " " + unit.Type.Name + " " + unit.Name;
                if (err == ErrorCode.Success)
                    response.AddValue(clientpoa);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public GetResponse<ClientPoaWithAccountDetailsBO> GetClientsPoas(int projectid, List<int> activities)
        {
            GetResponse<ClientPoaWithAccountDetailsBO> response = new GetResponse<ClientPoaWithAccountDetailsBO>();

            UnitOfWork uow = new UnitOfWork();
            var dao = uow.GetClientPoaDAO();
            // And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
            // m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
            var entities = dao.GetNoTracking().Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectid) && m.Activity.Any(s => activities.Contains(s.ActivityId)));
            foreach (var _entity in entities)
            {
                ClientPoaWithAccountDetailsBO clientpoa = new ClientPoaWithAccountDetailsBO();
                var err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa);
                if (_entity.ClientAccount.Name is not null)
                {
                    Salutation i = (Salutation)Enum.Parse(typeof(Salutation), _entity.ClientAccount.Salutation);
                    clientpoa.AccountName = i.GetDisplayName() + " " + _entity.ClientAccount.Name;
                }
                else
                    clientpoa.AccountName = _entity.ClientAccount.CompanyName;
                foreach (var unit in _entity.ClientAccount.Units.Where(m => m.TypeId == 1))
                    clientpoa.LivingUnit = clientpoa.LivingUnit + " " + unit.Type.Name + " " + unit.Name;
                if (err == ErrorCode.Success)
                    response.AddValue(clientpoa);
                else
                    response.AddError(err.ToString());
            }
            return response;
        }
        public Response InsertUpdateClientPoa(ClientPoaBO poa)
        {
            Response response = new Response();
            if ((string.IsNullOrWhiteSpace(poa.Description)))
                response.AddError("description is mandatory");
            if ((!response.Success))
                return response;
            UnitOfWork uow = new UnitOfWork();
            ClientPoa _entity = null/* TODO Change to default(_) if this is not a reference type */;
            if ((poa.Id == 0))
                _entity = uow.GetClientPoaDAO().GetNew();
            else
                _entity = uow.GetClientPoaDAO().GetById(poa.Id);
            if ((_entity != null))
            {
                var err = ClientPoaTranslator.TranslateBOToEntity(_entity, poa, uow);
                if ((err != ErrorCode.Success))
                    response.AddError(err.ToString());
            }
            else
                response.AddError("clientpoa not found");
            response.AddError(uow.SaveChanges());
            return response;
        }

        public Response DeleteClientPoa(List<int> ids)
        {
            Response response = new Response();
            UnitOfWork uow = new UnitOfWork();

            foreach (var id in ids)
                uow.GetClientPoaDAO().DeleteObject(id);
            response.Messages.AddRange(uow.SaveChanges());

            return response;
        }
        public Response DeleteClientPoa(List<ClientPoaBO> bos)
        {
            return DeleteClientPoa(bos.Select(s => s.Id).ToList());
        }
    }
}
