Imports BO
Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc

Public Class ClientModel
    Public Sub New()
        _clientAccount = New ClientAccountBO
        _unitsgrouped = New List(Of GroupUnitsBO)
        _gifts = New List(Of ClientGiftBO)
        _poas = New List(Of ClientPoaBO)
        _changeorders = New List(Of ChangeOrderBO)
    End Sub
    Private _clientAccount As ClientAccountBO
    Public Property Client() As ClientAccountBO
        Get
            Return _clientAccount
        End Get
        Set(ByVal value As ClientAccountBO)
            _clientAccount = value
        End Set
    End Property
    Private _unitsgrouped As List(Of GroupUnitsBO)
    Public Property UnitsGrouped() As List(Of GroupUnitsBO)
        Get
            Return _unitsgrouped
        End Get
        Set(ByVal value As List(Of GroupUnitsBO))
            _unitsgrouped = value
        End Set
    End Property
    Private _gifts As List(Of ClientGiftBO)
    Public Property Gifts() As List(Of ClientGiftBO)
        Get
            Return _gifts
        End Get
        Set(ByVal value As List(Of ClientGiftBO))
            _gifts = value
        End Set
    End Property
    Private _poas As List(Of ClientPoaBO)
    Public Property Poas() As List(Of ClientPoaBO)
        Get
            Return _poas
        End Get
        Set(ByVal value As List(Of ClientPoaBO))
            _poas = value
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
    Private _folder As String
    Public Property Folder() As String
        Get
            Return _folder
        End Get
        Set(ByVal value As String)
            _folder = value
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
    Private _unitswithstages As List(Of UnitWithStagesBO)
    Public Property UnitsWithStages() As List(Of UnitWithStagesBO)
        Get
            Return _unitswithstages
        End Get
        Set(ByVal value As List(Of UnitWithStagesBO))
            _unitswithstages = value
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
    Private _latestDocs As List(Of ProjectDocBO)
    Public Property LatestDocs() As List(Of ProjectDocBO)
        Get
            Return _latestDocs
        End Get
        Set(ByVal value As List(Of ProjectDocBO))
            _latestDocs = value
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
End Class
Public Class EditClientModel
    Public Sub New()
        _clientAccount = New ClientAccountBO
        _units = New List(Of UnitBO)
        _ownertypes = New List(Of IdNameBO)
        _selectedPostalCode = New PostalcodeModel
        _selectedInvoicePostalCode = New PostalcodeModel
        _gifts = New List(Of ClientGiftBO)
        _poas = New List(Of ClientPoaBO)
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
    Private _clientAccount As ClientAccountBO
    Public Property Client() As ClientAccountBO
        Get
            Return _clientAccount
        End Get
        Set(ByVal value As ClientAccountBO)
            _clientAccount = value
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
    Private _ownertypes As List(Of IdNameBO)
    Public Property OwnerTypes() As List(Of IdNameBO)
        Get
            Return _ownertypes
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _ownertypes = value
        End Set
    End Property
    Private _gifts As List(Of ClientGiftBO)
    Public Property Gifts() As List(Of ClientGiftBO)
        Get
            Return _gifts
        End Get
        Set(ByVal value As List(Of ClientGiftBO))
            _gifts = value
        End Set
    End Property
    Private _poas As List(Of ClientPoaBO)
    Public Property Poas() As List(Of ClientPoaBO)
        Get
            Return _poas
        End Get
        Set(ByVal value As List(Of ClientPoaBO))
            _poas = value
        End Set
    End Property
    Private _selectedPostalCode As PostalcodeModel
    <UIHint("Postalcode")>
    Public Property SelectedPostalcode() As PostalcodeModel
        Get
            Return _selectedPostalCode
        End Get
        Set(ByVal value As PostalcodeModel)
            _selectedPostalCode = value
        End Set
    End Property
    Private _selectedInvoicePostalCode As PostalcodeModel
    <UIHint("Postalcode")>
    Public Property SelectedInvoicePostalcode() As PostalcodeModel
        Get
            Return _selectedInvoicePostalCode
        End Get
        Set(ByVal value As PostalcodeModel)
            _selectedInvoicePostalCode = value
        End Set
    End Property
    Private _iscompany As Boolean
    <Display(Name:="Eigenaar is een onderneming")>
    Public Property IsCompany() As Boolean
        Get
            If Not Client Is Nothing Then
                If Client.CompanyName IsNot Nothing Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False
            End If

        End Get
        Set(ByVal value As Boolean)
            _iscompany = value
        End Set
    End Property
