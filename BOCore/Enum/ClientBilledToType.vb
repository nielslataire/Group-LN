Imports System.ComponentModel.DataAnnotations

''' <summary>"Op naam van" — wie het klantenaccount als partij op de factuur staat (migratie 057,
''' design-handoff 23a).</summary>
Public Enum ClientBilledToType As Integer

    <Display(Name:="Alle eigenaars")>
    AllOwners = 0

    ''' <summary>Zie ClientAccountBO.BilledToClientContactId; leeg = eigenaar 1 (het account zelf).</summary>
    <Display(Name:="Eén eigenaar")>
    SpecificOwner = 1

    <Display(Name:="Bedrijf")>
    Company = 2

End Enum
