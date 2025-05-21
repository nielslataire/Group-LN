Imports System.ComponentModel.DataAnnotations
<AtLeastOneProperty("Name", "CompanyName", ErrorMessage:="Gelieve de naam of bedrijfsnaam in te vullen")>
Public Class ClientAccountBO
    Public Sub New()
        m_postalcode = New PostalCodeBO
        m_invoicepostalcode = New PostalCodeBO
        _contacts = New List(Of ClientContactBO)
        _coowners = New List(Of ClientContactBO)
        m_ownertype = New ClientOwnerTypeBO
    End Sub
    Private m_Id As Integer
    Public Property Id() As Integer
        Get
            Return m_Id
        End Get
        Set(ByVal value As Integer)
            m_Id = value
        End Set
    End Property
    Private m_Name As String
    <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(ByVal value As String)
            m_Name = value
        End Set
    End Property
    Private m_Salutation As Salutation
    <Display(Name:="Aanspreking")>
    Public Property Salutation() As Salutation
        Get
            Return m_Salutation
        End Get
        Set(ByVal value As Salutation)
            m_Salutation = value
        End Set
    End Property
    Private m_street As String
    <Display(Name:="Straat")>
    Public Property Street() As String
        Get
            Return m_street
        End Get
        Set(ByVal value As String)
            m_street = value
        End Set
    End Property
    Private m_housenumber As String
    <Display(Name:="Huisnummer")>
    Public Property Housenumber() As String
        Get
            Return m_housenumber
        End Get
        Set(ByVal value As String)
            m_housenumber = value
        End Set
    End Property
    Private m_busnumber As String
    <Display(Name:="Bus")>
    Public Property Busnumber() As String
        Get
            Return m_busnumber
        End Get
        Set(ByVal value As String)
            m_busnumber = value
        End Set
    End Property
    Private m_postalcode As PostalCodeBO
    <Display(Name:="Gemeente")>
    Public Property Postalcode() As PostalCodeBO
        Get
            Return m_postalcode
        End Get
        Set(ByVal value As PostalCodeBO)
            m_postalcode = value
        End Set
    End Property
    Private m_datesalesagreement As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <DataType(DataType.Date)>
    <Display(Name:="Verkoopdatum")>
    Public Property DateSalesAgreement() As DateOnly?
        Get
            Return m_datesalesagreement
        End Get
        Set(ByVal value As DateOnly?)
            m_datesalesagreement = value
        End Set
    End Property
    Private m_datedeedofsale As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <Display(Name:="Aktedatum")>
    Public Property DateDeedOfSale() As DateOnly?
        Get
            Return m_datedeedofsale
        End Get
        Set(ByVal value As DateOnly?)
            m_datedeedofsale = value
        End Set
    End Property
    Private m_datedeedofsaleexpdate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <Display(Name:="Uiterste aktedatum")>
    Public Property DateDeedOfSaleExpDate() As DateOnly?
        Get
            Return m_datedeedofsaleexpdate
        End Get
        Set(ByVal value As DateOnly?)
            m_datedeedofsaleexpdate = value
        End Set
    End Property
    Private m_deliverydate As DateOnly?
    <UIHint("Date")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:dd/MM/yyyy}")>
    <Display(Name:="Opleverdatum")>
    Public Property DeliveryDate() As DateOnly?
        Get
            Return m_deliverydate
        End Get
        Set(ByVal value As DateOnly?)
            m_deliverydate = value
        End Set
    End Property
    Private m_deliverydoc As String
    <Display(Name:="Opleveringsdocument")>
    Public Property DeliveryDoc() As String
        Get
            Return m_deliverydoc
        End Get
        Set(ByVal value As String)
            m_deliverydoc = value
        End Set
    End Property

    Private m_ownerpercentage As Integer?
    Public Property OwnerPercentage() As Integer?
        Get
            Return m_ownerpercentage
        End Get
        Set(ByVal value As Integer?)
            m_ownerpercentage = value
        End Set
    End Property
    Private m_ownertype As ClientOwnerTypeBO
    Public Property OwnerType() As ClientOwnerTypeBO
        Get
            Return m_ownertype
        End Get
        Set(ByVal value As ClientOwnerTypeBO)
            m_ownertype = value
        End Set
    End Property
    Private _contacts As List(Of ClientContactBO)
    Public Property Contacts() As List(Of ClientContactBO)
        Get
            Return _contacts
        End Get
        Set(ByVal value As List(Of ClientContactBO))
            _contacts = value
        End Set
    End Property
    Private _coowners As List(Of ClientContactBO)
    Public Property CoOwners() As List(Of ClientContactBO)
        Get
            Return _coowners
        End Get
        Set(ByVal value As List(Of ClientContactBO))
            _coowners = value
        End Set
    End Property
    Private _vatnumber As String
    Public Property VATnumber() As String
        Get
            Return _vatnumber
        End Get
        Set(ByVal value As String)
            _vatnumber = value
        End Set
    End Property
    Private _companyName As String
    Public Property CompanyName() As String
        Get
            Return _companyName
        End Get
        Set(ByVal value As String)
            _companyName = value
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

    'INVOICING
    Private m_invoiceaddress As Boolean?
    Public Property InvoiceAddress() As Boolean?
        Get
            Return m_invoiceaddress
        End Get
        Set(ByVal value As Boolean?)
            m_invoiceaddress = value
        End Set
    End Property
    Private m_invoicestreet As String
    <Display(Name:="Straat")>
    Public Property InvoiceStreet() As String
        Get
            Return m_invoicestreet
        End Get
        Set(ByVal value As String)
            m_invoicestreet = value
        End Set
    End Property
    Private m_invoicehousenumber As String
    <Display(Name:="Huisnummer")>
    Public Property InvoiceHousenumber() As String
        Get
            Return m_invoicehousenumber
        End Get
        Set(ByVal value As String)
            m_invoicehousenumber = value
        End Set
    End Property
    Private m_invoicebusnumber As String
    <Display(Name:="Bus")>
    Public Property InvoiceBusnumber() As String
        Get
            Return m_invoicebusnumber
        End Get
        Set(ByVal value As String)
            m_invoicebusnumber = value
        End Set
    End Property
    Private m_invoicepostalcode As PostalCodeBO
    <Display(Name:="Gemeente")>
    Public Property InvoicePostalcode() As PostalCodeBO
        Get
            Return m_invoicepostalcode
        End Get
        Set(ByVal value As PostalCodeBO)
            m_invoicepostalcode = value
        End Set
    End Property
    Private m_invoiceextra As String
    <Display(Name:="Facturatie Tekst")>
    Public Property InvoiceExtra() As String
        Get
            Return m_invoiceextra
        End Get
        Set(ByVal value As String)
            m_invoiceextra = value
        End Set
    End Property

    'HELPER
    Public ReadOnly Property DisplayName() As String
        Get
            If Name IsNot Nothing Then

                Return Name
            Else
                Return CompanyName
            End If
        End Get
    End Property


End Class

Public Class ClientGiftWithAccountDetailsBO
    Inherits ClientGiftBO

    Private _accountname As String
    Public Property AccountName() As String
        Get
            Return _accountname
        End Get
        Set(ByVal value As String)
            _accountname = value
        End Set
    End Property
    Private _livingunit As String
    Public Property LivingUnit() As String
        Get
            Return _livingunit
        End Get
        Set(ByVal value As String)
            _livingunit = value
        End Set
    End Property

End Class
Public Class ClientPoaWithAccountDetailsBO
    Inherits ClientPoaBO

    Private _accountname As String
    Public Property AccountName() As String
        Get
            Return _accountname
        End Get
        Set(ByVal value As String)
            _accountname = value
        End Set
    End Property
    Private _livingunit As String
    Public Property LivingUnit() As String
        Get
            Return _livingunit
        End Get
        Set(ByVal value As String)
            _livingunit = value
        End Set
    End Property

End Class
