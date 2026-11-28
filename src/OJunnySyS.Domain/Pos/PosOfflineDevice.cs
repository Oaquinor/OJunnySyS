using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Pos;

/// <summary>A revocable browser key authorized to attest POS drafts for one tenant user.</summary>
public sealed class PosOfflineDevice : TenantEntity
{
    private PosOfflineDevice() { }

    public PosOfflineDevice(Guid tenantId, Guid userId, Guid deviceId, string label, string publicKey, string fingerprint) : base(tenantId)
    {
        if (userId == Guid.Empty || deviceId == Guid.Empty) throw new DomainException("El usuario y el dispositivo son obligatorios.");
        UserId = userId;
        DeviceId = deviceId;
        Label = NormalizeLabel(label);
        PublicKey = publicKey;
        Fingerprint = fingerprint;
        IsActive = true;
        Revision = 1;
    }

    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string PublicKey { get; private set; } = string.Empty;
    public string Fingerprint { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastUsedAtUtc { get; private set; }
    public long Revision { get; private set; }

    public void Revoke(long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (!IsActive) return;
        IsActive = false;
        Revision++;
        MarkUpdated();
    }

    public void RecordUse(DateTimeOffset now)
    {
        if (!IsActive) throw new DomainException("Este dispositivo POS fue revocado.");
        LastUsedAtUtc = now;
        MarkUpdated();
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("El dispositivo cambió en otra sesión. Actualiza la lista.");
    }

    private static string NormalizeLabel(string? label)
    {
        var normalized = label?.Trim() ?? string.Empty;
        if (normalized.Length is < 2 or > 80) throw new DomainException("El nombre del dispositivo debe tener entre 2 y 80 caracteres.");
        return normalized;
    }
}
