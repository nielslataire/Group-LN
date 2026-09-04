Imports System.ComponentModel.DataAnnotations

Public Class ContractBO
    Public Sub New()
        m_Activities = New List(Of ContractActivityBO)
        _company = New IdNameBO
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
    Private _projectid As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer)
            _projectid = value
        End Set
    End Property
    Private _vatpercentage As Decimal?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Percentage")>
    <Display(Name:="BTW %")>
    Public Property VatPercentage() As Decimal?
        Get
            Return _vatpercentage
        End Get
        Set(ByVal value As Decimal?)
            _vatpercentage = value
        End Set

    End Property
    Private _paymentterm As Decimal?
    <Display(Name:="Betaaltermijn")>
    <DisplayFormat(DataFormatString:="{0:0}", ApplyFormatInEditMode:=False)>
    Public Property PaymentTerm() As Decimal?
        Get
            Return _paymentterm
        End Get
        Set(ByVal value As Decimal?)
            _paymentterm = value
        End Set
    End Property
    Private _cashDiscount As Boolean
    Public Property CashDiscount() As Boolean
        Get
            Return _cashDiscount
        End Get
        Set(ByVal value As Boolean)
            _cashDiscount = value
        End Set
    End Property
    Private _cashdiscountpercentage As Decimal?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Percentage")>
    <Display(Name:="% korting contant")>
    Public Property CashDiscountPercentage() As Decimal?
        Get
            Return _cashdiscountpercentage
        End Get
        Set(ByVal value As Decimal?)
            _cashdiscountpercentage = value
        End Set
    End Property
    Private _cashdiscountpaymentterm As Decimal?
    <Display(Name:="Betaaltermijn korting contant")>
    <DisplayFormat(DataFormatString:="{0:0}", ApplyFormatInEditMode:=False)>
    Public Property CashDiscountPaymentTerm() As Decimal?
        Get
            Return _cashdiscountpaymentterm
        End Get
        Set(ByVal value As Decimal?)
            _cashdiscountpaymentterm = value
        End Set
    End Property
    Private _guaranteetype As ContractGuaranteeType
    <Display(Name:="Waarborg")>
    Public Property GuaranteeType() As ContractGuaranteeType
        Get
            Return _guaranteetype
        End Get
        Set(ByVal value As ContractGuaranteeType)
            _guaranteetype = value
        End Set
    End Property
    Private _guaranteeprecentage As Decimal?
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:F}")>
    <UIHint("Percentage")>
    <Display(Name:="Waarborg %")>
    Public Property GuaranteePercentage() As Decimal?
        Get
            Return _guaranteeprecentage
        End Get
        Set(ByVal value As Decimal?)
            _guaranteeprecentage = value
        End Set
    End Property
    Private _guaranteeDocFilename As String
    <Display(Name:="Waarborgdocument")>
    Public Property GuaranteeDocFilename() As String
        Get
            Return _guaranteeDocFilename
        End Get
        Set(ByVal value As String)
            _guaranteeDocFilename = value
        End Set
    End Property

    Private _guaranteeDocUploadedAt As Date?
    Public Property GuaranteeDocUploadedAt() As Date?
        Get
            Return _guaranteeDocUploadedAt
        End Get
        Set(ByVal value As Date?)
            _guaranteeDocUploadedAt = value
        End Set
    End Property

    ''' <summary>True wanneer de waarborg een bankwaarborg is maar het document nog niet toegevoegd is.</summary>
    Public ReadOnly Property GuaranteeDocumentMissing() As Boolean
        Get
            Return _guaranteetype = ContractGuaranteeType.BankGuarantee AndAlso String.IsNullOrWhiteSpace(_guaranteeDocFilename)
        End Get
    End Property

    Private _contractsigned As Boolean
    <Display(Name:="Contract getekend")>
    Public Property ContractSigned() As Boolean
        Get
            Return _contractsigned
        End Get
        Set(ByVal value As Boolean)
            _contractsigned = value
        End Set
    End Property

    Private _contractSentDate As Date?
    <Display(Name:="Datum contract verstuurd")>
    <DataType(DataType.Date)>
    <DisplayFormat(DataFormatString:="{0:yyyy-MM-dd}", ApplyFormatInEditMode:=True)>
    Public Property ContractSentDate() As Date?
        Get
            Return _contractSentDate
        End Get
        Set(ByVal value As Date?)
            _contractSentDate = value
        End Set
    End Property

    Private _contractSentNote As String
    <Display(Name:="Opmerking verzending")>
    Public Property ContractSentNote() As String
        Get
            Return _contractSentNote
        End Get
        Set(ByVal value As String)
            _contractSentNote = value
        End Set
    End Property

    Private _siteNotification As Boolean
    <Display(Name:="Werfmelding")>
    Public Property SiteNotification() As Boolean
        Get
            Return _siteNotification
        End Get
        Set(ByVal value As Boolean)
            _siteNotification = value
        End Set
    End Property

    Private _vgmCharter As Boolean
    <Display(Name:="VGM charter")>
    Public Property VgmCharter() As Boolean
        Get
            Return _vgmCharter
        End Get
        Set(ByVal value As Boolean)
            _vgmCharter = value
        End Set
    End Property

    Private _pidAttest As Boolean
    <Display(Name:="PID-attesten")>
    Public Property PidAttest() As Boolean
        Get
            Return _pidAttest
        End Get
        Set(ByVal value As Boolean)
            _pidAttest = value
        End Set
    End Property


    Private m_Activities As List(Of ContractActivityBO)
    Public Property Activities() As List(Of ContractActivityBO)
        Get
            Return m_Activities
        End Get
        Set(ByVal value As List(Of ContractActivityBO))
            m_Activities = value
        End Set
    End Property
    Private _company As IdNameBO
    Public Property Company() As IdNameBO
        Get
            Return _company
        End Get
        Set(ByVal value As IdNameBO)
            _company = value
        End Set
    End Property
    Private _siteManagerContactId As Integer?
    <Display(Name:="Verantwoordelijke werfleider")>
    Public Property SiteManagerContactId() As Integer?
        Get
            Return _siteManagerContactId
        End Get
        Set(ByVal value As Integer?)
            _siteManagerContactId = value
        End Set
    End Property

    Private _contractName As String
    <Display(Name:="Contractnaam")>
    Public Property ContractName() As String
        Get
            Return _contractName
        End Get
        Set(ByVal value As String)
            _contractName = value
        End Set
    End Property

End Class
