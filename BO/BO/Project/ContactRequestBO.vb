Public Class ContactRequestBO
    Public Property Id As Integer
    Public Property ProjectId As Integer?
    Public Property UnitId As Integer?
    Public Property DocumentId As Integer?
    Public Property DocumentName As String
    Public Property DocumentFileName As String
    Public Property DocumentType As String
    Public Property SourceSite As String
    Public Property Origin As String
    Public Property RequestType As String
    Public Property Firstname As String
    Public Property Lastname As String
    Public Property Fullname As String
    Public Property Email As String
    Public Property Phone As String
    Public Property Subject As String
    Public Property Question As String
    Public Property ExternalMailStatus As String
    Public Property InternalMailStatus As String
    Public Property CreatedAt As DateTime
End Class