Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssueNotificationChannel As Integer

    <Display(Name:="E-mail")>
    Email = 0

    <Display(Name:="PDF-rapport")>
    PDFReport = 1

    <Display(Name:="Portaalmelding")>
    PortalStub = 2

End Enum