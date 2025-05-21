Imports System.Linq.Expressions

Module ExtensionsModule

    <System.Runtime.CompilerServices.Extension>
    Public Function ToSelectList(Of T)(enumerable As IEnumerable(Of T), text As Func(Of T, String), value As Func(Of T, String), defaultOption As String) As List(Of SelectListItem)
        Dim items = enumerable.[Select](Function(f) New SelectListItem() With {.Text = text(f), .Value = value(f)}).ToList()
        items.Insert(0, New SelectListItem() With {
            .Text = defaultOption,
            .Value = "-1"
        })
        Return items
    End Function


End Module
