Imports System.ComponentModel.DataAnnotations

Public Class InsuranceBO
    Public Sub New()
        _insuranceCompany = New InsuranceCompanyBO
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
    Private _contractactivityID As Integer
    Public Property ContractActivityID() As Integer
        Get
            Return _contractactivityID
        End Get
        Set(ByVal value As Integer)
            _contractactivityID = value
        End Set
    End Property
    Private _insurancebrokername As String
    <Display(Name:="Makelaar")>
    Public Property InsuranceBrokerName() As String
        Get
            Return _insurancebrokername
        End Get
        Set(ByVal value As String)
            _insurancebrokername = value
        End Set
    End Property
    Private _insuranceCompany As InsuranceCompanyBO
    <Display(Name:="Maatschappij")>
    Public Property InsuranceCompany() As InsuranceCompanyBO
        Get
            Return _insuranceCompany
        End Get
        Set(ByVal value As InsuranceCompanyBO)
            _insuranceCompany = value
        End Set
    End Property
    Private _projectid As Integer
    Public Property ProjectID() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    'Startdatum
    Private _startdate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Startdatum")>
    Public Property Startdate() As DateOnly?
        Get
            Return _startdate
        End Get
        Set(ByVal value As DateOnly?)
            _startdate = value
        End Set
    End Property
    Private _period As Integer?
    <Display(Name:="Termijn")>
    Public Property Period() As Integer?
        Get
            Return _period
        End Get
        Set(ByVal value As Integer?)
            _period = value
        End Set
    End Property
    Private _extensionperiod As Integer?
    <Display(Name:="Verlenging")>
    Public Property ExtensionPeriod() As Integer?
        Get
            Return _extensionperiod
        End Get
        Set(ByVal value As Integer?)
            _extensionperiod = value
        End Set
    End Property
    Private _guaranteeperiod As Integer?
    <Display(Name:="Waarborgperiode")>
    Public Property GuaranteePeriod() As Integer?
        Get
            Return _guaranteeperiod
        End Get
        Set(ByVal value As Integer?)
            _guaranteeperiod = value
        End Set
    End Property
    Private _type As InsuranceType
    <Display(Name:="Type")>
    Public Property Type() As InsuranceType
        Get
            Return _type
        End Get
        Set(ByVal value As InsuranceType)
            _type = value
        End Set
    End Property
    Private _enddate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Einddatum")>
    Public Property Enddate() As DateOnly?
        Get
            Return _enddate
        End Get
        Set(ByVal value As DateOnly?)
            _enddate = value
        End Set
    End Property
End Class
