Imports System.ComponentModel.DataAnnotations

''' <summary>Kleine, generieke levenscyclus voor een projectdossier — geldt voor elk <c>DossierKind</c>.</summary>
Public Enum DossierStatus As Integer

    <Display(Name:="Nieuw")>
    Nieuw = 0

    <Display(Name:="Aangevraagd")>
    Aangevraagd = 1

    <Display(Name:="In behandeling")>
    InBehandeling = 2

    <Display(Name:="Afgehandeld")>
    Afgehandeld = 3

    <Display(Name:="Geannuleerd")>
    Geannuleerd = 4

End Enum
