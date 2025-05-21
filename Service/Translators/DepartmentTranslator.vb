Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class DepartmentTranslator
    Public Shared Function TranslateEntityToBO(_entity As CompanyDepartments, bo As DepartmentBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.ID = _entity.DepartmentId
        bo.Name = _entity.Naam
        bo.Busnumber = _entity.Bus
        bo.CellPhone = _entity.GSM
        bo.Email = _entity.Email
        bo.Housenumber = _entity.Huisnummer
        bo.Phone = _entity.Telefoon
        bo.Street = _entity.Straat
        If _entity.PostalCode IsNot Nothing Then
            Dim err = PostalcodeTranslator.TranslateEntityToBO(_entity.PostalCode, bo.Postalcode)
            If err <> ErrorCode.Success Then Return err
        End If

        If (_entity.CompanyInfo IsNot Nothing) Then
            bo.Company = _entity.CompanyInfo.GetIdName()
        End If
        For Each x In _entity.CompanyContacts
            Dim contact As New ContactBO
            contact.Id = x.ContactID
            If x.ContactNaam = "" Then
                contact.Weergavenaam1 = x.ContactVoornaam
            ElseIf x.ContactVoornaam = "" Then
                contact.Weergavenaam1 = x.ContactNaam
            Else
                contact.Weergavenaam1 = x.ContactNaam & " " & x.ContactVoornaam
            End If
            contact.Weergavenaam2 = x.CompanyInfo.BedrijfsNaam
            contact.Salutation = x.Aanspreking

            contact.JobFunction = x.Functie
            contact.Phone = x.Telefoon
            contact.CellPhone = x.GSM
            contact.Email = x.Email
            contact.Firstname = x.ContactVoornaam
            contact.Name = x.ContactNaam
            bo.Contacts.Add(contact)
        Next

        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As CompanyDepartments, bo As DepartmentBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Naam = bo.Name
        _entity.Bus = bo.Busnumber
        If Not bo.CellPhone Is Nothing Then _entity.GSM = Regex.Replace(bo.CellPhone, "[^0-9]", "")
        _entity.Email = bo.Email
        _entity.Huisnummer = bo.Housenumber
        If Not bo.Phone Is Nothing Then _entity.Telefoon = Regex.Replace(bo.Phone, "[^0-9]", "")
        _entity.Straat = bo.Street
        If bo.Postalcode IsNot Nothing AndAlso bo.Postalcode.PostcodeId <> 0 Then
            _entity.PostcodeId = bo.Postalcode.PostcodeId
        Else
            _entity.PostcodeId = Nothing
        End If
        If bo.Company IsNot Nothing AndAlso bo.Company.ID <> 0 Then
            _entity.CompanyId = bo.Company.ID
        Else
            _entity.CompanyId = Nothing
        End If

        Return ErrorCode.Success
        Dim err = HandleContacts(_entity, bo.Contacts)
        If (err <> ErrorCode.Success) Then Return err
    End Function
    Private Shared Function HandleContacts(_entity As CompanyDepartments, contacts As List(Of ContactBO)) As ErrorCode
        If (contacts.Count = 0) Then Return ErrorCode.Success
        For Each x In contacts
            If (x.Id = 0) Then
                'insert
                Dim contact As New CompanyContacts
                contact.ContactNaam = x.Name
                contact.Aanspreking = x.Salutation
                contact.ContactVoornaam = x.Firstname
                contact.Functie = x.JobFunction
                contact.Telefoon = x.Phone
                contact.GSM = x.CellPhone
                contact.Email = x.Email
                contact.CompanyID = x.Company.ID
                _entity.CompanyContacts.Add(contact)
            Else
                'update
                Dim contact = _entity.CompanyContacts.FirstOrDefault(Function(f) f.ContactID = x.Id)
                If (contact IsNot Nothing) Then
                    contact.ContactNaam = x.Name
                    contact.Aanspreking = x.Salutation
                    contact.ContactVoornaam = x.Firstname
                    contact.Functie = x.JobFunction
                    contact.Telefoon = x.Phone
                    contact.GSM = x.CellPhone
                    contact.Email = x.Email
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of CompanyContacts)
        For Each x In _entity.CompanyContacts
            If (Not contacts.Any(Function(f) f.Id = x.ContactID)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.CompanyContacts.Remove(x)
        Next
        Return ErrorCode.Success
    End Function

End Class
