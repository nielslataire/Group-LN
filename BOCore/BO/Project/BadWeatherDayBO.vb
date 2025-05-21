Public Class BadWeatherDayBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _weatherstationid As Integer
    Public Property WeatherStationId() As Integer
        Get
            Return _weatherstationid
        End Get
        Set(ByVal value As Integer)
            _weatherstationid = value
        End Set
    End Property
    Private _date As DateOnly
    Public Property BWDate() As DateOnly
        Get
            Return _date
        End Get
        Set(ByVal value As DateOnly)
            _date = value
        End Set
    End Property
    Private _type As Integer
    Public Property Type() As Integer
        Get
            Return _type
        End Get
        Set(ByVal value As Integer)
            _type = value
        End Set
    End Property



End Class
