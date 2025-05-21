Public Class MyViewEngine
    Inherits RazorViewEngine
    Public Sub New()
        Dim newLocationFormat = New String() {"~/Views/{1}/Partials/{0}.vbhtml"}

        PartialViewLocationFormats = PartialViewLocationFormats.Union(newLocationFormat).ToArray()
    End Sub
End Class
