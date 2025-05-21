Imports BO

Public Class ContactModel
    Public Sub New()
        _contact = New ContactBO()
    End Sub
    Private _contact As ContactBO
    Public Property Contact() As ContactBO
        Get
            Return _contact
        End Get
        Set(ByVal value As ContactBO)
            _contact = value
        End Set
    End Property
    Private _selectableDepartments As List(Of IdNameBO)
    Public Property SelectableDepartments() As List(Of IdNameBO)
        Get
            Return _selectableDepartments
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _selectableDepartments = value
        End Set
    End Property

End Class
