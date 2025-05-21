Imports DAL
Public Class ModelFactory

    Public Function Create(contact As CompanyContacts) As ContactModel
        'Return New ContactModel() With { _
        '    .Id = contact.ContactID, _
        '    .Naam = contact.ContactNaam, _
        '    .Voornaam = contact.ContactVoornaam _
        '}
    End Function

End Class
