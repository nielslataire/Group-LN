Imports System.ComponentModel.DataAnnotations

''' <summary>Wie een gebeurtenis in de audit trail veroorzaakte (<c>SigningEvent.ActorType</c>).</summary>
Public Enum SigningActorType As Integer

    ''' <summary>De applicatie zelf (achtergrondjob, automatische overgang).</summary>
    <Display(Name:="Systeem")>
    System = 0

    ''' <summary>Een ingelogde CPM-gebruiker (<c>ActorUserId</c> gevuld).</summary>
    <Display(Name:="Interne gebruiker")>
    Internal = 1

    ''' <summary>De ondertekenaar, via de persoonlijke link (<c>SigningPartyId</c> gevuld).</summary>
    <Display(Name:="Ondertekenaar")>
    Party = 2

    ''' <summary>Een externe provider (webhook/callback).</summary>
    <Display(Name:="Provider")>
    Provider = 3

End Enum
