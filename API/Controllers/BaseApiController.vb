Imports System.Net
Imports System.Web.Http

Namespace Controllers
    Public Class BaseApiController
        Inherits ApiController
        Private _modelFactory As ModelFactory

        Protected ReadOnly Property TheModelFactory() As ModelFactory
            Get
                If _modelFactory Is Nothing Then
                    _modelFactory = New ModelFactory()
                End If
                Return _modelFactory
            End Get
        End Property
    End Class
End Namespace