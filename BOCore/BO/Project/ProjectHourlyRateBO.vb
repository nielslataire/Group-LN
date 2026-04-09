Imports System.ComponentModel.DataAnnotations

Public Class ProjectHourlyRateBO

    Public Property Id As Integer
    Public Property ProjectId As Integer

    <Display(Name:="Gebruiker ID")>
    Public Property UserId As String

    <Display(Name:="Naam")>
    Public Property UserFullName As String

    <Display(Name:="Uurtarief (€)")>
    Public Property HourlyRate As Decimal

End Class
