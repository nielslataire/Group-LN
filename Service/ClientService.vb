Imports BO
Imports DAL
Imports Facade
Imports System.Linq.Expressions

Public Class ClientService
    Implements IClientService

    'ClIENT ACCOUNTS
    Public Function GetClientAccountById(id As Integer) As GetResponse(Of ClientAccountBO) Implements IClientService.GetClientAccountById
        Dim response As New GetResponse(Of ClientAccountBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()

        Dim _entity = dao.GetById(id)
        Dim clientaccount As New ClientAccountBO

        Dim err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount)
        If err = ErrorCode.Success Then
            response.Value = clientaccount
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetClientAccountByIds(ids As List(Of Integer)) As GetResponse(Of ClientAccountBO) Implements IClientService.GetClientAccountByIds
        Dim response As New GetResponse(Of ClientAccountBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) ids.Contains(m.Id))
        For Each _entity In entities
            Dim clientaccount As New ClientAccountBO
            Dim err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount)
            If err = ErrorCode.Success Then
                response.AddValue(clientaccount)
            Else
                response.AddError(err.ToString())
            End If
        Next

        Return response
    End Function
    Public Function GetClientAccountByIdWithUnits(id As Integer) As GetResponse(Of ClientAccountWithUnitsBO) Implements IClientService.GetClientAccountsByIdWithUnits
        Dim response As New GetResponse(Of ClientAccountWithUnitsBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()

        Dim _entity = dao.GetById(id)

        Dim clientaccount As New ClientAccountWithUnitsBO
        Dim err = ClientAccountTranslator.TranslateEntityToBO_WithUnits(_entity, clientaccount)
        If err = ErrorCode.Success Then
            response.AddValue(clientaccount)
        Else
            response.AddError(err.ToString())
        End If

        Return response
    End Function
    Public Function GetClientAccountsForSearchList(searchterm As String) As GetResponse(Of SelectBO) Implements IClientService.GetClientAccountsForSearchList
        Dim response As New GetResponse(Of SelectBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO
        Dim entitys = dao.GetNoTracking().Where(ClientAccountQuery.GetNameQuery(searchterm)).OrderBy(Function(m) m.Name).Select(Function(m) New SelectBO With {.id = m.Id, .text = m.Name & m.CompanyName, .extra = "Client"})
        response.Values = entitys.ToList()
        'For Each _entity In entitys
        '    response.AddValue(_entity.GetIdNameForSearch())
        'Next
        Return response
    End Function
    Public Function GetClientAccountNameById(id As Integer) As String Implements IClientService.GetClientAccountNameById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()
        Dim companyname As String = ""
        companyname = dao.GetById(id).CompanyName
        If companyname Is Nothing Or companyname = "" Then
            Return dao.GetById(id).Name
        Else
            Return companyname
        End If
    End Function
    Public Function GetClientAccountUnitsNameById(id As Integer) As String Implements IClientService.GetClientAccountUnitsNameById
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()
        Dim name As String = ""
        Dim i As Integer = 0
        Dim entities = dao.GetById(id).Units.Where(Function(m) m.UnitTypes.Selectable = True).OrderBy(Function(m) m.UnitTypes.GroupID)
        For Each _entity In entities
            If _entity.Id = entities.First.Id Then
                name = _entity.UnitTypes.Name & " " & _entity.Name
            Else
                name = name & " - " & _entity.UnitTypes.Name & " " & _entity.Name
            End If
        Next
        Return name
    End Function
    Public Function GetClientAccountsByProjectId(id As Integer) As GetResponse(Of ClientAccountBO) Implements IClientService.GetClientAccountsByProjectId
        Dim response As New GetResponse(Of ClientAccountBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()

        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.Any(Function(i) i.ProjectId = id))
        For Each _entity In entities
            Dim clientaccount As New ClientAccountBO
            Dim err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount)
            If err = ErrorCode.Success Then
                response.AddValue(clientaccount)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientAccountsByProjectIdWithUnits(id As Integer) As GetResponse(Of ClientAccountWithUnitsBO) Implements IClientService.GetClientAccountsByProjectIdWithUnits
        Dim response As New GetResponse(Of ClientAccountWithUnitsBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()

        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.Any(Function(i) i.ProjectId = id))
        For Each _entity In entities
            Dim clientaccount As New ClientAccountWithUnitsBO
            Dim err = ClientAccountTranslator.TranslateEntityToBO_WithUnits(_entity, clientaccount)
            If err = ErrorCode.Success Then
                response.AddValue(clientaccount)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientAccountsByProjectIdForSelect(projectid As Integer) As GetResponse(Of IdNameBO) Implements IClientService.GetClientAccountsByProjectIdForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO
        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.FirstOrDefault.ProjectId = projectid)
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetClientAccountsByProjectIdLast5(projectid As Integer) As GetResponse(Of IdNameBO) Implements IClientService.GetClientAccountsByProjectIdLast5
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO
        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.FirstOrDefault.ProjectId = projectid)
        For Each _entity In entities.OrderByDescending(Function(m) m.DateSalesAgreement).Take(5)
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function
    Public Function GetClientAccountsByUnitIds(Unitids As List(Of Integer)) As GetResponse(Of ClientAccountBO) Implements IClientService.GetClientAccountsByUnitIds
        Dim response As New GetResponse(Of ClientAccountBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) m.Units.Any(Function(i) Unitids.Contains(i.Id))).Distinct()
        For Each _entity In entities
            Dim clientaccount As New ClientAccountBO
            Dim err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount)
            If err = ErrorCode.Success Then
                response.AddValue(clientaccount)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientAccountsByDateDeedofSaleWarning() As GetResponse(Of ClientAccountBO) Implements IClientService.GetClientAccountsByDateDeedofSale
        Dim response As New GetResponse(Of ClientAccountBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientAccountDAO()
        Dim entities = dao.GetNoTracking().Where(Function(m) DateTime.Now() > System.Data.Entity.DbFunctions.AddMonths(m.DateSalesAgreement, 3) AndAlso m.DateDeedOfSale Is Nothing)

        For Each _entity In entities
            Dim clientaccount As New ClientAccountBO
            Dim err = ClientAccountTranslator.TranslateEntityToBO(_entity, clientaccount)
            If err = ErrorCode.Success Then
                response.AddValue(clientaccount)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function

    Public Function InsertUpdate(clientaccount As ClientAccountBO) As Response Implements IClientService.InsertUpdate
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(clientaccount.Name) AndAlso String.IsNullOrWhiteSpace(clientaccount.CompanyName)) Then
            response.AddError("name or companyname is mandatory")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ClientAccount = Nothing
        If (clientaccount.Id = 0) Then
            _entity = uow.GetClientAccountDAO().GetNew()
        Else
            _entity = uow.GetClientAccountDAO().GetById(clientaccount.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ClientAccountTranslator.TranslateBOToEntity(_entity, clientaccount, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("clientaccount not found")
        End If
        response.AddError(uow.SaveChanges())
        Dim msg As New Message
        msg.Type = MessageType.Value
        msg.Message = _entity.Id
        response.Messages.Add(msg)
        Return response
    End Function
    Public Function AddClientAccountToUnit(unitId As Integer, accountid As Integer) As Response Implements IClientService.AddClientAccountToUnit
        Dim response As New Response
        Dim uow As New UnitOfWork()
        Dim _entity As Units = Nothing
        _entity = uow.GetUnitsDAO().GetById(unitId)
        If (_entity IsNot Nothing) Then
            _entity.ClientAccountID = accountid
        Else
            response.AddError("unit not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function Delete(ids As List(Of Integer)) As Response Implements IClientService.Delete
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids

            uow.GetClientAccountDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Public Function Delete(bos As List(Of ClientAccountBO)) As Response Implements IClientService.Delete
        Return Delete(bos.Select(Function(s) s.Id).ToList())
    End Function

    'CLIENT CONTACTS
    Public Function GetClientContactById(id As Integer) As GetResponse(Of ClientContactBO) Implements IClientService.GetClientContactById
        Dim response As New GetResponse(Of ClientContactBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientContactsDAO()

        Dim _entity = dao.GetById(id)
        Dim clientcontact As New ClientContactBO

        Dim err = ClientContactTranslator.TranslateEntityToBO(_entity, clientcontact)
        If err = ErrorCode.Success Then
            response.Value = clientcontact
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetMaxOwnerPercentage(accountid As Integer, ownerid As Integer) As GetResponse(Of Decimal) Implements IClientService.GetMaxOwnerPercentage
        Dim response As New GetResponse(Of Decimal)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientContactsDAO()
        Dim max As Decimal = 99.99
        max = max - If(dao.GetNoTracking().Where(Function(m) m.ClientAccountID = accountid AndAlso m.IsCoOwner = True AndAlso m.Id <> ownerid).Sum(Function(m) m.CoOwnerPercentage), 0)
        response.Value = max
        Return response

    End Function
    Public Function InsertUpdateClientContact(contact As ClientContactBO) As Response Implements IClientService.InsertUpdateClientContact
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(contact.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ClientContacts = Nothing
        If (contact.Id = 0) Then
            _entity = uow.GetClientContactsDAO().GetNew()
        Else
            _entity = uow.GetClientContactsDAO().GetById(contact.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ClientContactTranslator.TranslateBOToEntity(_entity, contact, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("contact not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Public Function DeleteClientContact(ids As List(Of Integer)) As Response Implements IClientService.DeleteClientContact
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetClientContactsDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Public Function DeleteClientContact(bos As List(Of ClientContactBO)) As Response Implements IClientService.DeleteClientContact
        Return DeleteClientContact(bos.Select(Function(s) s.Id).ToList())
    End Function

    'CLIENT OWNER TYPE
    Function GetClientOwnerTypeById(id As Integer) As GetResponse(Of ClientOwnerTypeBO) Implements IClientService.GetClientOwnerTypeById
        Dim response As New GetResponse(Of ClientOwnerTypeBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientOwnerTypeDAO()

        Dim _entity = dao.GetById(id)
        Dim clientownertype As New ClientOwnerTypeBO

        Dim err = ClientOwnerTypeTranslator.TranslateEntityToBO(_entity, clientownertype)
        If err = ErrorCode.Success Then
            response.Value = clientownertype
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetOwnerTypes() As GetResponse(Of ClientOwnerTypeBO) Implements IClientService.GetOwnerTypes
        Dim response As New GetResponse(Of ClientOwnerTypeBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientOwnerTypeDAO
        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            Dim bo As New ClientOwnerTypeBO
            bo.Id = _entity.ID
            bo.Name = _entity.Name
            response.AddValue(bo)
        Next
        Return response
    End Function
    Public Function GetOwnerTypesForSelect() As GetResponse(Of IdNameBO) Implements IClientService.GetOwnerTypesForSelect
        Dim response As New GetResponse(Of IdNameBO)
        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientOwnerTypeDAO
        Dim entities = dao.GetNoTracking()
        For Each _entity In entities
            response.AddValue(_entity.GetIdName())
        Next
        Return response
    End Function

    Function InsertUpdateClientOwnerType(ownertype As ClientOwnerTypeBO) As Response Implements IClientService.InsertUpdateClientOwnerType
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(ownertype.Name)) Then
            response.AddError("name is mandatory")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ClientOwnerType = Nothing
        If (ownertype.Id = 0) Then
            _entity = uow.GetClientOwnerTypeDAO().GetNew()
        Else
            _entity = uow.GetClientOwnerTypeDAO().GetById(ownertype.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ClientOwnerTypeTranslator.TranslateBOToEntity(_entity, ownertype, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("ownertype not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function
    Function DeleteClientOwnerType(ids As List(Of Integer)) As Response Implements IClientService.DeleteClientOwnerType
        Dim response As New Response()
        Dim uow As New UnitOfWork()
        For Each id In ids
            uow.GetClientOwnerTypeDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())
        Return response
    End Function
    Function DeleteClientOwnerType(bos As List(Of ClientOwnerTypeBO)) As Response Implements IClientService.DeleteClientOwnerType
        Return DeleteClientOwnerType(bos.Select(Function(s) s.Id).ToList())
    End Function

    'CLIENT GIFTS
    Public Function GetClientGiftById(id As Integer) As GetResponse(Of ClientGiftBO) Implements IClientService.GetClientGiftById
        Dim response As New GetResponse(Of ClientGiftBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientGiftDAO()

        Dim _entity = dao.GetById(id)
        Dim clientgift As New ClientGiftBO

        Dim err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift)
        If err = ErrorCode.Success Then
            response.Value = clientgift
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetClientGiftByAccountId(id As Integer) As GetResponse(Of ClientGiftBO) Implements IClientService.GetClientGiftByAccountId
        Dim response As New GetResponse(Of ClientGiftBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientGiftDAO()

        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccountId = id)
        For Each _entity In entities
            Dim clientgift As New ClientGiftBO
            Dim err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift)
            If err = ErrorCode.Success Then
                response.AddValue(clientgift)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientsGifts(projectid As Integer) As GetResponse(Of ClientGiftWithAccountDetailsBO) Implements IClientService.GetClientsGifts
        Dim response As New GetResponse(Of ClientGiftWithAccountDetailsBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientGiftDAO()
        'And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
        'm.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid))
        For Each _entity In entities
            Dim clientgift As New ClientGiftWithAccountDetailsBO
            Dim err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift)
            If Not _entity.ClientAccount.Name Is Nothing Then
                Dim i As Salutation = _entity.ClientAccount.Salutation
                clientgift.AccountName = i.GetDisplayName() & " " & _entity.ClientAccount.Name

            Else
                clientgift.AccountName = _entity.ClientAccount.CompanyName

            End If
            For Each unit In _entity.ClientAccount.Units.Where(Function(m) m.TypeId = 1)
                clientgift.LivingUnit = clientgift.LivingUnit & " " & unit.UnitTypes.Name & " " & unit.Name
            Next
            If err = ErrorCode.Success Then
                response.AddValue(clientgift)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Public Function GetClientsGifts(projectid As Integer, activities As List(Of Integer)) As GetResponse(Of ClientGiftWithAccountDetailsBO) Implements IClientService.GetClientsGifts
        Dim response As New GetResponse(Of ClientGiftWithAccountDetailsBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientGiftDAO()
        'And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
        'm.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid) AndAlso m.Activity.Any(Function(s) activities.Contains(s.ActivityID)))
        For Each _entity In entities
            Dim clientgift As New ClientGiftWithAccountDetailsBO
            Dim err = ClientGiftTranslator.TranslateEntityToBO(_entity, clientgift)
            If Not _entity.ClientAccount.Name Is Nothing Then
                Dim i As Salutation = _entity.ClientAccount.Salutation
                clientgift.AccountName = i.GetDisplayName() & " " & _entity.ClientAccount.Name

            Else
                clientgift.AccountName = _entity.ClientAccount.CompanyName

            End If
            For Each unit In _entity.ClientAccount.Units.Where(Function(m) m.TypeId = 1)
                clientgift.LivingUnit = clientgift.LivingUnit & " " & unit.UnitTypes.Name & " " & unit.Name
            Next
            If err = ErrorCode.Success Then
                response.AddValue(clientgift)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Public Function InsertUpdateClientGift(gift As ClientGiftBO) As Response Implements IClientService.InsertUpdateClientGift
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(gift.Description)) Then
            response.AddError("description is mandatory")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ClientGift = Nothing
        If (gift.Id = 0) Then
            _entity = uow.GetClientGiftDAO().GetNew()
        Else
            _entity = uow.GetClientGiftDAO().GetById(gift.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ClientGiftTranslator.TranslateBOToEntity(_entity, gift, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("clientgift not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function

    Public Function DeleteClientGift(ids As List(Of Integer)) As Response Implements IClientService.DeleteClientGift
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetClientGiftDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Public Function DeleteClientGift(bos As List(Of ClientGiftBO)) As Response Implements IClientService.DeleteClientGift
        Return DeleteClientGift(bos.Select(Function(s) s.Id).ToList())
    End Function

    'ClIENT POINT OF ATTENTION (POA)
    Public Function GetClientPoaById(id As Integer) As GetResponse(Of ClientPoaBO) Implements IClientService.GetClientPoaById
        Dim response As New GetResponse(Of ClientPoaBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientPoaDAO()

        Dim _entity = dao.GetById(id)
        Dim clientpoa As New ClientPoaBO

        Dim err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa)
        If err = ErrorCode.Success Then
            response.Value = clientpoa
        Else
            response.AddError(err.ToString())
        End If
        Return response
    End Function
    Public Function GetClientPoaByAccountId(id As Integer) As GetResponse(Of ClientPoaBO) Implements IClientService.GetClientPoaByAccountId
        Dim response As New GetResponse(Of ClientPoaBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientPoaDAO()

        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccountId = id)
        For Each _entity In entities
            Dim clientpoa As New ClientPoaBO
            Dim err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa)
            If err = ErrorCode.Success Then
                response.AddValue(clientpoa)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response
    End Function
    Public Function GetClientsPoas(projectid As Integer) As GetResponse(Of ClientPoaWithAccountDetailsBO) Implements IClientService.GetClientsPoas
        Dim response As New GetResponse(Of ClientPoaWithAccountDetailsBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientPoaDAO()
        'And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
        'm.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid))
        For Each _entity In entities
            Dim clientpoa As New ClientPoaWithAccountDetailsBO
            Dim err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa)
            If Not _entity.ClientAccount.Name Is Nothing Then
                Dim i As Salutation = _entity.ClientAccount.Salutation
                clientpoa.AccountName = i.GetDisplayName() & " " & _entity.ClientAccount.Name

            Else
                clientpoa.AccountName = _entity.ClientAccount.CompanyName

            End If
            For Each unit In _entity.ClientAccount.Units.Where(Function(m) m.TypeId = 1)
                clientpoa.LivingUnit = clientpoa.LivingUnit & " " & unit.UnitTypes.Name & " " & unit.Name
            Next
            If err = ErrorCode.Success Then
                response.AddValue(clientpoa)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Public Function GetClientsPoas(projectid As Integer, activities As List(Of Integer)) As GetResponse(Of ClientPoaWithAccountDetailsBO) Implements IClientService.GetClientsPoas
        Dim response As New GetResponse(Of ClientPoaWithAccountDetailsBO)

        Dim uow As New UnitOfWork(False)
        Dim dao = uow.GetClientPoaDAO()
        'And m.Activity.Any(Function(s) s.ActivityID = activities.Any())
        'm.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid)
        Dim entities = dao.GetNoTracking().Where(Function(m) m.ClientAccount.Units.Any(Function(i) i.ProjectId = projectid) AndAlso m.Activity.Any(Function(s) activities.Contains(s.ActivityID)))
        For Each _entity In entities
            Dim clientpoa As New ClientPoaWithAccountDetailsBO
            Dim err = ClientPoaTranslator.TranslateEntityToBO(_entity, clientpoa)
            If Not _entity.ClientAccount.Name Is Nothing Then
                Dim i As Salutation = _entity.ClientAccount.Salutation
                clientpoa.AccountName = i.GetDisplayName() & " " & _entity.ClientAccount.Name

            Else
                clientpoa.AccountName = _entity.ClientAccount.CompanyName

            End If
            For Each unit In _entity.ClientAccount.Units.Where(Function(m) m.TypeId = 1)
                clientpoa.LivingUnit = clientpoa.LivingUnit & " " & unit.UnitTypes.Name & " " & unit.Name
            Next
            If err = ErrorCode.Success Then
                response.AddValue(clientpoa)
            Else
                response.AddError(err.ToString())
            End If
        Next
        Return response

    End Function
    Public Function InsertUpdateClientPoa(poa As ClientPoaBO) As Response Implements IClientService.InsertUpdateClientPoa
        Dim response As New Response
        If (String.IsNullOrWhiteSpace(poa.Description)) Then
            response.AddError("description is mandatory")
        End If
        If (Not response.Success) Then Return response
        Dim uow As New UnitOfWork()
        Dim _entity As ClientPOA = Nothing
        If (poa.Id = 0) Then
            _entity = uow.GetClientPoaDAO().GetNew()
        Else
            _entity = uow.GetClientPoaDAO().GetById(poa.Id)
        End If
        If (_entity IsNot Nothing) Then
            Dim err = ClientPoaTranslator.TranslateBOToEntity(_entity, poa, uow)
            If (err <> ErrorCode.Success) Then
                response.AddError(err.ToString())
            End If
        Else
            response.AddError("clientpoa not found")
        End If
        response.AddError(uow.SaveChanges())
        Return response
    End Function

    Public Function DeleteClientPoa(ids As List(Of Integer)) As Response Implements IClientService.DeleteClientPoa
        Dim response As New Response()
        Dim uow As New UnitOfWork()

        For Each id In ids
            uow.GetClientPoaDAO().DeleteObject(id)
        Next
        response.Messages.AddRange(uow.SaveChanges())

        Return response
    End Function
    Public Function DeleteClientPoa(bos As List(Of ClientPoaBO)) As Response Implements IClientService.DeleteClientPoa
        Return DeleteClientPoa(bos.Select(Function(s) s.Id).ToList())
    End Function

End Class
