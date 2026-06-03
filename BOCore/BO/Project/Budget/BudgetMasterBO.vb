Imports System.ComponentModel.DataAnnotations

Public Class BudgetMasterBO
    Public Sub New()
        _versies = New List(Of BudgetVersieBO)
    End Sub

    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
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

    Private _naam As String
    <Display(Name:="Naam")>
    Public Property Naam() As String
        Get
            Return _naam
        End Get
        Set(ByVal value As String)
            _naam = value
        End Set
    End Property

    Private _omschrijving As String
    <Display(Name:="Omschrijving")>
    Public Property Omschrijving() As String
        Get
            Return _omschrijving
        End Get
        Set(ByVal value As String)
            _omschrijving = value
        End Set
    End Property

    Private _isActief As Boolean
    Public Property IsActief() As Boolean
        Get
            Return _isActief
        End Get
        Set(ByVal value As Boolean)
            _isActief = value
        End Set
    End Property

    Private _isGearchiveerd As Boolean
    Public Property IsGearchiveerd() As Boolean
        Get
            Return _isGearchiveerd
        End Get
        Set(ByVal value As Boolean)
            _isGearchiveerd = value
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

    Private _versies As List(Of BudgetVersieBO)
    Public Property Versies() As List(Of BudgetVersieBO)
        Get
            Return _versies
        End Get
        Set(ByVal value As List(Of BudgetVersieBO))
            _versies = value
        End Set
    End Property
End Class
