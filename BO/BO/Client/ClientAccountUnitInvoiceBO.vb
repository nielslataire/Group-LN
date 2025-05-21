Imports System.ComponentModel.DataAnnotations
Public Class ClientAccountUnitInvoiceBO
    Public Sub New()

    End Sub
    Private _clientaccountid As Integer
    Public Property ClientAccountId() As Integer
        Get
            Return _clientaccountid
        End Get
        Set(ByVal value As Integer)
            _clientaccountid = value
        End Set
    End Property
    Private _unitid As Integer
    Public Property Unitid() As Integer
        Get
            Return _unitid
        End Get
        Set(ByVal value As Integer)
            _unitid = value
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
    Private _companyid As Integer
    Public Property CompanyId() As Integer
        Get
            Return _companyid
        End Get
        Set(ByVal value As Integer)
            _companyid = value
        End Set
    End Property
End Class
