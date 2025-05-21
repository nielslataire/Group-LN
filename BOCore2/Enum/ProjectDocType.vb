Imports System.ComponentModel.DataAnnotations
Public Enum ProjectDocType As Integer

    <Display(Name:="Afgifte PID")>
    PID_handoff = 9
    <Display(Name:="Attest")>
    Invoicing = 2
    <Display(Name:="Brandkeuring")>
    Fire_inspection = 15
    <Display(Name:="Bouwvergunning")>
    Buildingpermit = 16
    <Display(Name:="Certificaat sleutelcombinatie")>
    keycombinationcertificate = 11
    <Display(Name:="Conformiteitverslag Brandweer")>
    Firedepartementreport = 12
    <Display(Name:="Definitieve oplevering")>
    DefDelivery = 4
    <Display(Name:="Elektrische keuring")>
    Electrical_inspection = 6
    <Display(Name:="EPB-dossier")>
    EPB = 10
    <Display(Name:="EPC-attest")>
    EPC = 5
    <Display(Name:="Gaskeuring")>
    Gas_inspection = 8
    <Display(Name:="Rioolkeuring")>
    Sewer_inspection = 13
    <Display(Name:="Postinterventie dossier")>
    PID = 14
    <Display(Name:="Verkoopsdocument")>
    Sales = 1
    <Display(Name:="Voorlopige oplevering")>
    Delivery = 3
    <Display(Name:="Waterkeuring")>
    Water_inspection = 7
End Enum

