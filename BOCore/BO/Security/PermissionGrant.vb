Public Class PermissionGrant

    Public ReadOnly Property Read As Boolean
    Public ReadOnly Property Write As Boolean
    Public ReadOnly Property Delete As Boolean

    Public Sub New(canRead As Boolean, canWrite As Boolean, canDelete As Boolean)
        Me.Read = canRead
        Me.Write = canWrite
        Me.Delete = canDelete
    End Sub

End Class