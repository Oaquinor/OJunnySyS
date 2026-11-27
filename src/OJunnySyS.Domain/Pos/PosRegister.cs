using System.Text.RegularExpressions;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Inventory;

namespace OJunnySyS.Domain.Pos;

/// <summary>A tenant-owned POS register. Its immutable code is shared with cash shifts and historical sales.</summary>
public sealed class PosRegister : TenantEntity
{
    private PosRegister() { }

    public PosRegister(Guid tenantId, string code, string name, Guid warehouseId) : base(tenantId)
    {
        if (warehouseId == Guid.Empty) throw new DomainException("El almacén del terminal es obligatorio.");
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        WarehouseId = warehouseId;
        IsActive = true;
    }

    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public Guid WarehouseId { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void Rename(string name, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        Name = NormalizeName(name);
        AdvanceRevision();
    }

    public void ChangeWarehouse(Guid warehouseId, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (warehouseId == Guid.Empty) throw new DomainException("El almacén del terminal es obligatorio.");
        if (WarehouseId == warehouseId) return;
        WarehouseId = warehouseId;
        AdvanceRevision();
    }

    public void SetActive(bool active, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (IsActive == active) return;
        IsActive = active;
        AdvanceRevision();
    }

    public static string NormalizeCode(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? "";
        if (normalized.Length is < 1 or > 40 || !Regex.IsMatch(normalized, "^[A-Z0-9][A-Z0-9_-]*$"))
            throw new DomainException("El código de terminal debe tener hasta 40 caracteres: letras, números, guiones o guiones bajos.");
        return normalized;
    }

    private static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? "";
        if (normalized.Length is < 2 or > 120) throw new DomainException("El nombre del terminal debe tener entre 2 y 120 caracteres.");
        return normalized;
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("El terminal cambió en otra sesión. Recarga la página antes de guardar.");
    }

    private void AdvanceRevision()
    {
        Revision = checked(Revision + 1);
        MarkUpdated();
    }
}
