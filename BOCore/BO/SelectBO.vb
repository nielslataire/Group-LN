Public Class SelectBO
    Private m_id As Integer
    Public Property id() As Integer
        Get
            Return m_id
        End Get
        Set(ByVal value As Integer)
            m_id = value
        End Set
    End Property
    Private m_text As String
    Public Property text() As String
        Get
            Return m_text
        End Get
        Set(ByVal value As String)
            m_text = value
        End Set
    End Property
    Private m_extra As String
    Public Property extra() As String
        Get
            Return m_extra
        End Get
        Set(ByVal value As String)
            m_extra = value
        End Set
    End Property



End Class
