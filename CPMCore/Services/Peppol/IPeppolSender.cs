using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.Peppol
{
    public interface IPeppolSender
    {
        Task<PeppolSendResult> SendAsync(string participantId, string xmlContent, CancellationToken ct = default);
    }

    public sealed record PeppolSendResult(bool Success, string? DocumentId, string? Message);
}