Public Class PermissionDefinition

    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property ParentCode As String
    Public ReadOnly Property SortOrder As Integer

    Public Sub New(code As String, name As String, parentCode As String, sortOrder As Integer)
        Me.Code = code
        Me.Name = name
        Me.ParentCode = parentCode
        Me.SortOrder = sortOrder
    End Sub

End Class