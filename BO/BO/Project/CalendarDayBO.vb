Public Class CalendarDayBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _title As String
    Public Property Title() As String
        Get
            Return _title
        End Get
        Set(ByVal value As String)
            _title = value
        End Set
    End Property
    Private _year As Integer
    Public Property Year() As Integer
        Get
            Return _year
        End Get
        Set(ByVal value As Integer)
            _year = value
        End Set
    End Property
    Private _month As Integer
    Public Property Month() As Integer
        Get
            Return _month
        End Get
        Set(ByVal value As Integer)
            _month = value
        End Set
    End Property
    Private _day As Integer
    Public Property Day() As Integer
        Get
            Return _day
        End Get
        Set(ByVal value As Integer)
            _day = value
        End Set
    End Property
    Private _color As String
    Public Property Color() As String
        Get
            Return _color
        End Get
        Set(ByVal value As String)
            _color = value
        End Set
    End Property



End Class
