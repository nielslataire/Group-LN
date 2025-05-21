using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IClientService
    {
        // ClIENT ACCOUNTS
        GetResponse<ClientAccountBO> GetClientAccountById(int id);
        GetResponse<ClientAccountBO> GetClientAccountByIds(List<int> ids);
        GetResponse<ClientAccountWithUnitsBO> GetClientAccountByIdWithUnits(int id);
        GetResponse<SelectBO> GetClientAccountsForSearchList(string searchterm);
        string GetClientAccountNameById(int id);
        string GetClientAccountUnitsNameById(int id);
        GetResponse<ClientAccountBO> GetClientAccountsByProjectId(int id);
        GetResponse<ClientAccountWithUnitsBO> GetClientAccountsByProjectIdWithUnits(int id);
        GetResponse<IdNameBO> GetClientAccountsByProjectIdForSelect(int projectid);
        GetResponse<IdNameBO> GetClientAccountsByProjectIdLast5(int projectid);
        GetResponse<ClientAccountBO> GetClientAccountsByUnitIds(List<int> Unitids);
        GetResponse<ClientAccountBO> GetClientAccountsByDateDeedofSale();
        Response InsertUpdate(ClientAccountBO company);
        Response AddClientAccountToUnit(int unitId, int accountId);
        Response Delete(List<int> ids);
        Response Delete(List<ClientAccountBO> bos);

        // CLIENT CONTACTS
        GetResponse<ClientContactBO> GetClientContactById(int id);
        GetResponse<decimal> GetMaxOwnerPercentage(int accountid, int ownerid);
        Response InsertUpdateClientContact(ClientContactBO company);
        Response DeleteClientContact(List<int> ids);
        Response DeleteClientContact(List<ClientContactBO> bos);

        // CLIENT OWNER TYPE
        GetResponse<ClientOwnerTypeBO> GetClientOwnerTypeById(int id);
        GetResponse<ClientOwnerTypeBO> GetOwnerTypes();
        GetResponse<IdNameBO> GetOwnerTypesForSelect();
        Response InsertUpdateClientOwnerType(ClientOwnerTypeBO company);
        Response DeleteClientOwnerType(List<int> ids);
        Response DeleteClientOwnerType(List<ClientOwnerTypeBO> bos);

        // ClIENT GIFT
        GetResponse<ClientGiftBO> GetClientGiftById(int id);
        GetResponse<ClientGiftBO> GetClientGiftByAccountId(int id);
        GetResponse<ClientGiftWithAccountDetailsBO> GetClientsGifts(int projectid);
        GetResponse<ClientGiftWithAccountDetailsBO> GetClientsGifts(int projectid, List<int> activities);
        Response InsertUpdateClientGift(ClientGiftBO company);
        Response DeleteClientGift(List<int> ids);
        Response DeleteClientGift(List<ClientGiftBO> bos);

        // ClIENT POINT OF ATTENTION (POA)
        GetResponse<ClientPoaBO> GetClientPoaById(int id);
        GetResponse<ClientPoaBO> GetClientPoaByAccountId(int id);
        GetResponse<ClientPoaWithAccountDetailsBO> GetClientsPoas(int projectid);
        GetResponse<ClientPoaWithAccountDetailsBO> GetClientsPoas(int projectid, List<int> activities);
        Response InsertUpdateClientPoa(ClientPoaBO company);
        Response DeleteClientPoa(List<int> ids);
        Response DeleteClientPoa(List<ClientPoaBO> bos);
    }
}
