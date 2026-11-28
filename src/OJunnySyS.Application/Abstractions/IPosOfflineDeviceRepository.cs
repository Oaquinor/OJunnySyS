using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Application.Abstractions;

public interface IPosOfflineDeviceRepository
{
    Task<IReadOnlyList<PosOfflineDevice>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<PosOfflineDevice> RegisterAsync(Guid deviceId, string label, string publicKey, string fingerprint, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid deviceId, long expectedRevision, CancellationToken cancellationToken = default);
    Task<PosOfflineDevice?> GetActiveForAttestationAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
