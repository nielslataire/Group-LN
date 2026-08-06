using BOCore;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore
{
    public interface IHomeHeroProjectService
    {
        Task<HomeHeroProjectBO> GetAsync(CancellationToken ct = default);
        Task SaveAsync(HomeHeroProjectBO bo, CancellationToken ct = default);
    }
}
