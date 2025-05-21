Partial Public Class testdbEntities
    Inherits System.Data.Entity.DbContext

    Public Sub New(connstring As String)
        MyBase.New(connstring)
    End Sub

    Public Sub New(detectchanges As Boolean)
        Configuration.AutoDetectChangesEnabled = detectchanges

    End Sub
    Public Sub New(detectchanges As Boolean, connstring As String)
        MyBase.New(connstring)
        Configuration.AutoDetectChangesEnabled = detectchanges
    End Sub
End Class