End Class
Public Class AddClientAccountModel
    Public Sub New()
        _countries = New List(Of IdNameBO)
        _clientaccount = New ClientAccountBO
        _ownertypes = New List(Of IdNameBO)
        _availableunits = New List(Of IdNameBO)
        _selectedUnits = New List(Of Integer)
        _addedUnits = New List(Of UnitBO)

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
    Private _clientaccount As ClientAccountBO
    Public Property ClientAccount() As ClientAccountBO
        Get
            Return _clientaccount
        End Get
        Set(ByVal value As ClientAccountBO)
            _clientaccount = value
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
    Private _ownertypes As List(Of IdNameBO)
    Public Property OwnerTypes() As List(Of IdNameBO)
        Get
            Return _ownertypes
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _ownertypes = value
        End Set
    End Property
    Private _availableunits As List(Of IdNameBO)
    Public Property AvailableUnits() As List(Of IdNameBO)
        Get
            Return _availableunits
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _availableunits = value
        End Set
    End Property
    Private _selectedcoownertype As Integer
    Public Property SelectedCoOwnerType() As Integer
        Get
            Return _selectedcoownertype
        End Get
        Set(ByVal value As Integer)
            _selectedcoownertype = value
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

    Private _selectedInvoiceCountry As Integer
    Public Property SelectedInvoiceCountry() As Integer
        Get
            Return _selectedInvoiceCountry
        End Get
        Set(ByVal value As Integer)
            _selectedInvoiceCountry = value
        End Set
    End Property
    Private _selectedInvoicePostalCode As Integer
    Public Property SelectedInvoicePostalcode() As Integer
        Get
            Return _selectedInvoicePostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedInvoicePostalCode = value
        End Set
    End Property
    Private _selectedCoOwnerCountry As Integer
    Public Property SelectedCoOwnerCountry() As Integer
        Get
            Return _selectedCoOwnerCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCoOwnerCountry = value
        End Set
    End Property
    Private _selectedCoOwnerPostalCode As Integer
    Public Property SelectedCoOwnerPostalCode() As Integer
        Get
            Return _selectedCoOwnerPostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedCoOwnerPostalCode = value
        End Set
    End Property
    Private _selectedCoOwnerInvoiceCountry As Integer
    Public Property SelectedCoOwnerInvoiceCountry() As Integer
        Get
            Return _selectedCoOwnerInvoiceCountry
        End Get
        Set(ByVal value As Integer)
            _selectedCoOwnerInvoiceCountry = value
        End Set
    End Property
    Private _selectedCoOwnerInvoicePostalCode As Integer
    Public Property SelectedCoOwnerInvoicePostalCode() As Integer
        Get
            Return _selectedCoOwnerInvoicePostalCode
        End Get
        Set(ByVal value As Integer)
            _selectedCoOwnerInvoicePostalCode = value
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
    Private _addedUnits As List(Of UnitBO)
    Public Property AddedUnits() As List(Of UnitBO)
        Get
            Return _addedUnits
        End Get
        Set(ByVal value As List(Of UnitBO))
            _addedUnits = value
        End Set
    End Property
    Private _salutations As Salutation
    Public Property Salutations() As Salutation
        Get
            Return _salutations
        End Get
        Set(ByVal value As Salutation)
            _salutations = value
        End Set
    End Property

