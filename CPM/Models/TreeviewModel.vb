Public Class TreeviewModel
    Public Sub New()
        Me.List = New List(Of TreeviewModel)()
    End Sub

    Public Property Id() As Integer
        Get
            Return m_Id
        End Get
        Set(value As Integer)
            m_Id = Value
        End Set
    End Property
    Private m_Id As Integer
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(value As String)
            m_Name = Value
        End Set
    End Property
    Private m_Name As String
    Public Property Type() As String
        Get
            Return m_Type
        End Get
        Set(value As String)
            m_Type = Value
        End Set
    End Property
    Private m_Type As String
    Public Property List() As IList(Of TreeviewModel)
        Get
            Return m_List
        End Get
        Private Set(value As IList(Of TreeviewModel))
            m_List = value
        End Set
    End Property
    Private m_List As IList(Of TreeviewModel)
    Public ReadOnly Property IsChild() As Boolean
        Get
            Return Me.List.Count = 0
        End Get
    End Property
End Class
