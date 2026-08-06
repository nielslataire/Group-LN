Public Class HomeHeroFeaturedModel
    Private _kicker As String
    Public Property Kicker() As String
        Get
            Return _kicker
        End Get
        Set(ByVal value As String)
            _kicker = value
        End Set
    End Property
    Private _titel As String
    Public Property Titel() As String
        Get
            Return _titel
        End Get
        Set(ByVal value As String)
            _titel = value
        End Set
    End Property
    Private _tekst As String
    Public Property Tekst() As String
        Get
            Return _tekst
        End Get
        Set(ByVal value As String)
            _tekst = value
        End Set
    End Property
    Private _projecttitel As String
    Public Property ProjectTitel() As String
        Get
            Return _projecttitel
        End Get
        Set(ByVal value As String)
            _projecttitel = value
        End Set
    End Property
    Private _imagesrc As String
    Public Property ImageSrc() As String
        Get
            Return _imagesrc
        End Get
        Set(ByVal value As String)
            _imagesrc = value
        End Set
    End Property
    Private _videosrc As String
    Public Property VideoSrc() As String
        Get
            Return _videosrc
        End Get
        Set(ByVal value As String)
            _videosrc = value
        End Set
    End Property
    Private _isvideo As Boolean
    Public Property IsVideo() As Boolean
        Get
            Return _isvideo
        End Get
        Set(ByVal value As Boolean)
            _isvideo = value
        End Set
    End Property
    Private _detailurl As String
    Public Property DetailUrl() As String
        Get
            Return _detailurl
        End Get
        Set(ByVal value As String)
            _detailurl = value
        End Set
    End Property
End Class
