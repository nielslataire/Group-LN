Imports System.ComponentModel.DataAnnotations
Imports System.Web.Mvc
Public Class ProjectSalesSettingsBO
    Public Sub New()

    End Sub
    Private _settingsid As Integer
    Public Property SettingsId() As Integer
        Get
            Return _settingsid
        End Get
        Set(ByVal value As Integer)
            _settingsid = value
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
    Private _vatpercentage As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Percentage")>
    <Display(Name:="BTW %")>
    Public Property VatPercentage() As Decimal
        Get
            Return _vatpercentage
        End Get
        Set(ByVal value As Decimal)
            _vatpercentage = value
        End Set
    End Property
    Private _registrationpercentage As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Percentage")>
    <Display(Name:="Registratierechten %")>
    Public Property RegistrationPercentage() As Decimal
        Get
            Return _registrationpercentage
        End Get
        Set(ByVal value As Decimal)
            _registrationpercentage = value
        End Set
    End Property
    Private _mixedvatregistration As Boolean
    <Display(Name:="Onder BTW stelsel")>
    Public Property MixedVatRegistration() As Boolean
        Get
            Return _mixedvatregistration
        End Get
        Set(ByVal value As Boolean)
            _mixedvatregistration = value
        End Set
    End Property
    Private _connectionfees As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Aansluitkosten")>
    Public Property ConnectionFees() As Decimal
        Get
            Return _connectionfees
        End Get
        Set(ByVal value As Decimal)
            _connectionfees = value
        End Set
    End Property
    Private _basecertificatecost As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Aandeel Basisakte")>
    Public Property BaseCertificateCost() As Decimal
        Get
            Return _basecertificatecost
        End Get
        Set(ByVal value As Decimal)
            _basecertificatecost = value
        End Set
    End Property
    Private _fixedcertificatecost As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Vaste Aktekost")>
    Public Property FixedCertificateCost() As Decimal
        Get
            Return _fixedcertificatecost
        End Get
        Set(ByVal value As Decimal)
            _fixedcertificatecost = value
        End Set
    End Property
    Private _mortageregistrationcost As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Hypotheekkantoor")>
    Public Property MortageRegistrationCost() As Decimal
        Get
            Return _mortageregistrationcost
        End Get
        Set(ByVal value As Decimal)
            _mortageregistrationcost = value
        End Set
    End Property
    Private _bankaccountnumber As String
    <Display(Name:="Projectrekening")>
    Public Property BankAccountNumber() As String
        Get
            Return _bankaccountnumber
        End Get
        Set(ByVal value As String)
            _bankaccountnumber = value
        End Set
    End Property
    Private _salevisible As Boolean
    <Display(Name:="Verkoop zichtbaar")>
    Public Property SaleVisible() As Boolean
        Get
            Return _salevisible
        End Get
        Set(ByVal value As Boolean)
            _salevisible = value
        End Set
    End Property
    Private _registrationType As RegistrationType
    <Display(Name:="Type registratie")>
    Public Property RegistrationType() As RegistrationType
        Get
            Return _registrationType
        End Get
        Set(ByVal value As RegistrationType)
            _registrationType = value
        End Set
    End Property
    Private _surveyorcost As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Landmetingskosten")>
    Public Property SurveyorCost() As Decimal
        Get
            Return _surveyorcost
        End Get
        Set(ByVal value As Decimal)
            _surveyorcost = value
        End Set
    End Property
    Private _parcelcost As Decimal
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <UIHint("Currency")>
    <Display(Name:="Aandeel verkavelingsakte")>
    Public Property ParcelCost() As Decimal
        Get
            Return _parcelcost
        End Get
        Set(ByVal value As Decimal)
            _parcelcost = value
        End Set
    End Property
End Class