End Class
Public Class AddUpdateClientCoOwnerModel
    Public Sub New()

        _coowner = New ClientContactBO
        _ownertypes = New List(Of IdNameBO)
        _selectedCoOwnerPostalCode = New PostalcodeModel
        _selectedCoOwnerInvoicePostalCode = New PostalcodeModel
    End Sub
    Private _coowner As ClientContactBO
    Public Property CoOwner() As ClientContactBO
        Get
            Return _coowner
        End Get
        Set(ByVal value As ClientContactBO)
            _coowner = value
        End Set
    End Property
    Private _selectedcoownertype As Integer
    Public Property SelectedCoOwnerType() As Integer
        Get
            Return _selectedcoownertype
        End Get
        Set(ByVal value As Integer)
            _selectedcoownertype = value
        End Set
    End Property


    Private _ownertypes As List(Of IdNameBO)
    Public Property OwnerTypes() As List(Of IdNameBO)
        Get
            Return _ownertypes
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _ownertypes = value
        End Set
    End Property

    Private _selectedCoOwnerPostalCode As PostalcodeModel
    <UIHint("Postalcode")>
    Public Property SelectedCoOwnerPostalCode() As PostalcodeModel
        Get
            Return _selectedCoOwnerPostalCode
        End Get
        Set(ByVal value As PostalcodeModel)
            _selectedCoOwnerPostalCode = value
        End Set
    End Property

    Private _selectedCoOwnerInvoicePostalCode As PostalcodeModel
    <UIHint("Postalcode")>
    Public Property SelectedCoOwnerInvoicePostalCode() As PostalcodeModel
        Get
            Return _selectedCoOwnerInvoicePostalCode
        End Get
        Set(ByVal value As PostalcodeModel)
            _selectedCoOwnerInvoicePostalCode = value
        End Set
    End Property
    Private _iscompany As Boolean
    <Display(Name:="Mede-eigenaar is een onderneming")>
    Public Property IsCompany() As Boolean
        Get
            If CoOwner.CompanyName IsNot Nothing Then
                Return True
            Else
                Return False
            End If
        End Get
        Set(ByVal value As Boolean)
            _iscompany = value
        End Set
    End Property
    Private _maxcoownerpercentage As Decimal
    Public Property MaxCoOwnerPercentage() As Decimal
        Get
            Return _maxcoownerpercentage
        End Get
        Set(ByVal value As Decimal)
            _maxcoownerpercentage = value
        End Set
    End Property

End Class
Public Class AddUnitToClientModel
    Public Sub New()
        _unit = New UnitBO
        _availableunits = New List(Of IdNameBO)
        _availableprojects = New List(Of IdNameBO)
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

    Private _selectedunit As Integer
    Public Property SelectedUnit() As Integer
        Get
            Return _selectedunit
        End Get
        Set(ByVal value As Integer)
            _selectedunit = value
        End Set
    End Property
    Private _availableunits As List(Of IdNameBO)
    Public Property AvailableUnits() As List(Of IdNameBO)
        Get
            Return _availableunits
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _availableunits = value
        End Set
    End Property
    Private _selectedproject As Integer
    Public Property SelectedProject() As Integer
        Get
            Return _selectedproject
        End Get
        Set(ByVal value As Integer)
            _selectedproject = value
        End Set
    End Property
    Private _availableprojects As List(Of IdNameBO)
    Public Property AvailableProjects() As List(Of IdNameBO)
        Get
            Return _availableprojects
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _availableprojects = value
        End Set
    End Property
End Class

Public Class AddGiftToClientModel
    Public Sub New()
        _gift = New ClientGiftBO
        _listactivities = New List(Of IdNameBO)
        _selectedActivities = New List(Of Integer)
    End Sub
    Private _gift As ClientGiftBO
    Public Property Gift() As ClientGiftBO
        Get
            Return _gift
        End Get
        Set(ByVal value As ClientGiftBO)
            _gift = value
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
Public Class AddPoaToClientModel
    Public Sub New()
        _poa = New ClientPoaBO
        _listactivities = New List(Of IdNameBO)
        _selectedActivities = New List(Of Integer)
    End Sub
    Private _poa As ClientPoaBO
    Public Property POA() As ClientPoaBO
        Get
            Return _poa
        End Get
        Set(ByVal value As ClientPoaBO)
            _poa = value
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
Public Class DetailClientsModel
    Public Sub New()
        _clientaccounts = New List(Of ClientAccountWithUnitsBO)
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

    Private _clientaccounts As List(Of ClientAccountWithUnitsBO)
    Public Property ClientAccounts() As List(Of ClientAccountWithUnitsBO)
        Get
            Return _clientaccounts
        End Get
        Set(ByVal value As List(Of ClientAccountWithUnitsBO))
            _clientaccounts = value
        End Set
    End Property


End Class
Public Class ClientCalendarModel
    Public Sub New()
        _days = New List(Of CalendarDayBO)
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
    Private _client As ClientAccountBO
    Public Property Client() As ClientAccountBO
        Get
            Return _client
        End Get
        Set(ByVal value As ClientAccountBO)
            _client = value
        End Set
    End Property
    Private _executiondays As Integer
    <Display(Name:="Uitvoeringstermijn")>
    Public Property ExecutionDays() As Integer
        Get
            Return _executiondays
        End Get
        Set(ByVal value As Integer)
            _executiondays = value
        End Set
    End Property
    Private _startdate As Date
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Aanvangsdatum")>
    Public Property Startdate() As Date
        Get
            Return _startdate
        End Get
        Set(ByVal value As Date)
            _startdate = value
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
    Private _days As List(Of CalendarDayBO)
    Public Property Days() As List(Of CalendarDayBO)
        Get
            Return _days
        End Get
        Set(ByVal value As List(Of CalendarDayBO))
            _days = value
        End Set
    End Property


