Public Class FacebookFeedBO
    Public Sub New()
        _messagetags = New List(Of String)
    End Sub
    Private _name As String
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _link As String
    Public Property Link() As String
        Get
            Return _link
        End Get
        Set(ByVal value As String)
            _link = value
        End Set
    End Property
    Private _caption As String
    Public Property Caption() As String
        Get
            Return _caption
        End Get
        Set(ByVal value As String)
            _caption = value
        End Set
    End Property
    Private _description As String
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property
    Private _picture As String
    Public Property Picture() As String
        Get
            Return _picture
        End Get
        Set(ByVal value As String)
            _picture = value
        End Set
    End Property
    Private _message As String
    Public Property Message() As String
        Get
            Return _message
        End Get
        Set(ByVal value As String)
            _message = value
        End Set
    End Property
    Private _action As String
    Public Property Action() As String
        Get
            Return _action
        End Get
        Set(ByVal value As String)
            _action = value
        End Set
    End Property
    Private _actionlink As String
    Public Property Actionlink() As String
        Get
            Return _actionlink
        End Get
        Set(ByVal value As String)
            _actionlink = value
        End Set
    End Property
    Private _messagetags As List(Of String)
    Public Property MessageTags() As List(Of String)
        Get
            Return _messagetags
        End Get
        Set(ByVal value As List(Of String))
            _messagetags = value
        End Set
    End Property


End Class
