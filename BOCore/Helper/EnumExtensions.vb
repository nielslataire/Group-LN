Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.Reflection


Public Module EnumExtensions
    Sub New()

    End Sub
    <System.Runtime.CompilerServices.Extension>
    Public Function GetDisplayName(enumValue As [Enum]) As String
        ' Waarde die niet in de enum voorkomt (bv. onbekende code uit de database) of zonder [Display]: val terug op de ruwe tekst
        ' i.p.v. een exception die de hele pagina doet falen.
        If Not [Enum].IsDefined(enumValue.GetType(), enumValue) Then Return ""
        Dim member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault()
        Dim display = member?.GetCustomAttribute(Of DisplayAttribute)()
        Return If(display?.GetName(), enumValue.ToString())
    End Function

End Module
