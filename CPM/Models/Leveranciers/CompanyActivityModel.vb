Imports BO
Imports System.ComponentModel.DataAnnotations
Public Class CompanyActivityModel
    Public Sub New()
        _activity = New ActivityBO()
    End Sub
    Private _activity As ActivityBO
    Public Property Activity() As ActivityBO
        Get
            Return _activity
        End Get
        Set(ByVal value As ActivityBO)
            _activity = value
        End Set
    End Property
    Private _companyId As Integer
    Public Property CompanyId() As Integer
        Get
            Return _companyId
        End Get
        Set(ByVal value As Integer)
            _companyId = value
        End Set
    End Property
    Private _companyName As String
    Public Property CompanyName() As String
        Get
            Return _companyName
        End Get
        Set(ByVal value As String)
            _companyName = value
        End Set
    End Property

End Class
