Imports System.ComponentModel.DataAnnotations
Public Enum InsuranceType As Integer
    <Display(Name:="ABR Polis")>
    ABR = 1
    <Display(Name:="Brandverzekering")>
    Fire = 2
    <Display(Name:="10-jarige Aansprakelijkheid")>
    tenyear = 3
    <Display(Name:="BA-uitbating")>
    ba = 4
    <Display(Name:="Andere")>
    other = 5
End Enum