End Class
Public Class DetailClientsExportModel
    Public Sub New()
        _clientaccounts = New List(Of ClientAccountWithUnitsBO)
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

    Private _clientaccounts As List(Of ClientAccountWithUnitsBO)
    Public Property ClientAccounts() As List(Of ClientAccountWithUnitsBO)
        Get
            Return _clientaccounts
        End Get
        Set(ByVal value As List(Of ClientAccountWithUnitsBO))
            _clientaccounts = value
        End Set
    End Property
    Private _unitTypes As List(Of UnitTypeBO)
    Public Property UnitTypes() As List(Of UnitTypeBO)
        Get
            Return _unitTypes
        End Get
        Set(ByVal value As List(Of UnitTypeBO))
            _unitTypes = value
        End Set
    End Property


End Class
Public Class DetailClientsGiftsModel
    Public Sub New()
        _clientgifts = New List(Of ClientGiftWithAccountDetailsBO)
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

    Private _clientgifts As List(Of ClientGiftWithAccountDetailsBO)
    Public Property ClientGifts() As List(Of ClientGiftWithAccountDetailsBO)
        Get
            Return _clientgifts
        End Get
        Set(ByVal value As List(Of ClientGiftWithAccountDetailsBO))
            _clientgifts = value
        End Set
    End Property
    Private _selectedactivities As List(Of ActivityBO)
    Public Property SelectedActivities() As List(Of ActivityBO)
        Get
            Return _selectedactivities
        End Get
        Set(ByVal value As List(Of ActivityBO))
            _selectedactivities = value
        End Set
    End Property


End Class
Public Class DetailClientsPoasModel
    Public Sub New()
        _clientpoas = New List(Of ClientPoaWithAccountDetailsBO)
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

    Private _clientpoas As List(Of ClientPoaWithAccountDetailsBO)
    Public Property ClientPoas() As List(Of ClientPoaWithAccountDetailsBO)
        Get
            Return _clientpoas
        End Get
        Set(ByVal value As List(Of ClientPoaWithAccountDetailsBO))
            _clientpoas = value
        End Set
    End Property
    Private _selectedactivities As List(Of ActivityBO)
    Public Property SelectedActivities() As List(Of ActivityBO)
        Get
            Return _selectedactivities
        End Get
        Set(ByVal value As List(Of ActivityBO))
            _selectedactivities = value
        End Set
    End Property


End Class

Public Class DetailInvoicingModel
    Public Sub New()
        _invoices = New List(Of InvoiceBO)
        _client = New ClientAccountBO
    End Sub
    Private _invoices As List(Of InvoiceBO)
    Public Property Invoices() As List(Of InvoiceBO)
        Get
            Return _invoices
        End Get
        Set(ByVal value As List(Of InvoiceBO))
            _invoices = value
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
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
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
Public Class ExportGiftsToPdfModel
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
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
    Private _selectedactivities As List(Of Integer)
    Public Property SelectedActivities() As List(Of Integer)
        Get
            Return _selectedactivities
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedactivities = value
        End Set
    End Property
End Class
Public Class ExportPoasToPdfModel
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
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
    Private _selectedactivities As List(Of Integer)
    Public Property SelectedActivities() As List(Of Integer)
        Get
            Return _selectedactivities
        End Get
        Set(ByVal value As List(Of Integer))
            _selectedactivities = value
        End Set
    End Property
End Class
'Opleveringsgegevens opslaan
Public Class DeliveryModel
    Private _clientid As Integer
    Public Property ClientId() As Integer
        Get
            Return _clientid
        End Get
        Set(ByVal value As Integer)
            _clientid = value
        End Set
    End Property
    Private _projectid As String
    Public Property ProjectId() As String
        Get
            Return _projectid
        End Get
        Set(ByVal value As String)
            _projectid = value
        End Set
    End Property
    Private _deliverydate As Date?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <Display(Name:="Opleverdatum")>
    Public Property DeliveryDate() As Date?
        Get
            Return _deliverydate
        End Get
        Set(ByVal value As Date?)
            _deliverydate = value
        End Set
    End Property
    Private _deliverydoc As String
    Public Property DeliveryDoc() As String
        Get
            Return _deliverydoc
        End Get
        Set(ByVal value As String)
            _deliverydoc = value
        End Set
    End Property
End Class
