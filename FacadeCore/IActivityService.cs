using BOCore; 

namespace FacadeCore
{
    public interface IActivityService
    {
        GetResponse<ActivityBO> GetActivities();
        GetResponse<IdNameBO> GetActivitiesForSelect();
        GetResponse<ActivityBO> GetActivitybyId(int id);
        GetResponse<ActivityBO> GetActivitiesbyId(List<int> IdList);
        GetResponse<ActivityGroupBO> GetActivityGroups();
        GetResponse<IdNameBO> GetActivityGroupsForSelect();
        GetResponse<ActivityGroupBO> GetActivityGroupbyId(int id);
        Response InsertUpdate(ActivityBO bo);
        Response InsertUpdateGroup(ActivityGroupBO bo);
        Response Delete(List<int> ids);
        Response Delete(List<ActivityBO> bos);
        Response DeleteGroup(List<int> ids);
        Response DeleteGroup(List<ActivityBO> bos);
    }
}
