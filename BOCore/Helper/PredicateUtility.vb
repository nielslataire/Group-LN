Imports System.Linq.Expressions



Public Class ParameterRebinder
    Inherits ExpressionVisitor
    Private ReadOnly _map As Dictionary(Of ParameterExpression, ParameterExpression)

    Public Sub New(map As Dictionary(Of ParameterExpression, ParameterExpression))
        _map = If(map, New Dictionary(Of ParameterExpression, ParameterExpression)())
    End Sub

    Public Shared Function ReplaceParameters(map As Dictionary(Of ParameterExpression, ParameterExpression), exp As Expression) As Expression
        Return New ParameterRebinder(map).Visit(exp)
    End Function

    Protected Overrides Function VisitParameter(p As ParameterExpression) As Expression
        Dim replacement As ParameterExpression

        If _map.TryGetValue(p, replacement) Then
            p = replacement
        End If

        Return MyBase.VisitParameter(p)

    End Function
End Class

Public Module PredicateUtility
    Sub New()
    End Sub
    <System.Runtime.CompilerServices.Extension> _
    Public Function Compose(Of T)(first As Expression(Of T), second As Expression(Of T), merge As Func(Of Expression, Expression, Expression)) As Expression(Of T)
        'Build parameter map (from parameters of second to parameters of first)
        Dim map = first.Parameters.[Select](Function(f, i) New With { _
            f, _
            Key .s = second.Parameters(i) _
        }).ToDictionary(Function(p) p.s, Function(p) p.f)

        'Replace parameters in the second lambda expression with parameters from the first
        Dim secondBody = ParameterRebinder.ReplaceParameters(map, second.Body)

        'Apply composition of lambda expression bodies to parameters from the first expression
        Return Expression.Lambda(Of T)(merge(first.Body, secondBody), first.Parameters)
    End Function

    <System.Runtime.CompilerServices.Extension> _
    Public Function [And](Of T)(first As Expression(Of Func(Of T, Boolean)), second As Expression(Of Func(Of T, Boolean))) As Expression(Of Func(Of T, Boolean))
        If second Is Nothing AndAlso first Is Nothing Then
            Return Nothing
        End If
        If second Is Nothing Then
            Return first
        End If
        If first Is Nothing Then
            Return second
        End If

        Return first.Compose(second, AddressOf Expression.And)
    End Function

    <System.Runtime.CompilerServices.Extension> _
    Public Function [Or](Of T)(first As Expression(Of Func(Of T, Boolean)), second As Expression(Of Func(Of T, Boolean))) As Expression(Of Func(Of T, Boolean))
        If second Is Nothing AndAlso first Is Nothing Then
            Return Nothing
        End If
        If second Is Nothing Then
            Return first
        End If
        If first Is Nothing Then
            Return second
        End If

        Return first.Compose(second, AddressOf Expression.Or)
    End Function
End Module
