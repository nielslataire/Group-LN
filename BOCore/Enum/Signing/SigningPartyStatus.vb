Imports System.ComponentModel.DataAnnotations

''' <summary>Status van één ondertekenaar binnen een dossier (<c>SigningParty</c>).</summary>
Public Enum SigningPartyStatus As Integer

    ''' <summary>Nog niet uitgenodigd (bv. volgende in een ORDERED-reeks).</summary>
    <Display(Name:="Nog niet uitgenodigd")>
    Pending = 0

    <Display(Name:="Uitnodiging verzonden")>
    Invited = 1

    <Display(Name:="Document bekeken")>
    Opened = 2

    <Display(Name:="Verificatie uitgevoerd")>
    Verified = 3

    <Display(Name:="Ondertekend")>
    Signed = 4

    <Display(Name:="Geweigerd")>
    Declined = 5

    <Display(Name:="Verlopen")>
    Expired = 6

    ''' <summary>Link ingetrokken (dossier geannuleerd, of ANY-regel al vervuld door iemand anders).</summary>
    <Display(Name:="Ingetrokken")>
    Revoked = 7

End Enum
