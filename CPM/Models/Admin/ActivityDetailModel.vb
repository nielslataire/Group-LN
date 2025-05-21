Imports BO
Public Class ActivityDetailModel
    Private _Activity As ActivityBO
    Public Property Activity() As ActivityBO
        Get
            Return _Activity
        End Get
        Set(ByVal value As ActivityBO)
            _Activity = value
        End Set
    End Property
   



End Class
