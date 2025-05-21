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
    Private _vacationday As Date
    Public Property VacationDay() As Date
        Get
            Return _vacationday
        End Get
        Set(ByVal value As Date)
            _vacationday = value
        End Set
    End Property
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property

End Class
