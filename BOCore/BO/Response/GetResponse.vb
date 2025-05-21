Public Class GetResponse(Of t As New)
    Inherits Response

    Public Sub New()
        Values = New List(Of t)()
    End Sub

    Private _values As List(Of t)
    Public Property Values() As List(Of t)
        Get
            Return _values
        End Get
        Set(ByVal value As List(Of t))
            _values = value
        End Set
    End Property


    Public Property Value() As t
        Get
            If (Values IsNot Nothing AndAlso Values.Count = 1) Then
                Return Values.First()
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As t)
            Values = New List(Of t)()
            Values.Add(value)
        End Set
    End Property


    Public Sub AddValue(value As t)
        If (Values Is Nothing) Then Values = New List(Of t)()
        Values.Add(value)
    End Sub
End Class