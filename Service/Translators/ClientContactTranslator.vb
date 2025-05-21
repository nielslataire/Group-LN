Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ClientContactTranslator

    Friend Shared Function TranslateEntityToBO(_entity As ClientContacts, bo As ClientContactBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.AccountId = _entity.ClientAccountID
        bo.Name = _entity.Name
        bo.Street = _entity.Street
        bo.Housenumber = _entity.Housenumber
        bo.Busnumber = _entity.Busnumber
        bo.Email = _entity.Email
        bo.IsCoOwner = _entity.IsCoOwner
        bo.Phone = _entity.Phone
        bo.Cellphone = _entity.Cellphone
        bo.Firstname = _entity.Forename
        bo.Salutation = _entity.Salutation
        bo.VATnumber = _entity.VATnumber
        bo.CompanyName = _entity.CompanyName
        bo.InvoiceAddress = _entity.InvoiceAddress
        bo.InvoiceStreet = _entity.InvoiceStreet
        bo.InvoiceHousenumber = _entity.InvoiceHousenumber
        bo.InvoiceBusnumber = _entity.InvoiceBusnumber


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
        If Not _entity.CoOwnerPercentage Is Nothing Then bo.CoOwnerPercentage = _entity.CoOwnerPercentage
        If (_entity.ClientOwnerType IsNot Nothing) Then
            Dim err = ClientOwnerTypeTranslator.TranslateEntityToBO(_entity.ClientOwnerType, bo.CoOwnerType)
            If err <> ErrorCode.Success Then Return err
        End If

        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As ClientContacts, bo As ClientContactBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        _entity.ClientAccountID = bo.AccountId
        _entity.Name = bo.Name
        _entity.Street = bo.Street
        _entity.Housenumber = bo.Housenumber
        _entity.Busnumber = bo.Busnumber
        _entity.Email = bo.Email
        _entity.IsCoOwner = bo.IsCoOwner
        If Not bo.Cellphone Is Nothing Then _entity.Cellphone = Regex.Replace(bo.Cellphone, "[^0-9]", "")
        If Not bo.Phone Is Nothing Then _entity.Phone = Regex.Replace(bo.Phone, "[^0-9]", "")
        _entity.Salutation = bo.Salutation
        _entity.Forename = bo.Firstname
        _entity.CompanyName = bo.CompanyName
        _entity.VATnumber = bo.VATnumber
        _entity.InvoiceAddress = bo.InvoiceAddress
        _entity.InvoiceStreet = bo.InvoiceStreet
        _entity.InvoiceHousenumber = bo.InvoiceHousenumber
        _entity.InvoiceBusnumber = bo.InvoiceBusnumber

        If (bo.Postalcode IsNot Nothing) Then
            If bo.Postalcode.PostcodeId <> 0 Then
                _entity.PostalCodeID = bo.Postalcode.PostcodeId
            End If
        End If
        If (bo.InvoicePostalcode IsNot Nothing) Then
            If bo.InvoicePostalcode.PostcodeId <> 0 Then
                _entity.InvoicePostalCodeID = bo.InvoicePostalcode.PostcodeId
            End If
        End If
        _entity.CoOwnerPercentage = bo.CoOwnerPercentage
        If (bo.CoOwnerType IsNot Nothing) Then
            If bo.CoOwnerType.Id <> 0 Then
                _entity.CoOwnerTypeID = bo.CoOwnerType.Id
            End If
        End If

        Return ErrorCode.Success
    End Function


End Class
