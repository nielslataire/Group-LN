using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using ServiceCore.Translators;
using DALCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Diagnostics;

namespace ServiceCore
{
    public class ClientService : IClientService
    {
        private readonly UnitOfWorkCore _uow;

        public ClientService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        // ========== CLIENT ACCOUNTS ==========
        public GetResponse<ClientAccountBO> GetClientAccountById(int id)
        {
            var response = new GetResponse<ClientAccountBO>();

            var entity = _uow.ClientAccounts
                .GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.PostalCode).ThenInclude(pc => pc.Country)
                .Include(m => m.InvoicePostalCode).ThenInclude(pc => pc.Country)
                .Include(m => m.OwnerType)
                .Include(m => m.ClientContacts).ThenInclude(c => c.PostalCode).ThenInclude(pc => pc.Country)
                .Include(m => m.ClientContacts).ThenInclude(c => c.InvoicePostalCode).ThenInclude(pc => pc.Country)
                .Include(m => m.ClientContacts).ThenInclude(c => c.CoOwnerType)
                .AsSplitQuery()                 // veiliger bij meerdere includes
                .SingleOrDefault();

            var bo = new ClientAccountBO();
            var err = ClientAccountTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<ClientAccountBO> GetClientAccountByIds(List<int> ids)
        {
            var response = new GetResponse<ClientAccountBO>();

            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => ids.Contains(m.Id));

