using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IPartyLookupService
    {
        Task<int> GetFirstActiveIssuerIdAsync(CancellationToken ct = default);
        Task<IReadOnlyList<(int Id, string Name)>> ListActiveIssuersAsync(CancellationToken ct = default);

        // unified search
        Task<IReadOnlyList<PartyLookupItem>> SearchPartiesAsync(string term, int take = 20, CancellationToken ct = default);


    }

    public record PartyLookupItem(InvoicePartyType Type, int Id, string Name, string? Hint = null);
}
