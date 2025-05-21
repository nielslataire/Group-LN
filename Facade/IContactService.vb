Imports BO
Public Interface IContactService
    Function GetContacts() As GetResponse(Of ContactBO)
    Function GetContactById(id As Integer) As GetResponse(Of ContactBO)
    Function GetContactsByIds(IdList As List(Of Integer)) As GetResponse(Of ContactBO)
    Function GetContactsForSearchList(searchterm As String) As GetResponse(Of SelectBO)
    Function InsertUpdate(bo As ContactBO) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of ContactBO)) As Response

End Interface
