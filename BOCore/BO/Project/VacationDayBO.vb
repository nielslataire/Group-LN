Public Class VacationDayBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _vacationday As DateOnly
    Public Property VacationDay() As DateOnly
        Get
            Return _vacationday
        End Get
        Set(ByVal value As DateOnly)
            _vacationday = value
        End Set
    End Property
    Private _projectid As Integer?
    Public Property ProjectId() As Integer?
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer?)
            _projectid = value
        End Set
    End Property

    Private _kind As Byte
    ''' <summary>0 = bouwverlof, 1 = feestdag.</summary>
    Public Property Kind() As Byte
        Get
            Return _kind
        End Get
        Set(ByVal value As Byte)
            _kind = value
        End Set
    End Property

End Class
