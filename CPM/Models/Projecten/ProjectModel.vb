Imports BO
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectModel
    Public Sub New()
        _project = New ProjectBO()
        _countries = New List(Of IdNameBO)

    End Sub
    Private _project As ProjectBO
    Public Property Project() As ProjectBO
        Get
            Return _project
        End Get
        Set(ByVal value As ProjectBO)
            _project = value
        End Set
    End Property
    Private _countries As List(Of IdNameBO)
    Public Property Countries() As List(Of IdNameBO)
        Get
            Return _countries
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _countries = value
        End Set
    End Property
    Private _selectedCountry As Integer
    Public Property SelectedCountry() As Integer
        Get
            Return _selectedCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCountry = value
        End Set
    End Property
    Private _selectedPostalCode As Integer
    Public Property SelectedPostalcode() As Integer
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedPostalCode = value
        End Set
    End Property

End Class
Public Class EditProjectDetail
    Public Sub New()
        _project = New ProjectBO
        _countries = New List(Of IdNameBO)
        _facebookplaces = New List(Of FacebookPlaceBO)
    End Sub
    'Projectgegevens
    Private _project As ProjectBO
    Public Property Project As ProjectBO
        Get
            Return _project
        End Get
        Set(ByVal value As ProjectBO)
            _project = value
        End Set
    End Property
    Private _image As ImageBO
    Public Property Image() As ImageBO
        Get
            Return _image
        End Get
        Set(ByVal value As ImageBO)
            _image = value
        End Set
    End Property
    Private _imageupload As HttpPostedFileBase
    <DataType(DataType.Upload)>
    Public Property ImageUpload() As HttpPostedFileBase
        Get
            Return _imageupload
        End Get
        Set(ByVal value As HttpPostedFileBase)
            _imageupload = value
        End Set
    End Property
    Private _countries As List(Of IdNameBO)
    Public Property Countries() As List(Of IdNameBO)
        Get
            Return _countries
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _countries = value
        End Set
    End Property
    Private _users As IEnumerable(Of ApplicationUser)
    Public Property Users() As IEnumerable(Of ApplicationUser)
        Get
            Return _users
        End Get
        Set(ByVal value As IEnumerable(Of ApplicationUser))
            _users = value
        End Set
    End Property
    Private _selectedCountry As Integer
    Public Property SelectedCountry() As Integer
        Get
            Return _selectedCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCountry = value
        End Set
    End Property
    Private _selectedPostalCode As Integer
    Public Property SelectedPostalcode() As Integer
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedPostalCode = value
        End Set
    End Property
    Private _generaldataeditmode As Boolean
    Public Property GeneralDataEditMode() As Boolean
        Get
            Return _generaldataeditmode
        End Get
        Set(ByVal value As Boolean)
            _generaldataeditmode = value
        End Set
    End Property
    Private _selectedStatus As Integer
    Public Property SelectedStatus() As Integer
        Get
            Return _selectedStatus
        End Get
        Set(ByVal value As Integer)
            _selectedStatus = value
        End Set
    End Property
    Private _statuses As List(Of IdNameBO)
    Public Property Statuses() As List(Of IdNameBO)
        Get
            Return _statuses
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _statuses = value
        End Set
    End Property
    Private _facebookplaces As List(Of FacebookPlaceBO)
    Public Property FacebookPlaces() As List(Of FacebookPlaceBO)
        Get
            Return _facebookplaces
        End Get
        Set(ByVal value As List(Of FacebookPlaceBO))
            _facebookplaces = value
        End Set
    End Property
    Private _selectedfacebookplace As FacebookPlaceBO
    Public Property SelectedFacebookPlace() As FacebookPlaceBO
        Get
            Return _selectedfacebookplace
        End Get
        Set(ByVal value As FacebookPlaceBO)
            _selectedfacebookplace = value
        End Set
    End Property
    Private _docs As List(Of ProjectDocBO)
    Public Property Docs() As List(Of ProjectDocBO)
        Get
            Return _docs
        End Get
        Set(ByVal value As List(Of ProjectDocBO))
            _docs = value
        End Set
    End Property


End Class
Public Class ShowProjectsModel
    Public Sub New()
        _projects = New List(Of ProjectBO)
    End Sub
    Private _projects As List(Of ProjectBO)
    Public Property Projects() As List(Of ProjectBO)
        Get
            Return _projects
        End Get
        Set(ByVal value As List(Of ProjectBO))
            _projects = value
        End Set
    End Property
    Private _statuses As List(Of ProjectStatusBO)
    Public Property Statuses() As List(Of ProjectStatusBO)
        Get
            Return _statuses
        End Get
        Set(ByVal value As List(Of ProjectStatusBO))
            _statuses = value
        End Set
    End Property


