Imports System.ComponentModel.DataAnnotations
Imports System.Reflection

<AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
Public Class AtLeastOnePropertyAttribute
    Inherits ValidationAttribute
    Private m_PropertyList As String()
    Public Property PropertyList() As String()
        Get
            Return m_PropertyList
        End Get
        Set(ByVal value As String())
            m_PropertyList = value
        End Set
    End Property
    Public Sub New(ParamArray propertyList As String())
        Me.PropertyList = propertyList
    End Sub
    Public Overrides ReadOnly Property TypeId() As Object
        Get
            Return Me
        End Get
    End Property
    Public Overrides Function IsValid(value As Object) As Boolean
        Dim propertyInfo As PropertyInfo
        For Each propertyName As String In PropertyList
            propertyInfo = value.GetType().GetProperty(propertyName)
            If propertyInfo IsNot Nothing AndAlso propertyInfo.GetValue(value, Nothing) IsNot Nothing Then
                Return True
            End If
        Next
        Return False


    End Function

End Class
