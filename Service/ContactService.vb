Imports BO
Imports Facade
Imports DAL

Public Class ContactService
    Implements IContactService

    Function GetContacts() As GetResponse(Of ContactBO) Implements IContactService.GetContacts
        Dim response As New GetResponse(Of ContactBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyContactsDAO()

        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New ContactBO()
            Dim err = ContactTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Function GetContactById(id As Integer) As GetResponse(Of ContactBO) Implements IContactService.GetContactById
        Dim response As New GetResponse(Of ContactBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyContactsDAO()
        Dim _entity = dao.GetById(id)
        Dim bo As New ContactBO()
        Dim err = ContactTranslator.TranslateEntityToBO(_entity, bo)
        If err = ErrorCode.Success Then
            response.AddValue(bo)
        Else
            response.AddError(err.ToString())
        End If

        Return response
    End Function
    Function GetContactsByIds(IdList As List(Of Integer)) As GetResponse(Of ContactBO) Implements IContactService.GetContactsByIds
        Dim response As New GetResponse(Of ContactBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyContactsDAO()
        For Each id In IdList
            Dim _entity = dao.GetById(id)
            Dim bo As New ContactBO()
            Dim err = ContactTranslator.TranslateEntityToBO(_entity, bo)
            If err = ErrorCode.Success Then
                response.AddValue(bo)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Function GetContactsForSearchList(searchterm As String) As GetResponse(Of SelectBO) Implements IContactService.GetContactsForSearchList
        Dim response As New GetResponse(Of SelectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetCompanyContactsDAO
        Dim entitys = dao.GetNoTracking().Where(ContactQuery.GetNameQuery(searchterm)).OrderBy(Function(m) m.ContactNaam).Select(Function(m) New SelectBO With {.id = m.CompanyID, .text = m.ContactNaam & " " & m.ContactVoornaam & " - " & m.CompanyInfo.BedrijfsNaam, .extra = "Contact"})
        response.Values = entitys.ToList()
        'For Each _entity In entitys
        '    response.AddValue(_entity.GetIdNameForSearch())
        'Next
        Return response
    End Function

    Function InsertUpdate(bo As ContactBO) As Response Implements IContactService.InsertUpdate
        Dim response As New Response()
        If (String.IsNullOrWhiteSpace(bo.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response

        Dim uow As New UnitOfWork()
        Dim dao = uow.GetCompanyContactsDAO()
        Dim _entity As CompanyContacts = Nothing

        If (bo.Id = 0) Then
            _entity = dao.GetNew()
        Else
            _entity = dao.GetById(bo.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ContactTranslator.TranslateBOToEntity(_entity, bo)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("department not found")
        End If

        response.AddError(uow.SaveChanges())
        Return response
    End Function

    Function Delete(ids As List(Of Integer)) As Response Implements IContactService.Delete
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetCompanyContactsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Function Delete(bos As List(Of ContactBO)) As Response Implements IContactService.Delete
        Return Delete(bos.Select(Function(s) s.Id).ToList())
    End Function

End Class
