Imports BO
Public Interface IClientService

    'ClIENT ACCOUNTS
    Function GetClientAccountById(id As Integer) As GetResponse(Of ClientAccountBO)
    Function GetClientAccountByIds(ids As List(Of Integer)) As GetResponse(Of ClientAccountBO)
    Function GetClientAccountsByIdWithUnits(id As Integer) As GetResponse(Of ClientAccountWithUnitsBO)
    Function GetClientAccountsForSearchList(searchterm As String) As GetResponse(Of SelectBO)
    Function GetClientAccountNameById(id As Integer) As String
    Function GetClientAccountUnitsNameById(id As Integer) As String
    Function GetClientAccountsByProjectId(id As Integer) As GetResponse(Of ClientAccountBO)
    Function GetClientAccountsByProjectIdWithUnits(id As Integer) As GetResponse(Of ClientAccountWithUnitsBO)
    Function GetClientAccountsByProjectIdForSelect(projectid As Integer) As GetResponse(Of IdNameBO)
    Function GetClientAccountsByProjectIdLast5(projectid As Integer) As GetResponse(Of IdNameBO)
    Function GetClientAccountsByUnitIds(Unitids As List(Of Integer)) As GetResponse(Of ClientAccountBO)
    Function GetClientAccountsByDateDeedofSale() As GetResponse(Of ClientAccountBO)
    Function InsertUpdate(company As ClientAccountBO) As Response
    Function AddClientAccountToUnit(unitId As Integer, accountId As Integer) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of ClientAccountBO)) As Response

    'CLIENT CONTACTS
    Function GetClientContactById(id As Integer) As GetResponse(Of ClientContactBO)
    Function GetMaxOwnerPercentage(accountid As Integer, ownerid As Integer) As GetResponse(Of Decimal)
    Function InsertUpdateClientContact(company As ClientContactBO) As Response
    Function DeleteClientContact(ids As List(Of Integer)) As Response
    Function DeleteClientContact(bos As List(Of ClientContactBO)) As Response

    'CLIENT OWNER TYPE
    Function GetClientOwnerTypeById(id As Integer) As GetResponse(Of ClientOwnerTypeBO)
    Function GetOwnerTypes() As GetResponse(Of ClientOwnerTypeBO)
    Function GetOwnerTypesForSelect() As GetResponse(Of IdNameBO)
    Function InsertUpdateClientOwnerType(company As ClientOwnerTypeBO) As Response
    Function DeleteClientOwnerType(ids As List(Of Integer)) As Response
    Function DeleteClientOwnerType(bos As List(Of ClientOwnerTypeBO)) As Response

    'ClIENT GIFT
    Function GetClientGiftById(id As Integer) As GetResponse(Of ClientGiftBO)
    Function GetClientGiftByAccountId(id As Integer) As GetResponse(Of ClientGiftBO)
    Function GetClientsGifts(projectid As Integer) As GetResponse(Of ClientGiftWithAccountDetailsBO)
    Function GetClientsGifts(projectid As Integer, activities As List(Of Integer)) As GetResponse(Of ClientGiftWithAccountDetailsBO)
    Function InsertUpdateClientGift(company As ClientGiftBO) As Response
    Function DeleteClientGift(ids As List(Of Integer)) As Response
    Function DeleteClientGift(bos As List(Of ClientGiftBO)) As Response

    'ClIENT POINT OF ATTENTION (POA)
    Function GetClientPoaById(id As Integer) As GetResponse(Of ClientPoaBO)
    Function GetClientPoaByAccountId(id As Integer) As GetResponse(Of ClientPoaBO)
    Function GetClientsPoas(projectid As Integer) As GetResponse(Of ClientPoaWithAccountDetailsBO)
    Function GetClientsPoas(projectid As Integer, activities As List(Of Integer)) As GetResponse(Of ClientPoaWithAccountDetailsBO)
    Function InsertUpdateClientPoa(company As ClientPoaBO) As Response
    Function DeleteClientPoa(ids As List(Of Integer)) As Response
    Function DeleteClientPoa(bos As List(Of ClientPoaBO)) As Response



End Interface
