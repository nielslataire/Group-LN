using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IContactService
    {
        GetResponse<ContactBO> GetContacts();
        GetResponse<ContactBO> GetContactById(int id);
        GetResponse<ContactBO> GetContactsByIds(List<int> IdList);
        GetResponse<SelectBO> GetContactsForSearchList(string searchterm);
        Response InsertUpdate(ContactBO bo);
        Response Delete(List<int> ids);
        Response Delete(List<ContactBO> bos);
    }
}
