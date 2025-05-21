

Public Class AddressBook
    Public Property FirstName() As String
        Get
            Return m_FirstName
        End Get
        Set(value As String)
            m_FirstName = Value
        End Set
    End Property
    Private m_FirstName As String

    Public Property LastName() As String
        Get
            Return m_LastName
        End Get
        Set(value As String)
            m_LastName = Value
        End Set
    End Property
    Private m_LastName As String

    Public Property Address() As String
        Get
            Return m_Address
        End Get
        Set(value As String)
            m_Address = Value
        End Set
    End Property
    Private m_Address As String

    Public Property Phone() As String
        Get
            Return m_Phone
        End Get
        Set(value As String)
            m_Phone = Value
        End Set
    End Property
    Private m_Phone As String

    Public Sub New(firstname As String, lastname As String, address As String, phone As String)
        Me.FirstName = firstname
        Me.LastName = lastname
        Me.Address = address
        Me.Phone = phone
    End Sub
End Class

