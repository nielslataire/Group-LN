Public Class BankAccountBO

    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property
    Private _accountnr As String
    Public Property AccountNr() As String
        Get
            Return _accountnr
        End Get
        Set(ByVal value As String)
            _accountnr = value
        End Set
    End Property
    Private _bank As String
    Public Property Bank() As String
        Get
            Return _bank
        End Get
        Set(ByVal value As String)
            _bank = value
        End Set
    End Property
    Private _firm As ProgFirm
    Public Property Firm() As ProgFirm
        Get
            Return _firm
        End Get
        Set(ByVal value As ProgFirm)
            _firm = value
        End Set
    End Property
    Private _projectid As Integer?
    Public Property ProjectId() As Integer?
        Get
            Return _projectid
        End Get
        Set(ByVal value As Integer?)
            _projectid = value
        End Set
    End Property
End Class
