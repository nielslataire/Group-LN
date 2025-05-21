Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ClientAccountTranslator

    Friend Shared Function TranslateEntityToBO(_entity As ClientAccount, bo As ClientAccountBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Name = _entity.Name
        bo.Salutation = _entity.Salutation
        bo.Street = _entity.Street
        bo.Housenumber = _entity.Housenumber
        bo.Busnumber = _entity.Busnumber
        If Not _entity.DateDeedOfSale Is Nothing Then bo.DateDeedOfSale = _entity.DateDeedOfSale
        If Not _entity.DeedOfSaleExpDate Is Nothing Then bo.DateDeedOfSaleExpDate = _entity.DeedOfSaleExpDate
        bo.DateSalesAgreement = _entity.DateSalesAgreement
        bo.DeliveryDate = _entity.DeliveryDate
        bo.DeliveryDoc = _entity.DeliveryDoc
        bo.VATnumber = _entity.VATnumber
        bo.CompanyName = _entity.CompanyName
        bo.StartDateConstruction = _entity.StartDateConstruction
        bo.BankAccountNumber = _entity.BankAccountNumber
        bo.InvoiceAddress = _entity.InvoiceAddress
        bo.InvoiceStreet = _entity.InvoiceStreet
        bo.InvoiceHousenumber = _entity.InvoiceHousenumber
        bo.InvoiceBusnumber = _entity.InvoiceBusnumber
        bo.InvoiceExtra = _entity.InvoiceExtra

        If Not _entity.ExecutionDays Is Nothing Then bo.ExecutionDays = _entity.ExecutionDays

        If Not _entity.OwnerPercentage Is Nothing Then bo.OwnerPercentage = _entity.OwnerPercentage
        If (_entity.ClientOwnerType IsNot Nothing) Then
            Dim err = ClientOwnerTypeTranslator.TranslateEntityToBO(_entity.ClientOwnerType, bo.OwnerType)
            If err <> ErrorCode.Success Then Return err
        End If
        If (_entity.PostalCode IsNot Nothing) Then
            bo.Postalcode.Postcode = _entity.PostalCode.Postcode
            bo.Postalcode.Gemeente = _entity.PostalCode.Gemeente
            bo.Postalcode.PostcodeId = _entity.PostalCode.PostcodeID
            If _entity.PostalCode.Country IsNot Nothing Then
                bo.Postalcode.Country.Name = _entity.PostalCode.Country.LandNaam
                bo.Postalcode.Country.CountryID = _entity.PostalCode.Country.ID
                bo.Postalcode.Country.ISOCode = _entity.PostalCode.Country.LandISOCode
            End If
            If _entity.PostalCode.Provincie IsNot Nothing Then
                bo.Postalcode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName
                bo.Postalcode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieID
            End If
        End If
        If (_entity.InvoicePostalCode IsNot Nothing) Then
            bo.InvoicePostalcode.Postcode = _entity.InvoicePostalCode.Postcode
            bo.InvoicePostalcode.Gemeente = _entity.InvoicePostalCode.Gemeente
            bo.InvoicePostalcode.PostcodeId = _entity.InvoicePostalCode.PostcodeID
            If _entity.InvoicePostalCode.Country IsNot Nothing Then
                bo.InvoicePostalcode.Country.Name = _entity.InvoicePostalCode.Country.LandNaam
                bo.InvoicePostalcode.Country.CountryID = _entity.InvoicePostalCode.Country.ID
                bo.InvoicePostalcode.Country.ISOCode = _entity.InvoicePostalCode.Country.LandISOCode
            End If
            If _entity.InvoicePostalCode.Provincie IsNot Nothing Then
                bo.InvoicePostalcode.Provincie.Name = _entity.InvoicePostalCode.Provincie.ProvincieName
                bo.InvoicePostalcode.Provincie.ProvincieId = _entity.InvoicePostalCode.Provincie.ProvincieID
            End If
        End If
        If _entity.ClientContacts IsNot Nothing Then
            For Each item In _entity.ClientContacts
                If item.IsCoOwner = False Then
                    Dim contact As New ClientContactBO
                    Dim err = ClientContactTranslator.TranslateEntityToBO(item, contact)
                    If err <> ErrorCode.Success Then Return err
                    bo.Contacts.Add(contact)
                Else
                    Dim coowner As New ClientContactBO
                    Dim err = ClientContactTranslator.TranslateEntityToBO(item, coowner)
                    If err <> ErrorCode.Success Then Return err
                    bo.CoOwners.Add(coowner)
                End If
            Next
        End If

        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateEntityToBO_WithUnits(_entity As ClientAccount, bo As ClientAccountWithUnitsBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        Dim err = TranslateEntityToBO(_entity, bo.Client)
        If err <> ErrorCode.Success Then Return err

        For Each item In _entity.Units
            Dim unit As New UnitBO
            err = UnitTranslator.TranslateEntityToBO(item, unit)
            If err <> ErrorCode.Success Then Return err
            bo.Units.Add(unit)
        Next

        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ClientAccount, bo As ClientAccountBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull


        _entity.Name = bo.Name
        _entity.Salutation = bo.Salutation
        _entity.Street = bo.Street
        _entity.Housenumber = bo.Housenumber
        _entity.Busnumber = bo.Busnumber
        _entity.DateDeedOfSale = bo.DateDeedOfSale
        _entity.DeedOfSaleExpDate = bo.DateDeedOfSaleExpDate
        _entity.DateSalesAgreement = bo.DateSalesAgreement
        _entity.DeliveryDate = bo.DeliveryDate
        _entity.DeliveryDoc = bo.DeliveryDoc
        _entity.VATnumber = bo.VATnumber
        _entity.CompanyName = bo.CompanyName
        _entity.ExecutionDays = bo.ExecutionDays
        _entity.StartDateConstruction = bo.StartDateConstruction
        _entity.BankAccountNumber = bo.BankAccountNumber
        _entity.InvoiceAddress = bo.InvoiceAddress
        _entity.InvoiceStreet = bo.InvoiceStreet
        _entity.InvoiceHousenumber = bo.InvoiceHousenumber
        _entity.InvoiceBusnumber = bo.InvoiceBusnumber
        _entity.InvoiceExtra = bo.InvoiceExtra


        If (bo.OwnerType IsNot Nothing Or bo.OwnerType.Id <> 0) Then
            _entity.OwnerTypeId = bo.OwnerType.Id
        End If
        _entity.OwnerPercentage = bo.OwnerPercentage


        If (bo.Postalcode IsNot Nothing) Then
            _entity.PostalCodeID = bo.Postalcode.PostcodeId
        End If
        If (bo.InvoicePostalcode IsNot Nothing) AndAlso bo.InvoicePostalcode.PostcodeId <> 0 Then
            _entity.InvoicePostalCodeId = bo.InvoicePostalcode.PostcodeId
        End If
        Dim err = HandleContacts(_entity, bo.Contacts, bo.CoOwners, uow)
        If (err <> ErrorCode.Success) Then Return err
        err = HandleCoOwners(_entity, bo.CoOwners, bo.Contacts, uow)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Private Shared Function HandleContacts(_entity As ClientAccount, contacts As List(Of ClientContactBO), coowners As List(Of ClientContactBO), uow As UnitOfWork) As ErrorCode
        If (contacts Is Nothing) Then Return ErrorCode.Success
        If (contacts.Count = 0) Then Return ErrorCode.Success
        For Each x In contacts
            If (x.Id = 0) Then
                'insert
                Dim contact As New ClientContacts
                Dim err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow)
                contact.IsCoOwner = False
                contact.CoOwnerTypeID = Nothing
                contact.ClientOwnerType = Nothing
                contact.PostalCode = Nothing
                contact.PostalCodeID = Nothing
                contact.InvoicePostalCode = Nothing
                contact.InvoicePostalCodeID = Nothing
                If err <> ErrorCode.Success Then Return err

                _entity.ClientContacts.Add(contact)
            Else
                'update
                Dim contact = _entity.ClientContacts.FirstOrDefault(Function(f) f.Id = x.Id)
                If (contact IsNot Nothing) Then
                    Dim err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow)
                    contact.IsCoOwner = False
                    contact.CoOwnerTypeID = Nothing
                    contact.ClientOwnerType = Nothing
                    contact.PostalCode = Nothing
                    contact.PostalCodeID = Nothing
                    contact.InvoicePostalCode = Nothing
                    contact.InvoicePostalCodeID = Nothing
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of ClientContacts)
        For Each x In _entity.ClientContacts
            If (Not contacts.Any(Function(f) f.Id = x.Id) AndAlso Not coowners.Any(Function(m) m.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.ClientContacts.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleCoOwners(_entity As ClientAccount, coowners As List(Of ClientContactBO), contacts As List(Of ClientContactBO), uow As UnitOfWork) As ErrorCode
        If (coowners Is Nothing) Then Return ErrorCode.Success
        If (coowners.Count = 0) Then Return ErrorCode.Success
        For Each x In coowners
            If (x.Id = 0) Then
                'insert
                Dim contact As New ClientContacts
                Dim err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow)
                contact.IsCoOwner = True
                If err <> ErrorCode.Success Then Return err

                _entity.ClientContacts.Add(contact)
            Else
                'update
                Dim contact = _entity.ClientContacts.FirstOrDefault(Function(f) f.Id = x.Id)
                If (contact IsNot Nothing) Then
                    Dim err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow)
                    contact.IsCoOwner = True
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of ClientContacts)
        For Each x In _entity.ClientContacts
            If (Not coowners.Any(Function(f) f.Id = x.Id) AndAlso Not contacts.Any(Function(m) m.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.ClientContacts.Remove(x)
        Next
        Return ErrorCode.Success
    End Function


End Class
