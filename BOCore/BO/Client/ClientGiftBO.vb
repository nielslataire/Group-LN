Imports System.ComponentModel.DataAnnotations
Public Class ClientGiftBO
    Public Sub New()
        _activities = New List(Of ActivityBO)
    End Sub
    Private m_Id As Integer
    Public Property Id() As Integer
        Get
            Return m_Id
        End Get
        Set(ByVal value As Integer)
            m_Id = value
        End Set
    End Property
    Private m_accountid As Integer
    <Required(ErrorMessage:="Gelieve een account te kiezen")>
    Public Property AccountId() As Integer
        Get
            Return m_accountid
        End Get
        Set(ByVal value As Integer)
            m_accountid = value
        End Set
    End Property
    Private _description As String
    <Display(Name:="Toegift")>
    <Required(ErrorMessage:="Gelieve de omschrijving in te vullen")>
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property
    Private _activities As List(Of ActivityBO)
    Public Property Activities() As List(Of ActivityBO)
        Get
            Return _activities
        End Get
        Set(ByVal value As List(Of ActivityBO))
            _activities = value
        End Set
    End Property

End Class