End Class
'Detail
Public Class ShowProjectDetail
    Public Sub New()
        _project = New ProjectBO
        _countries = New List(Of IdNameBO)
        _facebookplaces = New List(Of FacebookPlaceBO)
        _docs = New List(Of ProjectDocBO)
        _recentclients = New List(Of IdNameBO)
        _LatestNews = New ProjectNewsBO
        _latestpicture = New ProjectPictureBO
        _latestDocs = New List(Of ProjectDocBO)
    End Sub
    'Projectgegevens
    Private _project As ProjectBO
    Public Property Project As ProjectBO
        Get
            Return _project
        End Get
        Set(ByVal value As ProjectBO)
            _project = value
        End Set
    End Property
    Private _image As ImageBO
    Public Property Image() As ImageBO
        Get
            Return _image
        End Get
        Set(ByVal value As ImageBO)
            _image = value
        End Set
    End Property
    Private _imageupload As HttpPostedFileBase
    <DataType(DataType.Upload)>
    Public Property ImageUpload() As HttpPostedFileBase
        Get
            Return _imageupload
        End Get
        Set(ByVal value As HttpPostedFileBase)
            _imageupload = value
        End Set
    End Property
    Private _countries As List(Of IdNameBO)
    Public Property Countries() As List(Of IdNameBO)
        Get
            Return _countries
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _countries = value
        End Set
    End Property
    Private _users As IEnumerable(Of ApplicationUser)
    Public Property Users() As IEnumerable(Of ApplicationUser)
        Get
            Return _users
        End Get
        Set(ByVal value As IEnumerable(Of ApplicationUser))
            _users = value
        End Set
    End Property
    Private _selectedCountry As Integer
    Public Property SelectedCountry() As Integer
        Get
            Return _selectedCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCountry = value
        End Set
    End Property
    Private _selectedPostalCode As Integer
    Public Property SelectedPostalcode() As Integer
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedPostalCode = value
        End Set
    End Property
    Private _generaldataeditmode As Boolean
    Public Property GeneralDataEditMode() As Boolean
        Get
            Return _generaldataeditmode
        End Get
        Set(ByVal value As Boolean)
            _generaldataeditmode = value
        End Set
    End Property
    Private _selectedStatus As Integer
    Public Property SelectedStatus() As Integer
        Get
            Return _selectedStatus
        End Get
        Set(ByVal value As Integer)
            _selectedStatus = value
        End Set
    End Property
    Private _statuses As List(Of IdNameBO)
    Public Property Statuses() As List(Of IdNameBO)
        Get
            Return _statuses
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _statuses = value
        End Set
    End Property
    Private _facebookplaces As List(Of FacebookPlaceBO)
    Public Property FacebookPlaces() As List(Of FacebookPlaceBO)
        Get
            Return _facebookplaces
        End Get
        Set(ByVal value As List(Of FacebookPlaceBO))
            _facebookplaces = value
        End Set
    End Property
    Private _selectedfacebookplace As FacebookPlaceBO
    Public Property SelectedFacebookPlace() As FacebookPlaceBO
        Get
            Return _selectedfacebookplace
        End Get
        Set(ByVal value As FacebookPlaceBO)
            _selectedfacebookplace = value
        End Set
    End Property
    Private _docs As List(Of ProjectDocBO)
    Public Property Docs() As List(Of ProjectDocBO)
        Get
            Return _docs
        End Get
        Set(ByVal value As List(Of ProjectDocBO))
            _docs = value
        End Set
    End Property
    Private _executiondays As Integer
    Public Property ExecutionDays() As Integer
        Get
            Return _executiondays
        End Get
        Set(ByVal value As Integer)
            _executiondays = value
        End Set
    End Property
    Private _startdate As Date
    Public Property StartDate() As Date
        Get
            Return _startdate
        End Get
        Set(ByVal value As Date)
            _startdate = value
        End Set
    End Property
    Private _finalconstructiondate As Date
    Public Property FinalConstructionDate() As Date
        Get
            Return _finalconstructiondate
        End Get
        Set(ByVal value As Date)
            _finalconstructiondate = value
        End Set
    End Property
    Private _workingdaysleft As Integer
    Public Property WorkingDaysLeft() As Integer
        Get
            Return _workingdaysleft
        End Get
        Set(ByVal value As Integer)
            _workingdaysleft = value
        End Set
    End Property
    Private _recentclients As List(Of IdNameBO)
    Public Property RecentClients() As List(Of IdNameBO)
        Get
            Return _recentclients
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _recentclients = value
        End Set
    End Property
    Private _LatestNews As ProjectNewsBO
    Public Property LatestNews() As ProjectNewsBO
        Get
            Return _LatestNews
        End Get
        Set(ByVal value As ProjectNewsBO)
            _LatestNews = value
        End Set
    End Property

    Private _latestpicture As ProjectPictureBO
    Public Property LatestPicture() As ProjectPictureBO
        Get
            Return _latestpicture
        End Get
        Set(ByVal value As ProjectPictureBO)
            _latestpicture = value
        End Set
    End Property

    Private _latestDocs As List(Of ProjectDocBO)
    Public Property LatestDocs() As List(Of ProjectDocBO)
        Get
            Return _latestDocs
        End Get
        Set(ByVal value As List(Of ProjectDocBO))
            _latestDocs = value
        End Set
    End Property
End Class
Public Class DetailPhotosModel
    Public Sub New()
        _photos = New List(Of ProjectPictureBO)
    End Sub
    Private _photos As List(Of ProjectPictureBO)
    Public Property Photos() As List(Of ProjectPictureBO)
        Get
            Return _photos
        End Get
        Set(ByVal value As List(Of ProjectPictureBO))
            _photos = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property




End Class
Public Class DetailNewsModel
    Public Sub New()
        _news = New List(Of ProjectNewsBO)
    End Sub
    Private _news As List(Of ProjectNewsBO)
    Public Property News() As List(Of ProjectNewsBO)
        Get
            Return _news
        End Get
        Set(ByVal value As List(Of ProjectNewsBO))
            _news = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property




