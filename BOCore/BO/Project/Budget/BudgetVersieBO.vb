Imports System.ComponentModel.DataAnnotations

Public Class BudgetVersieBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Private _budgetMasterId As Integer
    Public Property BudgetMasterId() As Integer
        Get
            Return _budgetMasterId
        End Get
        Set(ByVal value As Integer)
            _budgetMasterId = value
        End Set
    End Property

    Private _projectId As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property

    Private _versienummer As Integer
    Public Property Versienummer() As Integer
        Get
            Return _versienummer
        End Get
        Set(ByVal value As Integer)
            _versienummer = value
        End Set
    End Property

    Private _versieNaam As String
    <Display(Name:="Versienaam")>
    Public Property VersieNaam() As String
        Get
            Return _versieNaam
        End Get
        Set(ByVal value As String)
            _versieNaam = value
        End Set
    End Property

    Private _status As String
    <Display(Name:="Status")>
    Public Property Status() As String
        Get
            Return _status
        End Get
        Set(ByVal value As String)
            _status = value
        End Set
    End Property

    Private _isHuidig As Boolean
    Public Property IsHuidig() As Boolean
        Get
            Return _isHuidig
        End Get
        Set(ByVal value As Boolean)
            _isHuidig = value
        End Set
    End Property

    Private _notitie As String
    <Display(Name:="Notitie")>
    Public Property Notitie() As String
        Get
            Return _notitie
        End Get
        Set(ByVal value As String)
            _notitie = value
        End Set
    End Property

    Private _createdAt As DateTime
    Public Property CreatedAt() As DateTime
        Get
            Return _createdAt
        End Get
        Set(ByVal value As DateTime)
            _createdAt = value
        End Set
    End Property

    Private _createdByUserId As Integer?
    Public Property CreatedByUserId() As Integer?
        Get
            Return _createdByUserId
        End Get
        Set(ByVal value As Integer?)
            _createdByUserId = value
        End Set
    End Property

    Public ReadOnly Property VersieLabel() As String
        Get
            If Not String.IsNullOrWhiteSpace(_versieNaam) Then
                Return $"v{_versienummer} • {_versieNaam}"
            Else
                Return $"v{_versienummer}"
            End If
        End Get
    End Property
End Class
