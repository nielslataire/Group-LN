
Public Class ActivityGroupBO
    Public Sub New()
        _activities = New List(Of ActivityBO)
    End Sub
    Private _id As Integer
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _name As String
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _lot As Integer
    Public Property Lot() As Integer
        Get
            Return _lot
        End Get
        Set(ByVal value As Integer)
            _lot = value
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
