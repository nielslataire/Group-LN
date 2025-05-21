Imports System.ComponentModel.DataAnnotations

Public Class ImageBO
    Private _title As String
    Public Property Title() As String
        Get
            Return _title
        End Get
        Set(ByVal value As String)
            _title = value
        End Set
    End Property
    Private _alttext As String
    Public Property AltText() As String
        Get
            Return _alttext
        End Get
        Set(ByVal value As String)
            _alttext = value
        End Set
    End Property
    Private _caption As String
    <DataType(DataType.Html)>
    Public Property Caption() As String
        Get
            Return _caption
        End Get
        Set(ByVal value As String)
            _caption = value
        End Set
    End Property
    Private _imageurl As String
    <DataType(DataType.ImageUrl)>
    Public Property ImageURL() As String
        Get
            Return _imageurl
        End Get
        Set(ByVal value As String)
            _imageurl = value
        End Set
    End Property
    Private _createddate As DateTime
    <Required>
    <DataType(DataType.DateTime)>
    Public Property CreatedDate() As DateTime
        Get
            Return _createddate
        End Get
        Set(ByVal value As DateTime)
            _createddate = value
        End Set
    End Property







End Class