            foreach (var e in entities)
            {
                var bo = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientAccountWithUnitsBO> GetClientAccountByIdWithUnits(int id)
        {
            var response = new GetResponse<ClientAccountWithUnitsBO>();

            var entity = _uow.ClientAccounts
                .GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.Units).ThenInclude(u => u.Project)
                .Include(m => m.Units).ThenInclude(u => u.Type)
                .Include(m => m.PostalCode).ThenInclude(pc => pc.Country)
                .Include(m => m.PostalCode).ThenInclude(pc => pc.Provincie)
                .FirstOrDefault();

            var bo = new ClientAccountWithUnitsBO();
            var err = ClientAccountTranslator.TranslateEntityToBO_WithUnits(entity, bo);
            if (err == ErrorCode.Success) response.AddValue(bo);
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<SelectBO> GetClientAccountsForSearchList(string searchterm)
        {
            var response = new GetResponse<SelectBO>();

            var query = _uow.ClientAccounts.GetNoTracking()
                .Where(ClientAccountQuery.GetNameQuery(searchterm))
                .OrderBy(m => m.Name)
                .Select(m => new SelectBO
                {
                    id = m.Id,
                    text = (m.Name ?? string.Empty) + (m.CompanyName ?? string.Empty),
                    extra = "Client"
                });

            response.Values = query.ToList();
            return response;
        }

        public string GetClientAccountNameById(int id)
        {
            var e = _uow.ClientAccounts.GetById(id);
            if (e == null) return string.Empty;
            return string.IsNullOrEmpty(e.CompanyName) ? (e.Name ?? string.Empty) : e.CompanyName;
        }

        public string GetClientAccountUnitsNameById(int id)
        {
            var e = _uow.ClientAccounts.GetNoTracking()
                .Where(m => m.Id == id)
                .Include(m => m.Units).ThenInclude(u => u.Type)
                .FirstOrDefault();
            if (e == null) return string.Empty;

            var units = e.Units.Where(m => m.Type.Selectable == true)
                               .OrderBy(m => m.Type.GroupId)
                               .ToList();

            return string.Join(" - ", units.Select(u => $"{u.Type.Name} {u.Name}"));
        }

        public GetResponse<ClientAccountBO> GetClientAccountsByProjectId(int projectId)
        {
            var response = new GetResponse<ClientAccountBO>();

            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => m.Units.Any(i => i.ProjectId == projectId));

            foreach (var e in entities)
            {
                var bo = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientAccountWithUnitsBO> GetClientAccountsByProjectIdWithUnits(int projectId)
        {
            var response = new GetResponse<ClientAccountWithUnitsBO>();

            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => m.Units.Any(i => i.ProjectId == projectId))
                .Include(m => m.Units).ThenInclude(unit => unit.Project)
                .Include(m => m.Units).ThenInclude(unit => unit.Type)
                .Include(m => m.PostalCode).ThenInclude(pc => pc.Country)
                .Include(m => m.PostalCode).ThenInclude(pc => pc.Provincie);

            foreach (var e in entities)
            {
                var bo = new ClientAccountWithUnitsBO();
                var err = ClientAccountTranslator.TranslateEntityToBO_WithUnits(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<IdNameBO> GetClientAccountsByProjectIdForSelect(int projectId)
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => m.Units.First().ProjectId == projectId);

            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<IdNameBO> GetClientAccountsByProjectIdLast5(int projectId)
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => m.Units.First().ProjectId == projectId)
                .OrderByDescending(m => m.DateSalesAgreement)
                .Take(5);

            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        public GetResponse<ClientAccountBO> GetClientAccountsByUnitIds(List<int> unitIds)
        {
            var response = new GetResponse<ClientAccountBO>();

            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => m.Units.Any(i => unitIds.Contains(i.Id)))
                .Distinct();

            foreach (var e in entities)
            {
                var bo = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientAccountBO> GetClientAccountsByDateDeedofSale()
        {
            var response = new GetResponse<ClientAccountBO>();

            var threeMonthsAgo = DateOnly.FromDateTime(DateTime.Now.AddMonths(-3));
            var entities = _uow.ClientAccounts.GetNoTracking()
                .Where(m => threeMonthsAgo > m.DateSalesAgreement && m.DateDeedOfSale == null);

            foreach (var e in entities)
            {
                var bo = new ClientAccountBO();
                var err = ClientAccountTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdate(ClientAccountBO clientaccount)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(clientaccount.Name) && string.IsNullOrWhiteSpace(clientaccount.CompanyName))
            {
                response.AddError("name or companyname is mandatory");
                return response;
            }

            ClientAccount entity;

            if (clientaccount.Id == 0)
            {
                entity = _uow.ClientAccounts.GetNew();

                // Zorg dat nieuwe entity ook echt wordt toegevoegd aan de context,
                // voor het geval jouw GetNew() enkel 'new' doet:
                if (_uow.Context.Entry(entity).State == EntityState.Detached)
                    _uow.Context.Set<ClientAccount>().Add(entity);
            }
            else
            {
                // Bestaande account incl. contacten (tracked)
                entity = _uow.Context.Set<ClientAccount>()
                         .Include(ca => ca.ClientContacts)   // pas propertynaam aan indien nodig
                         .FirstOrDefault(ca => ca.Id == clientaccount.Id);
            }

            if (entity == null)
            {
                response.AddError("clientaccount not found");
                return response;
            }

            var err = ClientAccountTranslator.TranslateBOToEntity(entity, clientaccount, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            // Slim saven: buiten transactie echt saven; binnen transactie "succes" teruggeven
            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;

            response.InsertedId = entity.Id;
            response.AddSaveChangesResult(result, "Klant aangepast of toegevoegd", "Klant niet aangepast of toegevoegd");
            return response;
        }




        public Response AddClientAccountToUnit(int unitId, int accountId)
        {
            var response = new Response();

            var unit = _uow.Units.GetById(unitId);
            if (unit == null)
            {
                response.AddError("unit not found");
                return response;
            }

            unit.ClientAccountId = accountId;

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Unit gekoppeld", "Unit niet gekoppeld");
            return response;
        }

        public Response Delete(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ClientAccounts.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response Delete(List<ClientAccountBO> bos) => Delete(bos.Select(s => s.Id).ToList());

        // ========== CLIENT CONTACTS ==========
        public GetResponse<ClientContactBO> GetClientContactById(int id)
        {
            var response = new GetResponse<ClientContactBO>();

            var entity = _uow.ClientContacts.GetById(id);
            var bo = new ClientContactBO();
            var err = ClientContactTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<decimal> GetMaxOwnerPercentage(int accountId, int ownerId)
        {
            var response = new GetResponse<decimal>();

            decimal max = 99.99m;
            var sum = _uow.ClientContacts.GetNoTracking()
                        .Where(m => m.ClientAccountId == accountId && m.IsCoOwner && m.Id != ownerId)
                        .Sum(m => (decimal?)m.CoOwnerPercentage) ?? 0m;

            response.Value = max - sum;
            return response;
        }

        public Response InsertUpdateClientContact(ClientContactBO contact)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(contact.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            ClientContacts entity = contact.Id == 0
                ? _uow.ClientContacts.GetNew()
                : _uow.ClientContacts.GetById(contact.Id);

            if (entity == null)
            {
                response.AddError("contact not found");
                return response;
            }

            var err = ClientContactTranslator.TranslateBOToEntity(entity, contact, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Contact aangepast of toegevoegd", "Contact niet aangepast of toegevoegd");
            return response;
        }

        public Response DeleteClientContact(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ClientContacts.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response DeleteClientContact(List<ClientContactBO> bos) => DeleteClientContact(bos.Select(s => s.Id).ToList());

        // ========== CLIENT OWNER TYPE ==========
        public GetResponse<ClientOwnerTypeBO> GetClientOwnerTypeById(int id)
        {
            var response = new GetResponse<ClientOwnerTypeBO>();
            var entity = _uow.ClientOwnerTypes.GetById(id);

            var bo = new ClientOwnerTypeBO();
            var err = ClientOwnerTypeTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<ClientOwnerTypeBO> GetOwnerTypes()
        {
            var response = new GetResponse<ClientOwnerTypeBO>();
            var entities = _uow.ClientOwnerTypes.GetNoTracking();

            foreach (var e in entities)
                response.AddValue(new ClientOwnerTypeBO { Id = e.Id, Name = e.Name });

            return response;
        }

        public GetResponse<IdNameBO> GetOwnerTypesForSelect()
        {
            var response = new GetResponse<IdNameBO>();
            var entities = _uow.ClientOwnerTypes.GetNoTracking();
            foreach (var e in entities) response.AddValue(e.GetIdName());
            return response;
        }

        public Response InsertUpdateClientOwnerType(ClientOwnerTypeBO ownertype)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(ownertype.Name))
            {
                response.AddError("name is mandatory");
                return response;
            }

            ClientOwnerType entity = ownertype.Id == 0
                ? _uow.ClientOwnerTypes.GetNew()
                : _uow.ClientOwnerTypes.GetById(ownertype.Id);

            if (entity == null)
            {
                response.AddError("ownertype not found");
                return response;
            }

            var err = ClientOwnerTypeTranslator.TranslateBOToEntity(entity, ownertype, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Eigenaartype aangepast of toegevoegd", "Eigenaartype niet aangepast of toegevoegd");
            return response;
        }

        public Response DeleteClientOwnerType(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ClientOwnerTypes.DeleteObject(id);

            var result = _uow.SaveChanges();
            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response DeleteClientOwnerType(List<ClientOwnerTypeBO> bos) => DeleteClientOwnerType(bos.Select(s => s.Id).ToList());

        // ========== CLIENT GIFTS ==========
        public GetResponse<ClientGiftBO> GetClientGiftById(int id)
        {
            var response = new GetResponse<ClientGiftBO>();
            var entity = _uow.ClientGifts.GetById(id);

            var bo = new ClientGiftBO();
            var err = ClientGiftTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<ClientGiftBO> GetClientGiftByAccountId(int accountId)
        {
            var response = new GetResponse<ClientGiftBO>();

            var entities = _uow.ClientGifts.GetNoTracking()
                .Where(m => m.ClientAccountId == accountId)
                .Include(m => m.Activity).ThenInclude(a => a.Group)
                .ToList();

            foreach (var e in entities)
            {
                var bo = new ClientGiftBO();
                var err = ClientGiftTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientGiftWithAccountDetailsBO> GetClientsGifts(int projectId)
        {
            var response = new GetResponse<ClientGiftWithAccountDetailsBO>();

            var entities = _uow.ClientGifts.GetNoTracking()
                .Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectId));

            foreach (var e in entities)
            {
                var bo = new ClientGiftWithAccountDetailsBO();
                var err = ClientGiftTranslator.TranslateEntityToBO(e, bo);

                if (e.ClientAccount.Name is not null)
                {
                    var sal = (Salutation)Enum.Parse(typeof(Salutation), e.ClientAccount.Salutation);
                    bo.AccountName = sal.GetDisplayName() + " " + e.ClientAccount.Name;
                }
                else
                {
                    bo.AccountName = e.ClientAccount.CompanyName;
                }

                foreach (var unit in e.ClientAccount.Units.Where(m => m.TypeId == 1))
                    bo.LivingUnit = bo.LivingUnit + " " + unit.Type.Name + " " + unit.Name;

                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientGiftWithAccountDetailsBO> GetClientsGifts(int projectId, List<int> activities)
        {
            var response = new GetResponse<ClientGiftWithAccountDetailsBO>();

            var entities = _uow.ClientGifts.GetNoTracking()
                .Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectId)
                            && m.Activity.Any(s => activities.Contains(s.ActivityId)));

            foreach (var e in entities)
            {
                var bo = new ClientGiftWithAccountDetailsBO();
                var err = ClientGiftTranslator.TranslateEntityToBO(e, bo);

                if (e.ClientAccount.Name is not null)
                {
                    var sal = (Salutation)Enum.Parse(typeof(Salutation), e.ClientAccount.Salutation);
                    bo.AccountName = sal.GetDisplayName() + " " + e.ClientAccount.Name;
                }
                else
                {
                    bo.AccountName = e.ClientAccount.CompanyName;
                }

                foreach (var unit in e.ClientAccount.Units.Where(m => m.TypeId == 1))
                    bo.LivingUnit += " " + unit.Type.Name + " " + unit.Name;

                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateClientGift(ClientGiftBO gift)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(gift.Description))
            {
                response.AddError("description is mandatory");
                return response;
            }

            ClientGift entity;
            if (gift.Id == 0)
            {
                // Nieuwe gift
                entity = _uow.ClientGifts.GetNew() ?? new ClientGift();
                // Zorg dat nieuwe entity tracked is (indien GetNew() enkel 'new' doet)
                if (_uow.Context.Entry(entity).State == EntityState.Detached)
                    _uow.Context.Set<ClientGift>().Add(entity);
            }
            else
            {
                // Bestaande gift: tracked load MET activities
                entity = _uow.Context.Set<ClientGift>()
                    .Include(g => g.Activity) // <-- of: .Include(g => g.GiftActivities).ThenInclude(ga => ga.Activity)
                    .FirstOrDefault(g => g.Id == gift.Id);

                if (entity == null)
                {
                    response.AddError("clientgift not found");
                    return response;
                }
            }


            var err = ClientGiftTranslator.TranslateBOToEntity(entity, gift, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }

            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;


            response.InsertedId = entity.Id;
            response.AddSaveChangesResult(result, "Toegift aangepast of toegevoegd", "Toegift niet aangepast of toegevoegd");
            return response;
        }

        public Response DeleteClientGift(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ClientGifts.DeleteObject(id);

            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;

            response.AddSaveChangesResult(result, "Toegift verwijderd", "Toegift niet verwijderd");
            return response;
        }

        public Response DeleteClientGift(List<ClientGiftBO> bos) => DeleteClientGift(bos.Select(s => s.Id).ToList());

        // ========== CLIENT POA ==========
        public GetResponse<ClientPoaBO> GetClientPoaById(int id)
        {
            var response = new GetResponse<ClientPoaBO>();

            var entity = _uow.ClientPoas.GetById(id);
            var bo = new ClientPoaBO();
            var err = ClientPoaTranslator.TranslateEntityToBO(entity, bo);
            if (err == ErrorCode.Success) response.Value = bo;
            else response.AddError(err.ToString());

            return response;
        }

        public GetResponse<ClientPoaBO> GetClientPoaByAccountId(int accountId)
        {
            var response = new GetResponse<ClientPoaBO>();

            var entities = _uow.ClientPoas.GetNoTracking()
                .Where(m => m.ClientAccountId == accountId)
                .Include(m => m.Activity).ThenInclude(a => a.Group)
                .ToList();

            foreach (var e in entities)
            {
                var bo = new ClientPoaBO();
                var err = ClientPoaTranslator.TranslateEntityToBO(e, bo);
                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientPoaWithAccountDetailsBO> GetClientsPoas(int projectId)
        {
            var response = new GetResponse<ClientPoaWithAccountDetailsBO>();

            var entities = _uow.ClientPoas.GetNoTracking()
                .Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectId));

            foreach (var e in entities)
            {
                var bo = new ClientPoaWithAccountDetailsBO();
                var err = ClientPoaTranslator.TranslateEntityToBO(e, bo);

                if (e.ClientAccount.Name is not null)
                {
                    var sal = (Salutation)Enum.Parse(typeof(Salutation), e.ClientAccount.Salutation);
                    bo.AccountName = sal.GetDisplayName() + " " + e.ClientAccount.Name;
                }
                else
                {
                    bo.AccountName = e.ClientAccount.CompanyName;
                }

                foreach (var unit in e.ClientAccount.Units.Where(m => m.TypeId == 1))
                    bo.LivingUnit = bo.LivingUnit + " " + unit.Type.Name + " " + unit.Name;

                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public GetResponse<ClientPoaWithAccountDetailsBO> GetClientsPoas(int projectId, List<int> activities)
        {
            var response = new GetResponse<ClientPoaWithAccountDetailsBO>();

            var entities = _uow.ClientPoas.GetNoTracking()
                .Where(m => m.ClientAccount.Units.Any(i => i.ProjectId == projectId)
                            && m.Activity.Any(s => activities.Contains(s.ActivityId)));

            foreach (var e in entities)
            {
                var bo = new ClientPoaWithAccountDetailsBO();
                var err = ClientPoaTranslator.TranslateEntityToBO(e, bo);

                if (e.ClientAccount.Name is not null)
                {
                    var sal = (Salutation)Enum.Parse(typeof(Salutation), e.ClientAccount.Salutation);
                    bo.AccountName = sal.GetDisplayName() + " " + e.ClientAccount.Name;
                }
                else
                {
                    bo.AccountName = e.ClientAccount.CompanyName;
                }

                foreach (var unit in e.ClientAccount.Units.Where(m => m.TypeId == 1))
                    bo.LivingUnit += " " + unit.Type.Name + " " + unit.Name;

                if (err == ErrorCode.Success) response.AddValue(bo);
                else response.AddError(err.ToString());
            }
            return response;
        }

        public Response InsertUpdateClientPoa(ClientPoaBO poa)
        {
            var response = new Response();
            if (string.IsNullOrWhiteSpace(poa.Description))
            {
                response.AddError("description is mandatory");
                return response;
            }
            ClientPoa entity = poa.Id == 0
                ? _uow.ClientPoas.GetNew()
                : _uow.Context.Set<ClientPoa>()
                    .Include(p => p.Activity)     // <— LAAD bestaande activiteiten (tracked)
                    .FirstOrDefault(p => p.Id == poa.Id);

            if (entity == null)
            {
                response.AddError("clientpoa not found");
                return response;
            }

            var err = ClientPoaTranslator.TranslateBOToEntity(entity, poa, _uow);
            if (err != ErrorCode.Success)
            {
                response.AddError(err.ToString());
                return response;
            }
            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;


            response.InsertedId = entity.Id;
            response.AddSaveChangesResult(result, "Aandachtspunt aangepast of toegevoegd", "Aandachtspunt niet aangepast of toegevoegd");
            return response;
        }

        public Response DeleteClientPoa(List<int> ids)
        {
            var response = new Response();
            foreach (var id in ids)
                _uow.ClientPoas.DeleteObject(id);

            var hasTx = _uow.HasActiveTransaction;
            var saved = _uow.SaveIfNoActiveTransaction(); // 0 als hasTx==true, anders echte savecount
            var result = hasTx ? 1 : saved;

            response.AddSaveChangesResult(result, "Record(s) verwijderd", "Geen records verwijderd");
            return response;
        }

        public Response DeleteClientPoa(List<ClientPoaBO> bos) => DeleteClientPoa(bos.Select(s => s.Id).ToList());

        private void DumpChanges()
        {
            var entries = _uow.Context.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
                .Select(e => new
                {
                    Entity = e.Entity.GetType().Name,
                    e.State,
                    ModifiedProps = e.Properties
                        .Where(p => p.IsModified || e.State == EntityState.Added)
                        .Select(p => $"{p.Metadata.Name}: '{p.OriginalValue}' -> '{p.CurrentValue}'")
                        .ToList()
                });

            foreach (var x in entries)
                Debug.WriteLine($"{x.Entity} [{x.State}] {string.Join(", ", x.ModifiedProps)}");
        }
    }
}
