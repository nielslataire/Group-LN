Imports BO
Public Class SettingsModel
    Public Sub New()

    End Sub
    Private _bankaccounts As List(Of BankAccountBO)
    Public Property BankAccounts() As List(Of BankAccountBO)
        Get
            Return _bankaccounts
        End Get
        Set(ByVal value As List(Of BankAccountBO))
            _bankaccounts = value
        End Set
    End Property





End Class
