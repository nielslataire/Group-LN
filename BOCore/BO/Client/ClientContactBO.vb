Imports System.ComponentModel.DataAnnotations

Public Class ClientContactBO
    Public Sub New()
        m_postalcode = New PostalCodeBO
        m_invoicepostalcode = New PostalCodeBO
        _coownertype = New ClientOwnerTypeBO
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
    Private m_accountid As Integer
    Public Property AccountId() As Integer
        Get
            Return m_accountid
        End Get
        Set(ByVal value As Integer)
            m_accountid = value
        End Set
    End Property
    Private m_Name As String
    <Required(ErrorMessage:="Gelieve de naam in te vullen")>
    <Display(Name:="Naam")>
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(ByVal value As String)
            m_Name = value
        End Set
    End Property
    Private m_firstname As String
    <Display(Name:="Voornaam")>
    Public Property Firstname() As String
        Get
            Return m_firstname
        End Get
        Set(ByVal value As String)
            m_firstname = value
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
    Private m_phone As String
    <UIHint("Phone")>
    <Display(Name:="Telefoon")>
    Public Property Phone() As String
        Get
            Return m_phone
        End Get
        Set(ByVal value As String)
            m_phone = value
        End Set
    End Property
    Private m_cellphone As String
    <UIHint("Cellphone")>
    <Display(Name:="GSM")>
    Public Property Cellphone() As String
        Get
            Return m_cellphone
        End Get
        Set(ByVal value As String)
            m_cellphone = value
        End Set
    End Property
    Private m_Email As String
    <EmailAddress>
    <UIHint("Email")>
    <Display(Name:="Email")>
    Public Property Email() As String
        Get
            Return m_Email
        End Get
        Set(ByVal value As String)
            m_Email = value
        End Set
    End Property
    Private m_iscoowner As Boolean
    Public Property IsCoOwner() As Boolean
        Get
            Return m_iscoowner
        End Get
        Set(ByVal value As Boolean)
            m_iscoowner = value
        End Set
    End Property

    Private m_coownerpercentage As Decimal?
    <Display(Name:="% Mede-eigenaar")>
    <DisplayFormat(DataFormatString:="{0:0,00}")>
    Public Property CoOwnerPercentage() As Decimal?
        Get
            Return m_coownerpercentage
        End Get
        Set(ByVal value As Decimal?)
            m_coownerpercentage = value
        End Set
    End Property
    Private _coownertype As ClientOwnerTypeBO
    Public Property CoOwnerType() As ClientOwnerTypeBO
        Get
            Return _coownertype
        End Get
        Set(ByVal value As ClientOwnerTypeBO)
            _coownertype = value
        End Set
    End Property

    Private _vatnumber As String
    <Display(Name:="BTW nummer")>
    Public Property VATnumber() As String
        Get
            Return _vatnumber
        End Get
        Set(ByVal value As String)
            _vatnumber = value
        End Set
    End Property
    Private _companyname As String
    <Display(Name:="Bedrijfsnaam")>
    Public Property CompanyName() As String
        Get
            Return _companyname
        End Get
        Set(ByVal value As String)
            _companyname = value
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

    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedTelefoon() As String
        Get
            Return Phone
        End Get

    End Property
    <UIHint("TelefoonFormat")>
    Public ReadOnly Property FormattedGSM() As String
        Get
            Return Cellphone
        End Get

    End Property
End Class
