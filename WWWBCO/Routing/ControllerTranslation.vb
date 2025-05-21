Public Class ControllerTranslation
    Public Property ControllerName() As String
        Get
            Return m_ControllerName
        End Get
        Set(value As String)
            m_ControllerName = Value
        End Set
    End Property
    Private m_ControllerName As String
    Public Property Translation() As List(Of Translation)
        Get
            Return m_Translation
        End Get
        Set(value As List(Of Translation))
            m_Translation = Value
        End Set
    End Property
    Private m_Translation As List(Of Translation)
    Public Property ActionTranslations() As List(Of ActionTranslation)
        Get
            Return m_ActionTranslations
        End Get
        Set(value As List(Of ActionTranslation))
            m_ActionTranslations = Value
        End Set
    End Property
    Private m_ActionTranslations As List(Of ActionTranslation)

    Public Sub New(controllerName As String, translation As List(Of Translation), actionsList As List(Of ActionTranslation))
        Me.ControllerName = controllerName
        Me.Translation = translation
        Me.ActionTranslations = actionsList
    End Sub
End Class
