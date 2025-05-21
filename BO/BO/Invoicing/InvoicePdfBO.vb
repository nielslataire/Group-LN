Public Class InvoicePdfBO
    Public Sub New()
        _orderitems = New List(Of InvoicePdfOrderItemBO)
    End Sub
    Private _invoicenumber As Integer
    Public Property Invoicenumber() As Integer
        Get
            Return _invoicenumber
        End Get
        Set(ByVal value As Integer)
            _invoicenumber = value
        End Set
    End Property
    Private _Issuedate As DateTime
    Public Property Issuedate() As DateTime
        Get
            Return _Issuedate
        End Get
        Set(ByVal value As DateTime)
            _Issuedate = value
        End Set
    End Property
    Private _duedate As DateTime
    Public Property Duedate() As DateTime
        Get
            Return _duedate
        End Get
        Set(ByVal value As DateTime)
            _duedate = value
        End Set
    End Property
    Private _selleraddress As InvoicePdfAddressBo
    Public Property SellerAddress() As InvoicePdfAddressBo
        Get
            Return _selleraddress
        End Get
        Set(ByVal value As InvoicePdfAddressBo)
            _selleraddress = value
        End Set
    End Property
    Private _customeraddress As InvoicePdfAddressBo
    Public Property CustomerAddress() As InvoicePdfAddressBo
        Get
            Return _customeraddress
        End Get
        Set(ByVal value As InvoicePdfAddressBo)
            _customeraddress = value
        End Set
    End Property
    Private _orderitems As List(Of InvoicePdfOrderItemBO)
    Public Property Orderitems() As List(Of InvoicePdfOrderItemBO)
        Get
            Return _orderitems
        End Get
        Set(ByVal value As List(Of InvoicePdfOrderItemBO))
            _orderitems = value
        End Set
    End Property
    Private _comments As String
    Public Property Comments() As String
        Get
            Return _comments
        End Get
        Set(ByVal value As String)
            _comments = value
        End Set
    End Property
End Class
Public Class InvoicePdfOrderItemBO
    Private _name As String
    Public Property Name() As String
        Get
            Return _name
        End Get
        Set(ByVal value As String)
            _name = value
        End Set
    End Property
    Private _price As Decimal
    Public Property Price() As Decimal
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal)
            _price = value
        End Set
    End Property
    Private _quantity As Integer
    Public Property Quantity() As Integer
        Get
            Return _quantity
        End Get
        Set(ByVal value As Integer)
            _quantity = value
        End Set
    End Property
End Class
Public Class InvoicePdfAddressBo
    Private _companyname As String
    Public Property CompanyName() As String
        Get
            Return _companyname
        End Get
        Set(ByVal value As String)
            _companyname = value
        End Set
    End Property
    Private _street As String
    Public Property Street() As String
        Get
            Return _street
        End Get
        Set(ByVal value As String)
            _street = value
        End Set
    End Property
    Private _city As String
    Public Property City() As String
        Get
            Return _city
        End Get
        Set(ByVal value As String)
            _city = value
        End Set
    End Property
    Private _email As Object
    Public Property Email() As Object
        Get
            Return _email
        End Get
        Set(ByVal value As Object)
            _email = value
        End Set
    End Property
    Private _phone As String
    Public Property Phone() As String
        Get
            Return _phone
        End Get
        Set(ByVal value As String)
            _phone = value
        End Set
    End Property
    Private _VAT As String
    Public Property VAT() As String
        Get
            Return _VAT
        End Get
        Set(ByVal value As String)
            _VAT = value
        End Set
    End Property
End Class
