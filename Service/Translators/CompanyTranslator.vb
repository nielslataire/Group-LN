Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class CompanyTranslator

    Friend Shared Function TranslateEntityToBO(_entity As CompanyInfo, bo As CompanyBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Bedrijfsnaam = _entity.BedrijfsNaam
        bo.Busnummer = _entity.Busnummer
        bo.CompanyId = _entity.CompanyID
        bo.Email = _entity.Email
        bo.GSM = _entity.GSM
        bo.Huisnummer = _entity.Huisnummer
        bo.Straat = _entity.Straat
        bo.Telefoon1 = _entity.Telefoon1
        bo.Telefoon2 = _entity.Telefoon2
        bo.Toevoeging = _entity.Toevoeging
        bo.Opmerking = _entity.Opmerkingen
        bo.URL = _entity.WEBURL
        bo.OndNr = _entity.Ondernemingsnummer
        If (_entity.PostalCode IsNot Nothing) Then
            bo.Postcode.Postcode = _entity.PostalCode.Postcode
            bo.Postcode.Gemeente = _entity.PostalCode.Gemeente
            bo.Postcode.PostcodeId = _entity.PostalCode.PostcodeID
            If _entity.PostalCode.Country IsNot Nothing Then
                bo.Postcode.Country.Name = _entity.PostalCode.Country.LandNaam
                bo.Postcode.Country.CountryID = _entity.PostalCode.Country.ID
                bo.Postcode.Country.ISOCode = _entity.PostalCode.Country.LandISOCode
            End If
            If _entity.PostalCode.Provincie IsNot Nothing Then
                bo.Postcode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName
                bo.Postcode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieID
            End If
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
            If (x.CompanyDepartments IsNot Nothing) Then
                contact.Department = x.CompanyDepartments.GetIdName()
            End If
            bo.Contacts.Add(contact)
        Next
        For Each x In _entity.Activity
            Dim activity As New ActivityBO
            activity.ID = x.ActivityID
            activity.Name = x.Omschrijving
            'activity.Group.Name = x.ActivityGroup.Name
            'activity.Group.ID = x.ActivityGroup.GroupID
            'activity.Group.Lot = x.ActivityGroup.Lot
            bo.Activities.Add(activity)

        Next
        For Each x In _entity.CompanyDepartments
            Dim department As New DepartmentBO
            department.ID = x.DepartmentId
            department.Name = x.Naam
            department.Street = x.Straat
            department.Phone = x.Telefoon
            department.CellPhone = x.GSM
            department.Email = x.Email
            department.Housenumber = x.Huisnummer
            department.Postalcode.PostcodeId = x.PostcodeId
            department.Postalcode.Postcode = x.PostalCode.Postcode
            department.Postalcode.Country.Name = x.PostalCode.Country.LandNaam
            department.Postalcode.Gemeente = x.PostalCode.Gemeente
            bo.Departments.Add(department)

        Next

        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As CompanyInfo, bo As CompanyBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.BedrijfsNaam = bo.Bedrijfsnaam
        _entity.Busnummer = bo.Busnummer
        '_entity.CompanyID = bo.CompanyId
        _entity.Email = bo.Email
        If Not bo.GSM Is Nothing Then _entity.GSM = Regex.Replace(bo.GSM, "[^0-9]", "")
        _entity.Huisnummer = bo.Huisnummer
        _entity.Straat = bo.Straat
        If Not bo.Telefoon1 Is Nothing Then _entity.Telefoon1 = Regex.Replace(bo.Telefoon1, "[^0-9]", "")
        If Not bo.Telefoon2 Is Nothing Then _entity.Telefoon2 = Regex.Replace(bo.Telefoon2, "[^0-9]", "")
        _entity.Toevoeging = bo.Toevoeging
        _entity.Opmerkingen = bo.Opmerking
        _entity.WEBURL = bo.URL
        If Not bo.OndNr Is Nothing Then _entity.Ondernemingsnummer = Regex.Replace(bo.OndNr, "[^0-9]", "")
        If Not bo.RegNr Is Nothing Then _entity.RegistratieNr = Regex.Replace(bo.RegNr, "[^0-9]", "")

        If (bo.Postcode IsNot Nothing) Then
            _entity.PostCodeID = bo.Postcode.PostcodeId
        End If
        Dim err = HandleContacts(_entity, bo.Contacts)
        If (err <> ErrorCode.Success) Then Return err
        err = HandleDepartmens(_entity, bo.Departments)
        If (err <> ErrorCode.Success) Then Return err
        err = HandleActivities(_entity, bo.Activities, uow)
        If (err <> ErrorCode.Success) Then Return err

        Return ErrorCode.Success
    End Function

    Private Shared Function HandleContacts(_entity As CompanyInfo, contacts As List(Of ContactBO)) As ErrorCode
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
                If (x.Department IsNot Nothing And x.Department.ID > 0) Then
                    contact.DepartmentID = x.Department.ID
                End If

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
                    If (x.Department IsNot Nothing) Then
                        contact.DepartmentID = x.Department.ID
                    End If
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
    Private Shared Function HandleDepartmens(_entity As CompanyInfo, departments As List(Of DepartmentBO)) As ErrorCode
        If (departments.Count = 0) Then Return ErrorCode.Success
        For Each x In departments
            If (x.ID = 0) Then
                'insert
                Dim department As New CompanyDepartments
                Dim err = DepartmentTranslator.TranslateBOToEntity(department, x)
                If (err <> ErrorCode.Success) Then Return err
                _entity.CompanyDepartments.Add(department)
            Else
                'update
                Dim department = _entity.CompanyDepartments.FirstOrDefault(Function(f) f.DepartmentId = x.ID)
                If (department IsNot Nothing) Then
                    Dim err = DepartmentTranslator.TranslateBOToEntity(department, x)
                    If (err <> ErrorCode.Success) Then Return err
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of CompanyDepartments)
        For Each x In _entity.CompanyDepartments
            If (Not departments.Any(Function(f) f.ID = x.DepartmentId)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.CompanyDepartments.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleActivities(_entity As CompanyInfo, activities As List(Of ActivityBO), uow As UnitOfWork) As ErrorCode
        If (activities.Count = 0) Then Return ErrorCode.Success
        For Each x In activities
            If (x.ID = 0) Then
                'should never happen
            Else
                'add the activity to the company
                If (Not _entity.Activity.Any(Function(m) m.ActivityID = x.ID)) Then
                    Dim act = uow.GetActivityDAO().GetById(x.ID)
                    _entity.Activity.Add(act)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of Activity)
        For Each x In _entity.Activity
            If (Not activities.Any(Function(f) f.ID = x.ActivityID)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.Activity.Remove(x)
        Next
        Return ErrorCode.Success
    End Function

End Class
