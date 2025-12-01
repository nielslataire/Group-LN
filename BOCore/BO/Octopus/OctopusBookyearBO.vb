Imports System.Collections.Generic

Public Class OctopusBookyearBO
    Public Property Id As Integer
    Public Property IssuerCompanyId As Integer
    Public Property BookyearKeyId As Integer
    Public Property BookyearDescription As String
    Public Property StartDate As Date
    Public Property EndDate As Date
    Public Property Closed As Boolean
    Public Property Periods As List(Of OctopusBookyearPeriodBO) = New List(Of OctopusBookyearPeriodBO)()
    Public Property Journals As List(Of OctopusJournalBO) = New List(Of OctopusJournalBO)()
End Class

Public Class OctopusBookyearPeriodBO
    Public Property Id As Integer
    Public Property BookyearId As Integer
    Public Property BookyearPeriod As Integer
    Public Property StartDate As Date
    Public Property EndDate As Date
End Class

Public Class OctopusJournalBO
    Public Property Id As Integer
    Public Property BookyearId As Integer
    Public Property BookyearKeyId As Integer
    Public Property JournalKey As String
    Public Property Name As String
    Public Property Closed As Boolean
    Public Property CurrencyCode As String
    Public Property LastBookedDocumentNr As Integer?
    Public Property ProtectedPeriod As Integer?
    Public Property ClosedPeriod As Integer?
    Public Property InsertionType As Integer?
    Public Property CustomFieldListJson As String
    Public Property CustomFieldLineListJson As String
End Class