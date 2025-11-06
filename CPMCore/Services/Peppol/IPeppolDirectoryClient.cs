using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.Peppol
{
    public interface IPeppolDirectoryClient
    {
        Task<PeppolParticipant?> FindParticipantAsync(string identifier, CancellationToken ct = default);
    }

    public sealed record PeppolParticipant(string ParticipantId, string? Name, string? CountryCode);
}