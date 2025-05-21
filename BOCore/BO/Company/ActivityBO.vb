Public Class ActivityBO
    Public Sub New()
        Group = New ActivityGroupBO
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

    Private _groupName As String
    <Obsolete>
    Public ReadOnly Property GroupName() As String
        Get
            Return Group.Name
        End Get
    End Property
    Private _group As ActivityGroupBO
    Public Property Group() As ActivityGroupBO
        Get
            Return _group
        End Get
        Set(ByVal value As ActivityGroupBO)
            _group = value
        End Set
    End Property

End Class