End Class
Public Class DetailUnitsModel
    Public Sub New()
        _unitsgrouped = New List(Of GroupUnitsBO)
        _addunit = New UnitBO
        _constructionvalues = New List(Of UnitConstructionValueBO)
        _grouptypes = New List(Of UnitGroupTypeBO)
        _types = New List(Of UnitTypeBO)

        _units = New List(Of IdNameBO)
        _attachableunits = New List(Of IdNameBO)
        _paymentgroups = New List(Of IdNameBO)
    End Sub
    Private _unitsgrouped As List(Of GroupUnitsBO)
    Public Property UnitsGrouped() As List(Of GroupUnitsBO)
        Get
            Return _unitsgrouped
        End Get
        Set(ByVal value As List(Of GroupUnitsBO))
            _unitsgrouped = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _projectlandshare As Integer
    Public Property ProjectLandShare() As Integer
        Get
            Return _projectlandshare
        End Get
        Set(ByVal value As Integer)
            _projectlandshare = value
        End Set
    End Property

    Private _addunit As UnitBO
    Public Property AddUnit() As UnitBO
        Get
            Return _addunit
        End Get
        Set(ByVal value As UnitBO)
            _addunit = value
        End Set
    End Property
    Private _constructionvalues As List(Of UnitConstructionValueBO)
    Public Property ConstructionValues() As List(Of UnitConstructionValueBO)
        Get
            Return _constructionvalues
        End Get
        Set(ByVal value As List(Of UnitConstructionValueBO))
            _constructionvalues = value
        End Set
    End Property
    Private _grouptypes As List(Of UnitGroupTypeBO)
    Public Property GroupTypes() As List(Of UnitGroupTypeBO)
        Get
            Return _grouptypes
        End Get
        Set(ByVal value As List(Of UnitGroupTypeBO))
            _grouptypes = value
        End Set
    End Property
    Private _types As List(Of UnitTypeBO)
    Public Property Types() As List(Of UnitTypeBO)
        Get
            Return _types
        End Get
        Set(ByVal value As List(Of UnitTypeBO))
            _types = value
        End Set
    End Property

    Private _selectedGroupType As Integer
    Public Property SelectedGroupType() As Integer
        Get
            Return _selectedGroupType
        End Get
        Set(ByVal value As Integer)
            _selectedGroupType = value
        End Set
    End Property
    Private _selectedType As Integer
    Public Property SelectedType() As Integer
        Get
            Return _selectedType
        End Get
        Set(ByVal value As Integer)
            _selectedType = value
        End Set
    End Property

    Private _units As List(Of IdNameBO)
    Public Property Units() As List(Of IdNameBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _units = value
        End Set
    End Property
    Private _attachableunits As List(Of IdNameBO)
    Public Property AttachableUnits() As List(Of IdNameBO)
        Get
            Return _attachableunits
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _attachableunits = value
        End Set
    End Property
    Private _selectedUnits As List(Of Integer)
    Public Property SelectedUnits() As List(Of Integer)
        Get
            Return _selectedUnits
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedUnits = value
        End Set
    End Property
    Private _paymentgroups As List(Of IdNameBO)
    Public Property PaymentGroups() As List(Of IdNameBO)
        Get
            Return _paymentgroups
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _paymentgroups = value
        End Set
    End Property

    Public Enum EnumType As Integer
        Eenheid = 1
        Koppeling = 2
    End Enum
    Private _type As EnumType
    Public Property Type() As EnumType
        Get
            Return _type
        End Get
        Set(ByVal value As EnumType)
            _type = value
        End Set
    End Property

End Class
Public Class DetailDocsModel
    Public Sub New()
        _docs = New List(Of ProjectDocBO)
    End Sub
    Private _docs As List(Of ProjectDocBO)
    Public Property Docs() As List(Of ProjectDocBO)
        Get
            Return _docs
        End Get
        Set(ByVal value As List(Of ProjectDocBO))
            _docs = value
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
    Private _clientaccountid As Integer
    Public Property ClientAccountId() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _clientname As String
    Public Property ClientName() As String
        Get
            Return _clientname
        End Get
        Set(ByVal value As String)
            _clientname = value
        End Set
    End Property




End Class

'UNITS
Public Class EditUnitModel
    Public Sub New()
        _units = New List(Of IdNameBO)
        _selectedUnits = New List(Of Integer)
        _attachableunits = New List(Of IdNameBO)
        _rooms = New List(Of RoomBO)
    End Sub
    Private _unit As UnitBO
    Public Property Unit() As UnitBO
        Get
            Return _unit
        End Get
        Set(ByVal value As UnitBO)
            _unit = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _projectlandshare As Integer
    Public Property ProjectLandShare() As Integer
        Get
            Return _projectlandshare
        End Get
        Set(ByVal value As Integer)
            _projectlandshare = value
        End Set
    End Property
    Private _grouptypes As List(Of UnitGroupTypeBO)
    Public Property GroupTypes() As List(Of UnitGroupTypeBO)
        Get
            Return _grouptypes
        End Get
        Set(ByVal value As List(Of UnitGroupTypeBO))
            _grouptypes = value
        End Set
    End Property
    Private _types As List(Of UnitTypeBO)
    Public Property Types() As List(Of UnitTypeBO)
        Get
            Return _types
        End Get
        Set(ByVal value As List(Of UnitTypeBO))
            _types = value
        End Set
    End Property

    Private _selectedGroupType As Integer
    Public Property SelectedGroupType() As Integer
        Get
            Return _selectedGroupType
        End Get
        Set(ByVal value As Integer)
            _selectedGroupType = value
        End Set
    End Property
    Private _selectedType As Integer
    Public Property SelectedType() As Integer
        Get
            Return _selectedType
        End Get
        Set(ByVal value As Integer)
            _selectedType = value
        End Set
    End Property

    Private _units As List(Of IdNameBO)
    Public Property Units() As List(Of IdNameBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _units = value
        End Set
    End Property
    Private _selectedUnits As List(Of Integer)
    Public Property SelectedUnits() As List(Of Integer)
        Get
            Return _selectedUnits
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedUnits = value
        End Set
    End Property
    Public Enum EnumType As Integer
        Eenheid = 1
        Koppeling = 2
    End Enum
    Private _type As EnumType
    Public Property Type() As EnumType
        Get
            Return _type
        End Get
        Set(ByVal value As EnumType)
            _type = value
        End Set
    End Property
    Private _attachableunits As List(Of IdNameBO)
    Public Property AttachableUnits() As List(Of IdNameBO)
        Get
            Return _attachableunits
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _attachableunits = value
        End Set
    End Property
    Private _rooms As List(Of RoomBO)
    Public Property Rooms() As List(Of RoomBO)
        Get
            Return _rooms
        End Get
        Set(ByVal value As List(Of RoomBO))
            _rooms = value
        End Set
    End Property
    'PaymentGroup
    Private _selectedpaymentgroup As Integer?

    Public Property SelectedPaymentGroup() As Integer?
        Get
            Return _selectedpaymentgroup
        End Get
        Set(ByVal value As Integer?)
            _selectedpaymentgroup = value
        End Set
    End Property
    Private _PaymentGroups As List(Of IdNameBO)
    Public Property PaymentGroups() As List(Of IdNameBO)
        Get
            Return _PaymentGroups
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _PaymentGroups = value
        End Set
    End Property
    Private _constructionvalues As List(Of UnitConstructionValueBO)
    Public Property ConstructionValues() As List(Of UnitConstructionValueBO)
        Get
            Return _constructionvalues
        End Get
        Set(ByVal value As List(Of UnitConstructionValueBO))
            _constructionvalues = value
        End Set
    End Property

End Class
Public Class AddUnitLinkModel
    Public Sub New()
        _units = New List(Of IdNameBO)
        _selectedunits = New List(Of Integer)
    End Sub
    Private _selectedUnit As UnitBO
    Public Property SelectedUnit() As UnitBO
        Get
            Return _selectedUnit
        End Get
        Set(ByVal value As UnitBO)
            _selectedUnit = value
        End Set
    End Property
    Private _units As List(Of IdNameBO)
    Public Property Units() As List(Of IdNameBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _units = value
        End Set
    End Property
    Private _selectedunits As List(Of Integer)
    Public Property SelectedUnits() As List(Of Integer)
        Get
            Return _selectedunits
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedunits = value
        End Set
    End Property
    Private _unit As UnitBO
    Public Property Unit() As UnitBO
        Get
            Return _unit
        End Get
        Set(ByVal value As UnitBO)
            _unit = value
        End Set
    End Property
End Class
Public Class AddUnitConstructionValueModel
    Public Sub New()
        _constructionvalue = New UnitConstructionValueBO
        _paymentgroups = New List(Of IdNameBO)
    End Sub
    Private _constructionvalue As UnitConstructionValueBO
    Public Property ConstructionValue() As UnitConstructionValueBO
        Get
            Return _constructionvalue
        End Get
        Set(ByVal value As UnitConstructionValueBO)
            _constructionvalue = value
        End Set
    End Property
    Private _paymentgroups As List(Of IdNameBO)
    Public Property Paymentgroups() As List(Of IdNameBO)
        Get
            Return _paymentgroups
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _paymentgroups = value
        End Set
    End Property

End Class
Public Class ProjectVacationDaysModel
    Public Sub New()

    End Sub
    Private _projectId As Integer
    Public Property ProjectID() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property

End Class
Public Class BWDModel
    Public Sub New()
        _weatherstations = New List(Of IdNameBO)
    End Sub
    Private _weatherstations As List(Of IdNameBO)
    Public Property WeatherStations() As List(Of IdNameBO)
        Get
            Return _weatherstations
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _weatherstations = value
        End Set
    End Property
    Private _selectedweatherstation As Integer
    Public Property SelectedWeatherStation() As Integer
        Get
            Return _selectedweatherstation
        End Get
        Set(ByVal value As Integer)
            _selectedweatherstation = value
        End Set
    End Property


End Class
'SALES
Public Class ProjectSalesModel
    Public Sub New()
        _unitsgrouped = New List(Of GroupUnitsWithAttachedUnitsBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _unitsgrouped As List(Of GroupUnitsWithAttachedUnitsBO)
    Public Property UnitsGrouped() As List(Of GroupUnitsWithAttachedUnitsBO)
        Get
            Return _unitsgrouped
        End Get
        Set(ByVal value As List(Of GroupUnitsWithAttachedUnitsBO))
            _unitsgrouped = value
        End Set
    End Property
End Class
Public Class ProjectSalesExportModel
    Public Sub New()
        _unitsgrouped = New List(Of GroupUnitsWithAttachedUnitsWithDetailsBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _unitsgrouped As List(Of GroupUnitsWithAttachedUnitsWithDetailsBO)
    Public Property UnitsGrouped() As List(Of GroupUnitsWithAttachedUnitsWithDetailsBO)
        Get
            Return _unitsgrouped
        End Get
        Set(ByVal value As List(Of GroupUnitsWithAttachedUnitsWithDetailsBO))
            _unitsgrouped = value
        End Set
    End Property
    Private _surfacetypes As List(Of RoomType)
    Public Property SurfaceTypes() As List(Of RoomType)
        Get
            Return _surfacetypes
        End Get
        Set(ByVal value As List(Of RoomType))
            _surfacetypes = value
        End Set
    End Property


End Class
Public Class ProjectSalesSelectForPriceModel
    Public Sub New()

    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _units As List(Of IdNameBO)
    Public Property Units() As List(Of IdNameBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _units = value
        End Set
    End Property
    Private _selectedunits As List(Of Integer)
    Public Property SelectedUnits() As List(Of Integer)
        Get
            Return _selectedunits
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedunits = value
        End Set
    End Property
End Class
Public Class ProjectSalesCalculatePrice
    Public Sub New()
        _units = New List(Of UnitWithReductionBO)
        _reductions = New List(Of ConstructionReductionBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _units As List(Of UnitWithReductionBO)
    Public Property Units() As List(Of UnitWithReductionBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of UnitWithReductionBO))
            _units = value
        End Set
    End Property
    Private _salessettings As ProjectSalesSettingsBO
    Public Property SalesSettings() As ProjectSalesSettingsBO
        Get
            Return _salessettings
        End Get
        Set(ByVal value As ProjectSalesSettingsBO)
            _salessettings = value
        End Set
    End Property
    Private _reductions As List(Of ConstructionReductionBO)
    Public Property Reductions() As List(Of ConstructionReductionBO)
        Get
            Return _reductions
        End Get
        Set(ByVal value As List(Of ConstructionReductionBO))
            _reductions = value
        End Set
    End Property
    Private _abatement As Boolean
    Public Property Abatement() As Boolean
        Get
            Return _abatement
        End Get
        Set(ByVal value As Boolean)
            _abatement = value
        End Set
    End Property
    Private _raisedabatement As Boolean
    Public Property RaisedAbatement() As Boolean
        Get
            Return _raisedabatement
        End Get
        Set(ByVal value As Boolean)
            _raisedabatement = value
        End Set
    End Property
    Private _oneandownhome As Boolean
    Public Property OneAndOwnHome() As Boolean
        Get
            Return _oneandownhome
        End Get
        Set(ByVal value As Boolean)
            _oneandownhome = value
        End Set
    End Property

End Class

'INVOICING

Public Class ProjectInvoicingModel
    Public Sub New()
        _clientaccounts = New List(Of ClientAccountWithInvoicableBO)
        _clientChangeOrders = New List(Of ClientAccountWithInvoicableChangeOrderBO)
        _clientUtilityCosts = New List(Of ClientUtilityCostBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _clientaccounts As List(Of ClientAccountWithInvoicableBO)
    Public Property ClientAccounts() As List(Of ClientAccountWithInvoicableBO)
        Get
            Return _clientaccounts
        End Get
        Set(ByVal value As List(Of ClientAccountWithInvoicableBO))
            _clientaccounts = value
        End Set
    End Property
    Private _clientChangeOrders As List(Of ClientAccountWithInvoicableChangeOrderBO)
    Public Property ClientChangeOrders() As List(Of ClientAccountWithInvoicableChangeOrderBO)
        Get
            Return _clientChangeOrders
        End Get
        Set(ByVal value As List(Of ClientAccountWithInvoicableChangeOrderBO))
            _clientChangeOrders = value
        End Set
    End Property
    Private _clientUtilityCosts As List(Of ClientUtilityCostBO)
    Public Property ClientUtilityCosts() As List(Of ClientUtilityCostBO)
        Get
            Return _clientUtilityCosts
        End Get
        Set(ByVal value As List(Of ClientUtilityCostBO))
            _clientUtilityCosts = value
        End Set
    End Property



End Class
Public Class ProjectPaymentStagesModel
    Public Sub New()
        _groups = New List(Of ProjectPaymentGroupBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _groups As List(Of ProjectPaymentGroupBO)
    Public Property Groups() As List(Of ProjectPaymentGroupBO)
        Get
            Return _groups
        End Get
        Set(ByVal value As List(Of ProjectPaymentGroupBO))
            _groups = value
        End Set
    End Property
End Class
Public Class ProjectPaymentStagesAddUpdateModel
    Public Sub New()
        _group = New ProjectPaymentGroupBO
        _stages = New List(Of ProjectPaymentStageBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property

    Private _group As ProjectPaymentGroupBO
    Public Property Group() As ProjectPaymentGroupBO
        Get
            Return _group
        End Get
        Set(ByVal value As ProjectPaymentGroupBO)
            _group = value
        End Set
    End Property
    Private _stages As List(Of ProjectPaymentStageBO)
    Public Property Stages() As List(Of ProjectPaymentStageBO)
        Get
            Return _stages
        End Get
        Set(ByVal value As List(Of ProjectPaymentStageBO))
            _stages = value
        End Set
    End Property

End Class
Public Class ProjectPaymentGroupLinkModel
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectName As String
    Public Property ProjectName() As String
        Get
            Return _projectName
        End Get
        Set(ByVal value As String)
            _projectName = value
        End Set
    End Property
    Private _units As List(Of UnitBO)
    Public Property Units() As List(Of UnitBO)
        Get
            Return _units
        End Get
        Set(ByVal value As List(Of UnitBO))
            _units = value
        End Set
    End Property
    Private _paymentgroups As List(Of IdNameBO)
    Public Property PaymentGroups() As List(Of IdNameBO)
        Get
            Return _paymentgroups
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _paymentgroups = value
        End Set
    End Property
End Class
Public Class AddStageDocModel
    Public Sub New()
        _doc = New ProjectDocBO
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _stageid As Integer
    Public Property StageId() As Integer
        Get
            Return _stageid
        End Get
        Set(ByVal value As Integer)
            _stageid = value
        End Set
    End Property
    Private _doc As ProjectDocBO
    Public Property Doc() As ProjectDocBO
        Get
            Return _doc
        End Get
        Set(ByVal value As ProjectDocBO)
            _doc = value
        End Set
    End Property
End Class
Public Class SelectStageDocModel
    Public Sub New()
        _docs = New List(Of IdNameBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _stageid As Integer
    Public Property StageId() As Integer
        Get
            Return _stageid
        End Get
        Set(ByVal value As Integer)
            _stageid = value
        End Set
    End Property
    Private _docid As Integer
    Public Property DocId() As Integer
        Get
            Return _docid
        End Get
        Set(ByVal value As Integer)
            _docid = value
        End Set
    End Property
    Private _docs As List(Of IdNameBO)
    Public Property Docs() As List(Of IdNameBO)
        Get
            Return _docs
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _docs = value
        End Set
    End Property
End Class
Public Class DeleteStageDocModel
    Public Sub New()
        _doc = New ProjectDocBO
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _stageid As Integer
    Public Property StageId() As Integer
        Get
            Return _stageid
        End Get
        Set(ByVal value As Integer)
            _stageid = value
        End Set
    End Property
    Private _doc As ProjectDocBO
    Public Property Doc() As ProjectDocBO
        Get
            Return _doc
        End Get
        Set(ByVal value As ProjectDocBO)
            _doc = value
        End Set
    End Property
End Class
Public Class ModalPrintInvoiceListModel
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _clients As List(Of IdNameBO)
    Public Property Client() As List(Of IdNameBO)
        Get
            Return _clients
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _clients = value
        End Set
    End Property
    Private _selectedclient As Integer
    Public Property SelectedClient() As Integer
        Get
            Return _selectedclient
        End Get
        Set(ByVal value As Integer)
            _selectedclient = value
        End Set
    End Property

End Class
Public Class PrintInvoiceListModel
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _client As ClientAccountBO
    Public Property Client() As ClientAccountBO
        Get
            Return _client
        End Get
        Set(ByVal value As ClientAccountBO)
            _client = value
        End Set
    End Property
    Private _invoices As List(Of InvoiceBO)
    Public Property Invoices() As List(Of InvoiceBO)
        Get
            Return _invoices
        End Get
        Set(ByVal value As List(Of InvoiceBO))
            _invoices = value
        End Set
    End Property
    Private _unitswithstages As List(Of UnitWithStagesBO)
    Public Property UnitsWithStages() As List(Of UnitWithStagesBO)
        Get
            Return _unitswithstages
        End Get
        Set(ByVal value As List(Of UnitWithStagesBO))
            _unitswithstages = value
        End Set
    End Property
    Private _salessettings As ProjectSalesSettingsBO
    Public Property SalesSettings() As ProjectSalesSettingsBO
        Get
            Return _salessettings
        End Get
        Set(ByVal value As ProjectSalesSettingsBO)
            _salessettings = value
        End Set
    End Property

End Class

'CONTRACTS

Public Class ProjectContractsModel
    Public Sub New()
        _contracts = New List(Of ContractBO)
        _budgetActivities = New List(Of BudgetActivityBO)
        _IncommingInvoicesActivities = New List(Of IncommingInvoiceActivityBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _contracts As List(Of ContractBO)
    Public Property Contracts() As List(Of ContractBO)
        Get
            Return _contracts
        End Get
        Set(ByVal value As List(Of ContractBO))
            _contracts = value
        End Set
    End Property
    Private _activityGroups As List(Of ActivityGroupBO)
    Public Property ActivityGroups() As List(Of ActivityGroupBO)
        Get
            Return _activityGroups
        End Get
        Set(ByVal value As List(Of ActivityGroupBO))
            _activityGroups = value
        End Set
    End Property
    Private _budgetActivities As List(Of BudgetActivityBO)
    Public Property BudgetActivities() As List(Of BudgetActivityBO)
        Get
            Return _budgetActivities
        End Get
        Set(ByVal value As List(Of BudgetActivityBO))
            _budgetActivities = value
        End Set
    End Property
    Private _IncommingInvoicesActivities As List(Of IncommingInvoiceActivityBO)
    Public Property IncommingInvoicesActivities() As List(Of IncommingInvoiceActivityBO)
        Get
            Return _IncommingInvoicesActivities
        End Get
        Set(ByVal value As List(Of IncommingInvoiceActivityBO))
            _IncommingInvoicesActivities = value
        End Set
    End Property

End Class
Public Class ProjectAddContractModel
    Public Sub New()
        _contract = New ContractBO
        _companies = New List(Of IdNameBO)
        _activities = New List(Of ActivityBO)
        _contractactivities = New List(Of ContractActivityBO)
        _insurance = New InsuranceBO
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _contract As ContractBO
    Public Property Contract() As ContractBO
        Get
            Return _contract
        End Get
        Set(ByVal value As ContractBO)
            _contract = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _companies As List(Of IdNameBO)
    <Display(Name:="Bedrijfsnaam")>
    Public Property Companies() As List(Of IdNameBO)
        Get
            Return _companies
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _companies = value
        End Set
    End Property
    Private _selectedCompany As Integer
    Public Property SelectedCompany() As Integer
        Get
            Return _selectedCompany
        End Get
        Set(ByVal value As Integer)
            _selectedCompany = value
        End Set
    End Property
    Private _activities As List(Of ActivityBO)
    <Display(Name:="Activiteiten")>
    Public Property Activities() As List(Of ActivityBO)
        Get
            Return _activities
        End Get
        Set(ByVal value As List(Of ActivityBO))
            _activities = value
        End Set
    End Property
    Private _selectedActivities As List(Of Integer)
    Public Property SelectedActivities() As List(Of Integer)
        Get
            Return _selectedActivities
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedActivities = value
        End Set
    End Property
    Private _selectedActivitiesaddorders As List(Of Integer)
    Public Property SelectedActivitiesAddOrders() As List(Of Integer)
        Get
            Return _selectedActivitiesaddorders
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedActivitiesaddorders = value
        End Set
    End Property
    Private _insurance As InsuranceBO
    Public Property Insurance() As InsuranceBO
        Get
            Return _insurance
        End Get
        Set(ByVal value As InsuranceBO)
            _insurance = value
        End Set
    End Property
    Private _insurancecompanies As List(Of IdNameBO)
    <Display(Name:="Maatschappij")>
    Public Property InsuranceCompanies() As List(Of IdNameBO)
        Get
            Return _insurancecompanies
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _insurancecompanies = value
        End Set
    End Property
    Private _contractactivities As List(Of ContractActivityBO)
    Public Property ContractActivities() As List(Of ContractActivityBO)
        Get
            Return _contractactivities
        End Get
        Set(ByVal value As List(Of ContractActivityBO))
            _contractactivities = value
        End Set
    End Property

End Class
Public Class ProjectCalculationSettings
    Public Sub New()
        _budgetActivities = New List(Of BudgetActivityBO)
        _listactivities = New List(Of IdNameBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _budgetActivities As List(Of BudgetActivityBO)
    Public Property BudgetActivities() As List(Of BudgetActivityBO)
        Get
            Return _budgetActivities
        End Get
        Set(ByVal value As List(Of BudgetActivityBO))
            _budgetActivities = value
        End Set
    End Property
    Private _activityGroups As List(Of ActivityGroupBO)
    Public Property ActivityGroups() As List(Of ActivityGroupBO)
        Get
            Return _activityGroups
        End Get
        Set(ByVal value As List(Of ActivityGroupBO))
            _activityGroups = value
        End Set
    End Property
    Private _listactivities As List(Of IdNameBO)
    Public Property ListActivities() As List(Of IdNameBO)
        Get
            Return _listactivities
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _listactivities = value
        End Set
    End Property
    Private _selectedActivities As List(Of Integer)
    Public Property SelectedActivities() As List(Of Integer)
        Get
            Return _selectedActivities
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedActivities = value
        End Set
    End Property

End Class

Public Class ProjectChangeOrderModel
    Public Sub New()
        _changeorders = New List(Of ChangeOrderBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _changeorders As List(Of ChangeOrderBO)
    Public Property ChangeOrders() As List(Of ChangeOrderBO)
        Get
            Return _changeorders
        End Get
        Set(ByVal value As List(Of ChangeOrderBO))
            _changeorders = value
        End Set
    End Property
    Private _vatPercentage As Decimal
    <UIHint("Percentage")>
    Public Property VatPercentage() As Decimal
        Get
            Return _vatPercentage
        End Get
        Set(ByVal value As Decimal)
            _vatPercentage = value
        End Set
    End Property
End Class
Public Class ProjectChangeOrderAddUpdateModel
    Public Sub New()
        _clientaccounts = New List(Of IdNameBO)
        _projectContractActivities = New List(Of IdNameBO)
        _changeorder = New ChangeOrderBO
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _clientaccounts As List(Of IdNameBO)
    <Display(Name:="Klanten")>
    Public Property ClientAccounts() As List(Of IdNameBO)
        Get
            Return _clientaccounts
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _clientaccounts = value
        End Set
    End Property
    Private _projectContractActivities As List(Of IdNameBO)
    <Display(Name:="Contracten")>
    Public Property ProjectContractActivities() As List(Of IdNameBO)
        Get
            Return _projectContractActivities
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _projectContractActivities = value
        End Set
    End Property
    Private _selectedcontractactivity As Integer
    Public Property SelectedContractActivity() As Integer
        Get
            Return _selectedcontractactivity
        End Get
        Set(ByVal value As Integer)
            _selectedcontractactivity = value
        End Set
    End Property
    Private _changeorder As ChangeOrderBO
    Public Property ChangeOrder() As ChangeOrderBO
        Get
            Return _changeorder
        End Get
        Set(ByVal value As ChangeOrderBO)
            _changeorder = value
        End Set
    End Property
End Class
Public Class ProjectChangeOrderExportModel
    Public Sub New()
        _project = New ProjectBO
        _changeorder = New ChangeOrderBO
        _projectsalessettings = New ProjectSalesSettingsBO
        _clientaccount = New ClientAccountBO
    End Sub
    Private _project As ProjectBO
    Public Property Project() As ProjectBO
        Get
            Return _project
        End Get
        Set(ByVal value As ProjectBO)
            _project = value
        End Set
    End Property
    Private _projectsalessettings As ProjectSalesSettingsBO
    Public Property ProjectSalesSettings() As ProjectSalesSettingsBO
        Get
            Return _projectsalessettings
        End Get
        Set(ByVal value As ProjectSalesSettingsBO)
            _projectsalessettings = value
        End Set
    End Property
    Private _clientaccount As ClientAccountBO
    Public Property ClientAccount() As ClientAccountBO
        Get
            Return _clientaccount
        End Get
        Set(ByVal value As ClientAccountBO)
            _clientaccount = value
        End Set
    End Property
    Private _units As String
    Public Property Units() As String
        Get
            Return _units
        End Get
        Set(ByVal value As String)
            _units = value
        End Set
    End Property

    Private _changeorder As ChangeOrderBO
    Public Property ChangeOrder() As ChangeOrderBO
        Get
            Return _changeorder
        End Get
        Set(ByVal value As ChangeOrderBO)
            _changeorder = value
        End Set
    End Property

End Class

Public Class ProjectIncommingInvoiceAddUpdateModel
    Public Sub New()
        _projectContracts = New List(Of IdNameBO)
        _incomminginvoice = New IncommingInvoiceBO
        _activities = New List(Of Select2DTO)
        _listactivities = New List(Of IdNameBO)
        _selectedActivities = New List(Of Integer)
    End Sub
    Private _type As Integer
    Public Property Type() As Integer
        Get
            Return _type
        End Get
        Set(ByVal value As Integer)
            _type = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _incomminginvoice As IncommingInvoiceBO
    Public Property IncommingInvoice() As IncommingInvoiceBO
        Get
            Return _incomminginvoice
        End Get
        Set(ByVal value As IncommingInvoiceBO)
            _incomminginvoice = value
        End Set
    End Property
    Private _projectContracts As List(Of IdNameBO)
    <Display(Name:="Contracten")>
    Public Property ProjectContracts() As List(Of IdNameBO)
        Get
            Return _projectContracts
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _projectContracts = value
        End Set
    End Property
    Private _selectedcontract As Integer
    Public Property SelectedContract() As Integer
        Get
            Return _selectedcontract
        End Get
        Set(ByVal value As Integer)
            _selectedcontract = value
        End Set
    End Property
    Private _activities As List(Of Select2DTO)
    <Display(Name:="Activiteiten")>
    Public Property Activities() As List(Of Select2DTO)
        Get
            Return _activities
        End Get
        Set(ByVal value As List(Of Select2DTO))
            _activities = value
        End Set
    End Property
    Private _listactivities As List(Of IdNameBO)
    Public Property ListActivities() As List(Of IdNameBO)
        Get
            Return _listactivities
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _listactivities = value
        End Set
    End Property
    Private _selectedActivities As List(Of Integer)
    Public Property SelectedActivities() As List(Of Integer)
        Get
            Return _selectedActivities
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedActivities = value
        End Set
    End Property
    ReadOnly Property TotaalPrijsLijnen() As Decimal
        Get
            Return IncommingInvoice.Details.Sum(Function(m) m.Price)
        End Get
    End Property
End Class

'RECALCULATIOn
Public Class ProjectRecalculationDetailModel
    Public Sub New()
        _IncommingInvoicesActivities = New List(Of IncommingInvoiceActivityBO)
    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _activity As ActivityBO
    Public Property Activity() As ActivityBO
        Get
            Return _activity
        End Get
        Set(ByVal value As ActivityBO)
            _activity = value
        End Set
    End Property

    Private _IncommingInvoicesActivities As List(Of IncommingInvoiceActivityBO)
    Public Property IncommingInvoicesActivities() As List(Of IncommingInvoiceActivityBO)
        Get
            Return _IncommingInvoicesActivities
        End Get
        Set(ByVal value As List(Of IncommingInvoiceActivityBO))
            _IncommingInvoicesActivities = value
        End Set
    End Property
    Private _ContractsWithoutInvoices As List(Of ContractBO)
    Public Property ContractsWithoutInvoices() As List(Of ContractBO)
        Get
            Return _ContractsWithoutInvoices
        End Get
        Set(ByVal value As List(Of ContractBO))
            _ContractsWithoutInvoices = value
        End Set
    End Property

    Private _ContractActivities As List(Of ContractActivityBO)
    Public Property ContractActivities() As List(Of ContractActivityBO)
        Get
            Return _ContractActivities
        End Get
        Set(ByVal value As List(Of ContractActivityBO))
            _ContractActivities = value
        End Set
    End Property

End Class

'INSURANCES

Public Class DetailInsurancesModel
    Public Sub New()
        _insurances = New List(Of InsuranceBO)
    End Sub
    Private _insurances As List(Of InsuranceBO)
    Public Property Insurances() As List(Of InsuranceBO)
        Get
            Return _insurances
        End Get
        Set(ByVal value As List(Of InsuranceBO))
            _insurances = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
End Class
Public Class ProjectAddInsurancesModel
    Public Sub New()
        _insurance = New InsuranceBO
        _brokers = New List(Of IdNameBO)
        _companies = New List(Of IdNameBO)

    End Sub
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _insurance As InsuranceBO
    Public Property Insurance() As InsuranceBO
        Get
            Return _insurance
        End Get
        Set(ByVal value As InsuranceBO)
            _insurance = value
        End Set
    End Property
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
    Private _companies As List(Of IdNameBO)
    <Display(Name:="Maatschappij")>
    Public Property Companies() As List(Of IdNameBO)
        Get
            Return _companies
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _companies = value
        End Set
    End Property
    Private _brokers As List(Of IdNameBO)
    <Display(Name:="Makelaar")>
    Public Property Brokers() As List(Of IdNameBO)
        Get
            Return _brokers
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _brokers = value
        End Set
    End Property




End Class

'CONTRACTS
Public Class DetailContractsModel
    Public Sub New()
        _contracts = New List(Of ContractBO)
    End Sub
    Private _contracts As List(Of ContractBO)
    Public Property Contracts() As List(Of ContractBO)
        Get
            Return _contracts
        End Get
        Set(ByVal value As List(Of ContractBO))
            _contracts = value
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
    Private _projectname As String
    Public Property ProjectName() As String
        Get
            Return _projectname
        End Get
        Set(ByVal value As String)
            _projectname = value
        End Set
    End Property
End Class

'SHARED
Public Class Select2DTO
    ' as select2 is formed like id and text so we used DTO
    Public Property id() As Integer
        Get
            Return m_id
        End Get
        Set(value As Integer)
            m_id = value
        End Set
    End Property
    Private m_id As Integer
    Public Property text() As String
        Get
            Return m_text
        End Get
        Set(value As String)
            m_text = value
        End Set
    End Property
    Private m_text As String
End Class