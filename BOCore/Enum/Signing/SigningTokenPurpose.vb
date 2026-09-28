Imports System.ComponentModel.DataAnnotations

''' <summary>Waarvoor een persoonlijke link (<c>SigningAccessToken</c>) werd uitgegeven.</summary>
Public Enum SigningTokenPurpose As Integer

    <Display(Name:="Uitnodiging")>
    Invite = 0

    <Display(Name:="Herinnering")>
    Reminder = 1

    ''' <summary>Nieuwe link op vraag, nadat de vorige links ingetrokken werden.</summary>
    <Display(Name:="Nieuwe link")>
    Regenerated = 2

    ''' <summary>Alleen downloaden van het ondertekende document en het auditrapport; kan nooit ondertekenen.</summary>
    <Display(Name:="Download")>
    Download = 3

End Enum
