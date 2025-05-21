Public Class ProvincieBO
    Private m_ProvincieId As Integer
    Public Property ProvincieId() As Integer
        Get
            Return m_ProvincieId
        End Get
        Set(ByVal value As Integer)
            m_ProvincieId = value
        End Set
    End Property
    Private m_Name As String
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(ByVal value As String)
            m_Name = value
        End Set
    End Property
    



End Class
