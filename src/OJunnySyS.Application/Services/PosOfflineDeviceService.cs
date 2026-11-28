using System.Security.Cryptography;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Application.Services;

public sealed class PosOfflineDeviceService(IPosOfflineDeviceRepository repository, ITenantContext tenantContext)
{
    public async Task<IReadOnlyList<PosOfflineDeviceDto>> GetMineAsync(CancellationToken ct = default) =>
        (await repository.GetMineAsync(ct)).Select(Map).ToArray();

    public async Task<PosOfflineDeviceDto> RegisterAsync(RegisterPosOfflineDeviceRequest request, CancellationToken ct = default)
    {
        var label = request.Label?.Trim() ?? string.Empty;
        if (request.DeviceId == Guid.Empty) throw new DomainException("El identificador del dispositivo es obligatorio.");
        if (request.PublicKey is null || request.PublicKey.Length > 1024) throw new DomainException("La clave pública del dispositivo no es válida.");
        byte[] keyBytes;
        try { keyBytes = Convert.FromBase64String(request.PublicKey); }
        catch (FormatException) { throw new DomainException("La clave pública del dispositivo no tiene un formato válido."); }
        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(keyBytes, out var read);
            if (read != keyBytes.Length || key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
                throw new DomainException("El dispositivo debe usar una clave pública ECDSA P-256.");
        }
        catch (CryptographicException) { throw new DomainException("La clave pública del dispositivo no es válida."); }
        var fingerprint = Convert.ToHexString(SHA256.HashData(keyBytes)).ToLowerInvariant();
        var device = await repository.RegisterAsync(request.DeviceId, label, Convert.ToBase64String(keyBytes), fingerprint, ct);
        return Map(device);
    }

    public async Task RevokeAsync(Guid deviceId, RevokePosOfflineDeviceRequest request, CancellationToken ct = default) =>
        await repository.RevokeAsync(deviceId, request.ExpectedRevision, ct);

    public async Task<SaleDto> CheckoutAsync(OfflineCheckoutRequest request, StoreService store, CancellationToken ct = default)
    {
        if (request.DeviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Payload) || request.Payload.Length > 32_000)
            throw new DomainException("La firma del borrador offline no está completa.");
        byte[] signature;
        try { signature = Convert.FromBase64String(request.Signature); }
        catch (FormatException) { throw new DomainException("La firma del dispositivo tiene un formato inválido."); }
        if (signature.Length != 64) throw new DomainException("La firma del dispositivo no tiene el tamaño esperado.");
        var tenantId = tenantContext.GetRequiredTenantId();
        var userId = tenantContext.CurrentUserId ?? throw new DomainException("No se pudo identificar al usuario actual.");
        var canonicalPayload = PosOfflineCheckoutPayload.Create(tenantId, userId, request.DeviceId, request.Checkout);
        if (!CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(canonicalPayload), System.Text.Encoding.UTF8.GetBytes(request.Payload)))
            throw new DomainException("El contenido firmado no coincide con la orden enviada.");
        var device = await repository.GetActiveForAttestationAsync(request.DeviceId, ct)
            ?? throw new DomainException("Este dispositivo no está inscrito o fue revocado. Regístralo de nuevo con un administrador.");
        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(device.PublicKey), out _);
            if (!key.VerifyData(System.Text.Encoding.UTF8.GetBytes(canonicalPayload), signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                throw new DomainException("La firma del borrador offline no es válida.");
        }
        catch (CryptographicException) { throw new DomainException("La firma del borrador offline no es válida."); }
        return await store.CheckoutAsync(request.Checkout with { OfflineDeviceId = request.DeviceId }, userId, ct);
    }

    private static PosOfflineDeviceDto Map(PosOfflineDevice x) => new(x.DeviceId, x.Label, x.Fingerprint, x.IsActive, x.CreatedAtUtc, x.LastUsedAtUtc, x.Revision);
}
