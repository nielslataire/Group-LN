Public Class ActionTranslation
    Public Property ActionName() As String
        Get
            Return m_ActionName
        End Get
        Set(value As String)
            m_ActionName = Value
        End Set
    End Property
    Private m_ActionName As String
    Public Property Translation() As List(Of Translation)
        Get
            Return m_Translation
        End Get
        Set(value As List(Of Translation))
            m_Translation = Value
        End Set
    End Property
    Private m_Translation As List(Of Translation)

    Public Sub New(actionName As String, translation As List(Of Translation))
        Me.ActionName = actionName
        Me.Translation = translation
    End Sub

End Class
