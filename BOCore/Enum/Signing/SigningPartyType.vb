Imports System.ComponentModel.DataAnnotations

''' <summary>Waar een ondertekenaar vandaan komt. <c>SigningParty.SourceRefId</c> verwijst naar de bijhorende tabel.</summary>
Public Enum SigningPartyType As Integer

    ''' <summary>Het klantaccount zelf (<c>ClientAccount.Id</c>).</summary>
    <Display(Name:="Klant")>
    ClientAccount = 0

    ''' <summary>Een contactpersoon/mede-eigenaar (<c>ClientContacts.Id</c>).</summary>
    <Display(Name:="Contactpersoon")>
    ClientContact = 1

    ''' <summary>Een interne CPM-gebruiker (<c>Users.Id</c>) — tegenondertekening, geverifieerd via de eigen login.</summary>
    <Display(Name:="Interne gebruiker")>
    InternalUser = 2

    ''' <summary>Een persoon buiten CPM (bv. een aannemerscontact); enkel naam en e-mail als snapshot.</summary>
    <Display(Name:="Extern")>
    External = 3

End Enum
