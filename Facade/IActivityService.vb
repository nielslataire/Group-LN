Imports BO
Public Interface IActivityService
    Function GetActivities() As GetResponse(Of ActivityBO)
    Function GetActivitiesForSelect() As GetResponse(Of IdNameBO)
    Function GetActivitybyId(id As Integer) As GetResponse(Of ActivityBO)
    Function GetActivitiesbyId(IdList As List(Of Integer)) As GetResponse(Of ActivityBO)
    Function GetActivityGroups() As GetResponse(Of ActivityGroupBO)
    Function GetActivityGroupsForSelect() As GetResponse(Of IdNameBO)
    Function GetActivityGroupbyId(id As Integer) As GetResponse(Of ActivityGroupBO)
    Function InsertUpdate(bo As ActivityBO) As Response
    Function InsertUpdateGroup(bo As ActivityGroupBO) As Response
    Function Delete(ids As List(Of Integer)) As Response
    Function Delete(bos As List(Of ActivityBO)) As Response
    Function DeleteGroup(ids As List(Of Integer)) As Response
    Function DeleteGroup(bos As List(Of ActivityBO)) As Response
End Interface
