Imports System.ComponentModel.DataAnnotations

Public Class ProjectPictureBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _name As String
    <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property

    Private _caption As String
    <Display(Name:="Caption")>
    Public Property Caption() As String
        Get
            Return _caption
        End Get
        Set(ByVal value As String)
            _caption = value
        End Set
    End Property
    Private _type As PictureType
    Public Property Type() As PictureType
        Get
            Return _type
        End Get
        Set(ByVal value As PictureType)
            _type = value
        End Set
    End Property
    Private _datetimeuploaded As DateTime?
    Public Property DateTimeUploaded() As DateTime?
        Get
            Return _datetimeuploaded
        End Get
        Set(ByVal value As DateTime?)
            _datetimeuploaded = value
        End Set
    End Property

    Private _projectId As Integer
    <Display(Name:="ProjectId")>
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property
    Private _facebookidcopro As String
    Public Property FacebookIdCopro() As String
        Get
            Return _facebookidcopro
        End Get
        Set(ByVal value As String)
            _facebookidcopro = value
        End Set
    End Property

    Private _sectionId As Integer?
    Public Property SectionId() As Integer?
        Get
            Return _sectionId
        End Get
        Set(ByVal value As Integer?)
            _sectionId = value
        End Set
    End Property

    Private _isPublic As Boolean = True
    Public Property IsPublic() As Boolean
        Get
            Return _isPublic
        End Get
        Set(ByVal value As Boolean)
            _isPublic = value
        End Set
    End Property

    Private _sortOrder As Integer
    Public Property SortOrder() As Integer
        Get
            Return _sortOrder
        End Get
        Set(ByVal value As Integer)
            _sortOrder = value
        End Set
    End Property

    ''' <summary>0 = Photo, 1 = Video</summary>
    Private _mediaType As Integer
    Public Property MediaType() As Integer
        Get
            Return _mediaType
        End Get
        Set(ByVal value As Integer)
            _mediaType = value
        End Set
    End Property

    Private _fileSizeBytes As Long?
    Public Property FileSizeBytes() As Long?
        Get
            Return _fileSizeBytes
        End Get
        Set(ByVal value As Long?)
            _fileSizeBytes = value
        End Set
    End Property

    Private _widthPx As Integer?
    Public Property WidthPx() As Integer?
        Get
            Return _widthPx
        End Get
        Set(ByVal value As Integer?)
            _widthPx = value
        End Set
    End Property

    Private _heightPx As Integer?
    Public Property HeightPx() As Integer?
        Get
            Return _heightPx
        End Get
        Set(ByVal value As Integer?)
            _heightPx = value
        End Set
    End Property

    Private _durationSeconds As Double?
    Public Property DurationSeconds() As Double?
        Get
            Return _durationSeconds
        End Get
        Set(ByVal value As Double?)
            _durationSeconds = value
        End Set
    End Property

    Private _sectionName As String
    Public Property SectionName() As String
        Get
            Return _sectionName
        End Get
        Set(ByVal value As String)
            _sectionName = value
        End Set
    End Property

End Class
