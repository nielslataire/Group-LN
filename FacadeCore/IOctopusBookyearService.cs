using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IOctopusBookyearService
    {
        Task SyncAsync(int issuerId, IEnumerable<OctopusBookyearBO> bookyears, CancellationToken ct = default);
        Task<IReadOnlyList<OctopusBookyearBO>> ListByIssuerAsync(int issuerId, CancellationToken ct = default);
        Task<OctopusBookyearBO?> GetAsync(int id, CancellationToken ct = default);
    }
}