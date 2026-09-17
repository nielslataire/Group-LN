Imports System.ComponentModel.DataAnnotations

''' <summary>
''' Bron waaruit de werkelijke/streefdatum van een mijlpaal wordt afgeleid. De resolver
''' (increment 2) leest deze bronnen uitsluitend read-only.
''' </summary>
Public Enum ComputedBinding As Integer

    <Display(Name:="Handmatig")>
    Handmatig = 0

    <Display(Name:="Projectdocument (type)")>
    ProjectDoc = 1

    <Display(Name:="Projectdatum (kolom)")>
    ProjectDatum = 2

    <Display(Name:="Projectvlag (bool-kolom)")>
    ProjectVlag = 3

    <Display(Name:="Planningstaak")>
    PlanningTaak = 4

    <Display(Name:="Planningssectie")>
    PlanningSectie = 5

    <Display(Name:="Facturatieschijf")>
    InvoicingPaymentStage = 6

    <Display(Name:="Klantdatum (compromis/akte/oplevering)")>
    ClientAccountDatum = 7

    <Display(Name:="Nutsafrekening")>
    ConnectionSettlement = 8

    <Display(Name:="Werfpunten in fase")>
    ConstructionIssue = 9

    <Display(Name:="Voortgangsfase")>
    ProjectVoortgangFase = 10

    <Display(Name:="Gekoppeld dossier")>
    Dossier = 11

    <Display(Name:="Stap in gekoppeld dossier")>
    DossierSubstap = 12

    ''' <summary>
    ''' Voor een mijlpaal met meerdere gekoppelde dossiers (bv. één nutsaanvraag per eenheid):
    ''' bereikt zodra élk gekoppeld dossier (via ProjectDossierMijlpaal) dezelfde checklist-stap
    ''' (BronParam) heeft afgerond. Werkelijke datum = de laatste (strengste) van die stap-datums.
    ''' </summary>
    <Display(Name:="Stap op alle gekoppelde dossiers")>
    AlleGekoppeldeDossierSubstappen = 13

    ''' <summary>
    ''' Zoals AlleGekoppeldeDossierSubstappen, maar zonder dat er iets gekoppeld moet worden: kijkt
    ''' automatisch naar élk Nutsaansluiting-dossier van het project (huidige en toekomstige, geen
    ''' handmatige "Koppel"-stap nodig). Bereikt zodra elk niet-geannuleerd nutsdossier de gekozen
    ''' checklist-stap (BronParam) heeft afgerond.
    ''' </summary>
    <Display(Name:="Stap op alle nutsaanvragen (heel het project)")>
    AlleNutsaanvragenSubstap = 14

End Enum
