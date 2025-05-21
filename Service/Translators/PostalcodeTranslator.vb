Imports DAL
Imports BO
Public Class PostalcodeTranslator

    Friend Shared Function TranslateEntityToBO(_entity As PostalCode, bo As PostalCodeBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.PostcodeId = _entity.PostcodeID
        bo.Postcode = _entity.Postcode
        bo.Gemeente = _entity.Gemeente
        If (_entity.Country IsNot Nothing) Then
            bo.Country.CountryID = _entity.Country.ID
            bo.Country.Name = _entity.Country.LandNaam
            bo.Country.ISOCode = _entity.Country.LandISOCode
        End If
        If (_entity.Provincie IsNot Nothing) Then
            bo.Provincie.Name = _entity.Provincie.ProvincieName
            bo.Provincie.ProvincieId = _entity.Provincie.ProvincieID
        End If
        Return ErrorCode.Success
    End Function

    'Public Shared Function TranslateBOToEntity(_entity As CompanyInfo, bo As CompanyBO) As ErrorCode
    '    If _entity Is Nothing Then Return ErrorCode.EntityNull
    '    If bo Is Nothing Then Return ErrorCode.BoNull
    '    _entity.BedrijfsNaam = bo.Bedrijfsnaam
    '    _entity.Busnummer = bo.Busnummer
    '    _entity.CompanyID = bo.CompanyId
    '    _entity.Email = bo.Email
    '    _entity.GSM = bo.GSM
    '    _entity.Huisnummer = bo.Huisnummer
    '    _entity.Straat = bo.Straat
    '    _entity.Telefoon1 = bo.Telefoon1
    '    _entity.Telefoon2 = bo.Telefoon2
    '    _entity.Toevoeging = bo.Toevoeging
    '    If (bo.Postcode IsNot Nothing) Then
    '        _entity.PostCodeID = bo.Postcode.PostcodeId
    '    End If

    '    For Each x In bo.Contacts
    '        If (x.Id = 0) Then
    '            'insert
    '            Dim contact As New CompanyContacts
    '            contact.ContactNaam = x.Weergavenaam1
    '            _entity.CompanyContacts.Add(contact)
    '        Else
    '            'update
    '            Dim contact = _entity.CompanyContacts.FirstOrDefault(Function(f) f.ContactID = x.Id)
    '            If (contact IsNot Nothing) Then
    '                contact.ContactNaam = x.Weergavenaam1
    '            End If
    '        End If
    '    Next
    '    'delete
    '    Dim delList As New List(Of CompanyContacts)
    '    For Each x In _entity.CompanyContacts
    '        If (Not bo.Contacts.Any(Function(f) f.Id = x.ContactID)) Then
    '            delList.Add(x)
    '        End If
    '    Next
    '    For Each x In delList
    '        _entity.CompanyContacts.Remove(x)
    '    Next

    '    Return ErrorCode.Success
    'End Function
End Class
