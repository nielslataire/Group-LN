Public Class ProjectStatusBO
    Private b_id As Integer
    Public Property Id() As Integer
        Get
            Return b_id
        End Get
        Set(ByVal value As Integer)
            b_id = value
        End Set
    End Property
    Private b_Name As String
    Public Property Name() As String
        Get
            Return b_Name
        End Get
        Set(ByVal value As String)
            b_Name = value
        End Set
    End Property


End Class
