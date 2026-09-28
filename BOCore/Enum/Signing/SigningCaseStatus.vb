Imports System.ComponentModel.DataAnnotations

''' <summary>Status van een ondertekeningsdossier (<c>SigningCase</c>). Zie ONDERTEKENEN_VOORSTEL.md §3.2.</summary>
Public Enum SigningCaseStatus As Integer

    ''' <summary>Aangemaakt, document en partijen vastgelegd, nog niet aangeboden.</summary>
    <Display(Name:="Concept")>
    Draft = 0

    ''' <summary>Aangeboden: links uitgegeven, wacht op ondertekening.</summary>
    <Display(Name:="Wacht op ondertekening")>
    Open = 1

    <Display(Name:="Ondertekend")>
    Completed = 2

    <Display(Name:="Geweigerd")>
    Declined = 3

    <Display(Name:="Verlopen")>
    Expired = 4

    <Display(Name:="Geannuleerd")>
    Cancelled = 5

End Enum
