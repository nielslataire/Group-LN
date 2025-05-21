Imports System.ComponentModel.DataAnnotations

Public Class ProjectBO
    Public Sub New()
        _postalcode = New PostalCodeBO
        _Status = New ProjectStatusBO
        _wheaterstation = New WheaterStationBO
        _developer = New IdNameBO
        _architect = New IdNameBO
        _engineer = New IdNameBO
        _securitycoordinator = New IdNameBO
        _epbreporter = New IdNameBO
        _builder = New IdNameBO
        _pictures = New List(Of ProjectPictureBO)
        _defaultpicture = New ProjectPictureBO
    End Sub
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
    Private _slug As String
    Public Property Slug() As String
        Get
            Return _slug
        End Get
        Set(ByVal value As String)
            _slug = value
        End Set
    End Property


    Private _street As String
    <Display(Name:="Adres")>
    Public Property Street() As String
        Get
            Return _street
        End Get
        Set(ByVal value As String)
            _street = value
        End Set
    End Property
    Private _housenumber As String
    Public Property HouseNumber() As String
        Get
            Return _housenumber
        End Get
        Set(ByVal value As String)
            _housenumber = value
        End Set
    End Property
    Private _postalcode As PostalCodeBO
    <Display(Name:="Gemeente")>
    <DisplayFormat(HtmlEncode:=True)>
    Public Property Postalcode() As PostalCodeBO
        Get
            Return _postalcode
        End Get
        Set(ByVal value As PostalCodeBO)
            _postalcode = value
        End Set
    End Property
    Private _Status As ProjectStatusBO
    <Display(Name:="Status")>
    Public Property Status() As ProjectStatusBO
        Get
            Return _Status
        End Get
        Set(ByVal value As ProjectStatusBO)
            _Status = value
        End Set
    End Property
    Private _commercialtitlenl As String
    <Display(Name:="Titel")>
    Public Property CommercialTitleNL() As String
        Get
            Return _commercialtitlenl
        End Get
        Set(ByVal value As String)
            _commercialtitlenl = value
        End Set
    End Property
    Private _commercialtextnl As String

    <Display(Name:="Tekst")>
    Public Property CommercialTextNL() As String
        Get
            Return _commercialtextnl
        End Get
        Set(ByVal value As String)
            _commercialtextnl = value
        End Set
    End Property
    'Opleveringsdatum
    Private _deliverydate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Opleveringsdatum")>
    Public Property DeliveryDate() As DateOnly?
        Get
            Return _deliverydate
        End Get
        Set(ByVal value As DateOnly?)
            _deliverydate = value
        End Set
    End Property
    'Definitieve Opleveringsdatum
    Private _deliverydatedef As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Definitieve opleveringsdatum")>
    Public Property DeliveryDateDef() As DateOnly?
        Get
            Return _deliverydatedef
        End Get
        Set(ByVal value As DateOnly?)
            _deliverydatedef = value
        End Set
    End Property
    'Aanvangsdatum werken
    Private _startdateconstruction As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Aanvangsdatum")>
    Public Property StartDateConstruction() As DateOnly?
        Get
            Return _startdateconstruction
        End Get
        Set(ByVal value As DateOnly?)
            _startdateconstruction = value
        End Set
    End Property
    'Bouwheer
    Private _developer As IdNameBO
    <Display(Name:="Projectontwikkelaar")>
    Public Property Developer() As IdNameBO
        Get
            Return _developer
        End Get
        Set(ByVal value As IdNameBO)
            _developer = value
        End Set
    End Property
    Private _builder As IdNameBO
    <Display(Name:="Bouwheer")>
    Public Property Builder() As IdNameBO
        Get
            Return _builder
        End Get
        Set(ByVal value As IdNameBO)
            _builder = value
        End Set
    End Property
    Private _executiondays As Integer?
    <Display(Name:="Uitvoeringstermijn")>
    Public Property ExecutionDays() As Integer?
        Get
            Return _executiondays
        End Get
        Set(ByVal value As Integer?)
            _executiondays = value
        End Set
    End Property
    Private _wheaterstation As WheaterStationBO
    <Display(Name:="Weerstation")>
    Public Property WheaterStation() As WheaterStationBO
        Get
            Return _wheaterstation
        End Get
        Set(ByVal value As WheaterStationBO)
            _wheaterstation = value
        End Set
    End Property
    Private _pictures As List(Of ProjectPictureBO)
    Public Property Pictures() As List(Of ProjectPictureBO)
        Get
            Return _pictures
        End Get
        Set(ByVal value As List(Of ProjectPictureBO))
            _pictures = value
        End Set
    End Property
    Private _defaultpicture As ProjectPictureBO
    Public Property DefaultPicture() As ProjectPictureBO
        Get
            Return _defaultpicture
        End Get
        Set(ByVal value As ProjectPictureBO)
            _defaultpicture = value
        End Set
    End Property
    Private _architect As IdNameBO
    <Display(Name:="Architect")>
    Public Property Architect() As IdNameBO
        Get
            Return _architect
        End Get
        Set(ByVal value As IdNameBO)
            _architect = value
        End Set
    End Property
    Private _engineer As IdNameBO
    <Display(Name:="Ingenieur")>
    Public Property Engineer() As IdNameBO
        Get
            Return _engineer
        End Get
        Set(ByVal value As IdNameBO)
            _engineer = value
        End Set
    End Property
    Private _securitycoordinator As IdNameBO
    <Display(Name:="Veiligheidscoördinator")>
    Public Property SecurityCoordinator() As IdNameBO
        Get
            Return _securitycoordinator
        End Get
        Set(ByVal value As IdNameBO)
            _securitycoordinator = value
        End Set
    End Property
    Private _epbreporter As IdNameBO
    <Display(Name:="EPB-verslaggever")>
    Public Property EpbReporter() As IdNameBO
        Get
            Return _epbreporter
        End Get
        Set(ByVal value As IdNameBO)
            _epbreporter = value
        End Set
    End Property
    Private _facebookalbumid As Decimal?
    Public Property FacebookAlbumId() As Decimal?
        Get
            Return _facebookalbumid
        End Get
        Set(ByVal value As Decimal?)
            _facebookalbumid = value
        End Set
    End Property
    Private _uploadtofacebook As Boolean
    Public Property UploadToFacebook() As Boolean
        Get
            Return _uploadtofacebook
        End Get
        Set(ByVal value As Boolean)
            _uploadtofacebook = value
        End Set
    End Property
    Private _aspnetuserid As String
    <Display(Name:="Projectbeheerder")>
    Public Property AspNetUserID() As String
        Get
            Return _aspnetuserid
        End Get
        Set(ByVal value As String)
            _aspnetuserid = value
        End Set
    End Property
    Private _totallandshare As Decimal?
    <Display(Name:="Totale grondaandelen")>
    Public Property TotalLandShare() As Decimal?
        Get
            Return _totallandshare
        End Get
        Set(ByVal value As Decimal?)
            _totallandshare = value
        End Set
    End Property
    Private _facebookPlaceId As String
    Public Property FacebookPlaceId() As String
        Get
            Return _facebookPlaceId
        End Get
        Set(ByVal value As String)
            _facebookPlaceId = value
        End Set
    End Property
    Private _projectfolder As String
    <Display(Name:="Projectmap")>
    Public Property ProjectFolder() As String
        Get
            Return _projectfolder
        End Get
        Set(ByVal value As String)
            _projectfolder = value
        End Set
    End Property
    Private _docpid As Boolean?
    <Display(Name:="Postinterventiedossier")>
    Public Property DocPID() As Boolean?
        Get
            Return _docpid
        End Get
        Set(ByVal value As Boolean?)
            _docpid = value
        End Set
    End Property
    Private _docelectricalinspection As Boolean?
    <Display(Name:="Elektrische keuring")>
    Public Property DocElectricalInspection() As Boolean?
        Get
            Return _docelectricalinspection
        End Get
        Set(ByVal value As Boolean?)
            _docelectricalinspection = value
        End Set
    End Property
    Private _docwaterinspection As Boolean?
    <Display(Name:="Waterkeuring")>
    Public Property DocWaterInspection() As Boolean?
        Get
            Return _docwaterinspection
        End Get
        Set(ByVal value As Boolean?)
            _docwaterinspection = value
        End Set
    End Property
    Private _docsewerinspection As Boolean?
    <Display(Name:="Rioolkeuring")>
    Public Property DocSewerInspection() As Boolean?
        Get
            Return _docsewerinspection
        End Get
        Set(ByVal value As Boolean?)
            _docsewerinspection = value
        End Set
    End Property
    Private _docfireinspection As Boolean?
    <Display(Name:="Branddetectie keuring")>
    Public Property DocFireInspection() As Boolean?
        Get
            Return _docfireinspection
        End Get
        Set(ByVal value As Boolean?)
            _docfireinspection = value
        End Set
    End Property
    Private _docdelivery As Boolean?
    <Display(Name:="Voorlopige oplevering")>
    Public Property DocDelivery() As Boolean?
        Get
            Return _docdelivery
        End Get
        Set(ByVal value As Boolean?)
            _docdelivery = value
        End Set
    End Property
    Private _docdefdelivery As Boolean?
    <Display(Name:="Definitieve oplevering")>
    Public Property DocDefDelivery() As Boolean?
        Get
            Return _docdefdelivery
        End Get
        Set(ByVal value As Boolean?)
            _docdefdelivery = value
        End Set
    End Property
    Private _projecttype As ProjectType
    <Display(Name:="Type project")>
    Public Property ProjectType() As ProjectType
        Get
            Return _projecttype
        End Get
        Set(ByVal value As ProjectType)
            _projecttype = value
        End Set
    End Property
End Class
