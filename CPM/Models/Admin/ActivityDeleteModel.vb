Imports BO
Public Class ActivityDeleteModel
    Private _Activity As ActivityBO
    Public Property Activity() As ActivityBO
        Get
            Return _Activity
        End Get
        Set(ByVal value As ActivityBO)
            _Activity = value
        End Set
    End Property
    Private _activitycount As Integer
    Public Property ActivityCount() As Integer
        Get
            Return _activitycount
        End Get
        Set(ByVal value As Integer)
            _activitycount = value
        End Set
    End Property


End Class
