Imports BO
Public Class ActivitiesModel
    Public Sub New()
        _activities = New List(Of ActivityGroupBO)
        _selectlistActivities = New List(Of IdNameBO)
        _selectedactivity = New ActivityBO
        _selectListGroups = New List(Of IdNameBO)
        _selectedActivityGroup = New ActivityGroupBO
    End Sub
    '---- PROPERTIES VOOR ACTIVITIES ------
    Private _activities As List(Of ActivityGroupBO)
    Public Property Activities() As List(Of ActivityGroupBO)
        Get
            Return _activities
        End Get
        Set(ByVal value As List(Of ActivityGroupBO))
            _activities = value
        End Set

    End Property
    Private _selectedactivity As ActivityBO
    Public Property SelectedActivity() As ActivityBO
        Get
            Return _selectedactivity
        End Get
        Set(ByVal value As ActivityBO)
            _selectedactivity = value
        End Set

    End Property
    Private _selectlistgroupsforactivity As List(Of IdNameBO)
    Public Property SelectListGroupsForActivity() As List(Of IdNameBO)
        Get
            Return _selectlistgroupsforactivity
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _selectlistgroupsforactivity = value
        End Set
    End Property
    Private _selectlistActivities As List(Of IdNameBO)
    Public Property SelectListActivities() As List(Of IdNameBO)
        Get
            Return _selectlistActivities
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _selectlistActivities = value
        End Set
    End Property
    Private _selectedgroup As Integer
    Public Property SelectedGroup() As Integer
        Get
            Return _selectedgroup
        End Get
        Set(ByVal value As Integer)
            _selectedgroup = value
        End Set

    End Property
    Private _SelectedActivityID As Integer
    Public Property SelectedActivityID() As Integer
        Get
            Return _SelectedActivityID
        End Get
        Set(ByVal value As Integer)
            _SelectedActivityID = value
        End Set
    End Property

    '----- PROPERTIES VOOR ACTIVITYGROUPS
    Private _selectedActivityGroup As ActivityGroupBO
    Public Property SelectedActivityGroup() As ActivityGroupBO
        Get
            Return _selectedActivityGroup
        End Get
        Set(ByVal value As ActivityGroupBO)
            _selectedActivityGroup = value
        End Set
    End Property
    Private _selectedActivityGroupID As Integer
    Public Property SelectedActivityGroupId() As Integer
        Get
            Return _selectedActivityGroupID
        End Get
        Set(ByVal value As Integer)
            _selectedActivityGroupID = value
        End Set

    End Property
    Private _selectListGroups As List(Of IdNameBO)
    Public Property SelectListGroups() As List(Of IdNameBO)
        Get
            Return _selectListGroups
        End Get
        Set(ByVal value As List(Of IdNameBO))
            _selectListGroups = value
        End Set
    End Property





End Class
