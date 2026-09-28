Imports System.ComponentModel.DataAnnotations

''' <summary>Facturatiewijze van een klantenaccount (migratie 057, design-handoff 23a/23e) — enkel
''' bewaard, stuurt de facturatie-generatie nog niet aan.</summary>
Public Enum ClientInvoicingMode As Integer

    ''' <summary>Eén factuur voor het hele account; de aandelen blijven gelden voor akte en portaal.</summary>
    <Display(Name:="Gemeenschappelijk — één factuur")>
    Joint = 0

    ''' <summary>Elke eigenaar krijgt een eigen factuur op zijn eigen adres, volgens aandeel.</summary>
    <Display(Name:="Per eigenaar volgens aandeel")>
    PerOwner = 1

End Enum
