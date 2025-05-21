Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.Reflection


Public Module EnumExtensions
    Sub New()

    End Sub
    <System.Runtime.CompilerServices.Extension>
    Public Function GetDisplayName(enumValue As [Enum]) As String
        Return enumValue.GetType().GetMember(enumValue.ToString()).First().GetCustomAttribute(Of DisplayAttribute).GetName()
    End Function

End Module
