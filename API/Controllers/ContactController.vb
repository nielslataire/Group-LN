Imports System.Net
Imports System.Web.Http
Imports DAL

Namespace Controllers
    Public Class ContactController
        Inherits BaseApiController

        Public Function [Get]() As List(Of ContactModel)
            Dim uow As New UnitOfWork(False)
            Dim dao = uow.GetCompanyContactsDAO()

            Dim test = dao.GetNoTracking().OrderBy(Function(w) w.ContactNaam)
            Dim contacts As New List(Of ContactModel)
            For Each x In test
                Dim contact As New ContactModel
                contact.Id = x.ContactID
                If x.ContactNaam = "" Then
                    contact.Weergavenaam1 = x.ContactVoornaam
                ElseIf x.ContactVoornaam = "" Then
                    contact.Weergavenaam1 = x.ContactNaam
                Else
                    contact.Weergavenaam1 = x.ContactNaam & " " & x.ContactVoornaam
                End If
                contact.Weergavenaam2 = x.CompanyInfo.BedrijfsNaam
                contact.CompanyId = x.CompanyInfo.CompanyID
                contacts.Add(contact)
            Next
            Dim dao2 = uow.GetCompanyInfoDAO()
            Dim bedrijven = dao2.GetNoTracking().OrderBy(Function(w) w.BedrijfsNaam)
            For Each bedrijf In bedrijven
                Dim contact As New ContactModel
                contact.Id = 0
                contact.Weergavenaam1 = bedrijf.BedrijfsNaam
                contact.Weergavenaam2 = ""
                contact.CompanyId = bedrijf.CompanyID
                contacts.Add(contact)
            Next
            Return contacts
        End Function
        Public Function [Get](ByVal id As Integer) As ContactDetailModel
            Dim uow As New UnitOfWork(False)
            Dim dao = uow.GetCompanyContactsDAO()

            ''UPDATE
            'Dim dbobject = dao.GetById(id)
            'If (dbobject Is Not Nothing) Then
            'dbobject.Email = ""
            'uow.SaveChanges()
            'End If

            ''INSERT NOT SURE IF THIS WORKS
            'Dim dbobject = dao.GetNew()
            'dbobject.Email = "qmdkfjqmdlksjf"
            'uow.SaveChanges()

            Dim test = dao.GetNoTracking().Where(Function(w) w.ContactID = id).Single
            Dim contact As New ContactDetailModel
            contact.Id = test.ContactID
            contact.Naam = test.ContactNaam
            contact.Voornaam = test.ContactVoornaam
            contact.CompanyId = test.CompanyInfo.CompanyID
            contact.Bedrijfsnaam = test.CompanyInfo.BedrijfsNaam
            contact.Email = test.Email
            contact.GSM = test.GSM
            contact.Telefoon = test.Telefoon
            contact.Functie = test.Functie
            Return contact
        End Function
        Public Sub [Delete](ByVal id As Integer)
            Dim uow As New UnitOfWork(False)
            Dim dao = uow.GetCompanyContactsDAO()

            ''UPDATE
            Dim dbobject = dao.GetById(id)
            If Not dbobject Is Nothing Then
                dao.DeleteObject(dbobject)
                uow.SaveChanges()
            End If

            ''INSERT NOT SURE IF THIS WORKS
            'Dim dbobject = dao.GetNew()
            'dbobject.Email = "qmdkfjqmdlksjf"
            'uow.SaveChanges()

            ''Dim test = dao.GetNoTracking().Where(Function(w) w.ContactID = id).Single
            'Dim contact As New ContactDetailModel
            'contact.Id = test.ContactID
            'contact.Naam = test.ContactNaam
            'contact.Voornaam = test.ContactVoornaam
            'contact.CompanyId = test.CompanyInfo.CompanyID
            'contact.Bedrijfsnaam = test.CompanyInfo.BedrijfsNaam
            'contact.Email = test.Email
            'contact.GSM = test.GSM
            'contact.Telefoon = test.Telefoon
            'contact.Functie = test.Functie
            'Return contact
        End Sub
    End Class
End Namespace