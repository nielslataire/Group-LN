Imports System.ComponentModel.DataAnnotations

''' <summary>Ondertekenregel van een dossier: wie moet tekenen voor het dossier voltooid is.</summary>
Public Enum SigningRule As Integer

    ''' <summary>Alle aangeduide personen moeten ondertekenen; iedereen wordt tegelijk uitgenodigd.</summary>
    <Display(Name:="Iedereen")>
    All = 0

    ''' <summary>Eén van de aangeduide personen volstaat; na de eerste handtekening worden de andere links ingetrokken.</summary>
    <Display(Name:="Eén volstaat")>
    Any = 1

    ''' <summary>Iedereen, in volgorde: de volgende wordt pas uitgenodigd nadat de vorige ondertekend heeft.</summary>
    <Display(Name:="In volgorde")>
    Ordered = 2

End Enum
