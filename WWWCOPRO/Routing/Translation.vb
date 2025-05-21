Imports System.Globalization
Public Class Translation
    Public Property CultureInfo() As CultureInfo
        Get
            Return m_CultureInfo
        End Get
        Set(value As CultureInfo)
            m_CultureInfo = value
        End Set
    End Property
    Private m_CultureInfo As CultureInfo
    Public Property TranslatedValue() As String
        Get
            Return m_TranslatedValue
        End Get
        Set(value As String)
            m_TranslatedValue = value
        End Set
    End Property
    Private m_TranslatedValue As String
    Public Sub New(culture As CultureInfo, translatedValue__1 As String)
        CultureInfo = culture
        TranslatedValue = translatedValue__1
    End Sub
End Class
