Imports System.ComponentModel.DataAnnotations
Public Enum ContractGuaranteeType As Integer
    <Display(Name:="Geen waarborg")>
    NoGuarantee = 1
    <Display(Name:="Financiële waarborg")>
    FinancialGuarantee = 2
    <Display(Name:="Bankwaarborg")>
    BankGuarantee = 3
End Enum

